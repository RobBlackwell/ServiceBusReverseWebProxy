//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System;
    using System.IO;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ServiceBus;

    class ReverseWebProxy
    {
        Binding upstreamBinding;
        Uri upstreamUri;
        string upstreamBasePath;
        Uri downstreamUri;
        TransportClientEndpointBehavior credentials;
        MessageEncoder encoder;
        IChannelListener<IReplyChannel> replyChannelListener;

        public ReverseWebProxy(Uri upstreamUri, Uri downstreamUri, TransportClientEndpointBehavior credentials)
        {
            this.upstreamUri = upstreamUri;
            this.downstreamUri = downstreamUri;

            this.upstreamBasePath = this.upstreamUri.PathAndQuery;
            if (this.upstreamBasePath.EndsWith("/"))
            {
                this.upstreamBasePath = this.upstreamBasePath.Substring(0, this.upstreamBasePath.Length - 1);
            }
                    

            ServicePointManager.DefaultConnectionLimit = 50;

            WebHttpRelayBinding relayBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None);
            relayBinding.MaxReceivedMessageSize = int.MaxValue;
            relayBinding.TransferMode = TransferMode.Streamed;
            relayBinding.AllowCookies = false;
            relayBinding.ReceiveTimeout = TimeSpan.MaxValue;
            relayBinding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            relayBinding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            this.upstreamBinding = relayBinding;

            WebMessageEncodingBindingElement encoderBindingElement = new WebMessageEncodingBindingElement();
            encoderBindingElement.ReaderQuotas.MaxArrayLength = int.MaxValue;
            encoderBindingElement.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            encoderBindingElement.ContentTypeMapper = new RawContentTypeMapper();
            encoder = encoderBindingElement.CreateMessageEncoderFactory().Encoder;

            this.credentials = credentials;
        }

        public void Open()
        {
            this.replyChannelListener = this.upstreamBinding.BuildChannelListener<IReplyChannel>(this.upstreamUri, credentials);
            this.replyChannelListener.Open();
            this.replyChannelListener.BeginAcceptChannel(ChannelAccepted, replyChannelListener);
        }

        public void Close()
        {
            if (this.replyChannelListener != null)
            {
                this.replyChannelListener.Close();
            }
        }

        void ChannelAccepted(IAsyncResult result)
        {
            try
            {
                IReplyChannel replyChannel = replyChannelListener.EndAcceptChannel(result);
                if (replyChannel != null)
                {
                    try
                    {
                        replyChannel.Open();
                        replyChannel.BeginReceiveRequest(RequestAccepted, replyChannel);
                    }
                    catch
                    {
                        replyChannel.Abort();
                    }

                    if (replyChannelListener.State == CommunicationState.Opened)
                    {
                        this.replyChannelListener.BeginAcceptChannel(ChannelAccepted, replyChannelListener);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
                this.replyChannelListener.Abort();
                this.replyChannelListener = null;
            }
        }

        void RequestAccepted(IAsyncResult result)
        {
            IReplyChannel replyChannel = (IReplyChannel)result.AsyncState;
            
            try
            {
                RequestContext requestContext = replyChannel.EndReceiveRequest(result);
                if (requestContext != null)
                {
                    string relativePath = requestContext.RequestMessage.Properties.Via.PathAndQuery.Substring(upstreamBasePath.Length);
                    if (relativePath.StartsWith("/"))
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    Uri targetUri = new Uri(this.downstreamUri.AbsoluteUri+relativePath);
                    HttpRequestMessageProperty rm = 
                        requestContext.RequestMessage.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;

                    HttpWebRequest downstreamRequest = HttpWebRequest.Create(targetUri) as HttpWebRequest;
                    downstreamRequest.Method = rm.Method;
                    downstreamRequest.Proxy = null;
                    foreach (string hdr in rm.Headers.Keys)
                    {
                        switch (hdr.ToUpperInvariant())
                        {
                            case "CONNECTION":
                            case "KEEP-ALIVE":
                            case "CONTENT-LENGTH":
                            case "HOST":
                            case "DATE":
                            case "TRANSFER-ENCODING":
                            case "VIA":
                                break;
                            case "ACCEPT":
                                downstreamRequest.Accept = rm.Headers[hdr];
                                break;
                            case "CONTENT-TYPE":
                                downstreamRequest.ContentType = rm.Headers[hdr];
                                break;
                            case "EXPECT":
                                downstreamRequest.Expect = rm.Headers[hdr];
                                break;
                            case "IF-MODIFIED-SINCE":
                                downstreamRequest.IfModifiedSince = DateTime.Parse(rm.Headers[hdr]);
                                break;
                            case "REFERER":
                                downstreamRequest.Referer = rm.Headers[hdr];
                                break;
                            case "USER-AGENT":
                                downstreamRequest.UserAgent = rm.Headers[hdr];
                                break;
                            default:
                                try
                                {
                                    downstreamRequest.Headers.Add(hdr, rm.Headers[hdr]);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                                break;
                        }
                    }
                    downstreamRequest.AllowAutoRedirect = false;
                    downstreamRequest.KeepAlive = true;
                    downstreamRequest.SendChunked = true;
                    
                    int cl;
                    if (rm.SuppressEntityBody ||
                         string.IsNullOrEmpty(rm.Headers[HttpRequestHeader.ContentLength]) ||
                         !int.TryParse(rm.Headers[HttpRequestHeader.ContentLength], out cl) ||
                        cl == 0)
                    {
                        downstreamRequest.BeginGetResponse(RequestCompleted, new object[] { downstreamRequest, requestContext, replyChannel });
                    }
                    else
                    {
                        using (Stream requestStream = downstreamRequest.GetRequestStream())
                        {
                            encoder.WriteMessage(requestContext.RequestMessage, requestStream);
                        }
                        downstreamRequest.BeginGetResponse(RequestCompleted, new object[] { downstreamRequest, requestContext, replyChannel });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                replyChannel.Abort();
            }
        }

        void RequestCompleted(IAsyncResult result)
        {
            HttpWebRequest downstreamRequest = (HttpWebRequest)((object[])result.AsyncState)[0];
            RequestContext requestContext = (RequestContext)((object[])result.AsyncState)[1];
            IReplyChannel replyChannel = (IReplyChannel)((object[])result.AsyncState)[2];

            try
            {
                HttpWebResponse response = downstreamRequest.EndGetResponse(result) as HttpWebResponse;
                if (response.ContentLength > 0)
                {
                    Message downstreamReply = encoder.ReadMessage(response.GetResponseStream(), 65536, response.ContentType);
                    Message upstreamReply = Message.CreateMessage(MessageVersion.None, "RESPONSE", downstreamReply.GetReaderAtBodyContents());
                    HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();
                    responseProperty.Headers.Add(response.Headers);
                    responseProperty.StatusCode = response.StatusCode;
                    responseProperty.StatusDescription = response.StatusDescription;
                    FixResponseHeaders(responseProperty);
                    upstreamReply.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
                    upstreamReply.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
                    requestContext.BeginReply(upstreamReply, DoneReplying, new object[] { response, requestContext, replyChannel });
                }
                else
                {
                    Message upstreamReply = Message.CreateMessage(MessageVersion.None, "RESPONSE");
                    HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();
                    responseProperty.Headers.Add(response.Headers);
                    responseProperty.StatusCode = response.StatusCode;
                    responseProperty.StatusDescription = response.StatusDescription;
                    responseProperty.SuppressEntityBody = true;
                    FixResponseHeaders(responseProperty);
                    upstreamReply.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
                    requestContext.BeginReply(upstreamReply, DoneReplying, new object[] { response, requestContext, replyChannel });
                }
            }
            catch (WebException we)
            {
                HttpWebResponse resp = we.Response as HttpWebResponse;
                if (resp != null)
                {
                    Message upstreamReply = Message.CreateMessage(MessageVersion.None, "RESPONSE");
                    HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();
                    responseProperty.Headers.Add(resp.Headers);
                    responseProperty.StatusCode = resp.StatusCode;
                    responseProperty.StatusDescription = resp.StatusDescription;
                    responseProperty.SuppressEntityBody = true;
                    FixResponseHeaders(responseProperty);
                    upstreamReply.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
                    requestContext.BeginReply(upstreamReply, DoneReplying, new object[] { resp, requestContext, replyChannel });
                }
                else
                {
                    Message upstreamReply = Message.CreateMessage(MessageVersion.None, "RESPONSE");
                    HttpResponseMessageProperty responseProp = new HttpResponseMessageProperty();
                    responseProp.StatusCode = HttpStatusCode.InternalServerError;
                    responseProp.SuppressEntityBody = true;
                    upstreamReply.Properties.Add(HttpResponseMessageProperty.Name, responseProp);
                    requestContext.BeginReply(upstreamReply, DoneReplying, new object[] { null, requestContext, replyChannel });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void FixResponseHeaders(HttpResponseMessageProperty responseProperty)
        {
            for (int i = 0; i < responseProperty.Headers.Keys.Count; i++ )
            {
                responseProperty.Headers[responseProperty.Headers.Keys[i]] =
                    responseProperty.Headers[responseProperty.Headers.Keys[i]].Replace(this.downstreamUri.AbsoluteUri,
                                                             this.upstreamUri.AbsoluteUri);
            }
        }

        void DoneReplying(IAsyncResult result)
        {
            HttpWebResponse downstreamResponse = ((object[])result.AsyncState)[0] as HttpWebResponse;
            RequestContext requestContext = (RequestContext)((object[])result.AsyncState)[1];
            IReplyChannel replyChannel = (IReplyChannel)((object[])result.AsyncState)[2];

            try
            {
                requestContext.EndReply(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (downstreamResponse != null)
                {
                    downstreamResponse.Close();
                }
                replyChannel.BeginReceiveRequest(RequestAccepted, replyChannel);
            }
        }

        class RawContentTypeMapper : WebContentTypeMapper
        {
            public override WebContentFormat GetMessageFormatForContentType(string contentType)
            {
                return WebContentFormat.Raw;
            }
        }
    }
}
