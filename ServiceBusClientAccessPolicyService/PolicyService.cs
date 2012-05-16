//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ClientAccessPolicyService
{
    using System;
    using System.IO;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.ServiceBus.Web;

    [ServiceContract]
    interface IClientAccessPolicyXml
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message GetSilverlightPolicy(Message request);
    }

    [ServiceContract]
    interface ICrossDomainXml
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message GetFlashPolicy(Message request);
    }

    class PolicyService : IClientAccessPolicyXml, ICrossDomainXml
    {
        public Message GetSilverlightPolicy(Message request)
        {
            HttpRequestMessageProperty httpRequestProperty;
            if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                httpRequestProperty = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                if (!httpRequestProperty.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    Message reply = Message.CreateMessage(MessageVersion.None, String.Empty);
                    HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();
                    responseProperty.StatusCode = System.Net.HttpStatusCode.MethodNotAllowed;
                    responseProperty.SuppressEntityBody = true;
                    reply.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
                    return reply;
                }
            }

            string result = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <access-policy>
                                        <cross-domain-access>
                                            <policy>
                                                <allow-from http-request-headers=""*"">
                                                    <domain uri=""*""/>
                                                </allow-from>
                                                <grant-to>
                                                    <resource path=""/"" include-subpaths=""true""/>
                                                </grant-to>
                                            </policy>
                                        </cross-domain-access>
                                    </access-policy>";
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml";
            Message replyMessage = StreamMessageHelper.CreateMessage(MessageVersion.None, String.Empty, new MemoryStream(Encoding.UTF8.GetBytes(result)));
            HttpResponseMessageProperty replyProperty = new HttpResponseMessageProperty();
            replyProperty.StatusCode = System.Net.HttpStatusCode.OK;
            replyProperty.Headers[HttpResponseHeader.ContentType] = "text/xml;charset=utf-8";
            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, replyProperty);
            return replyMessage;

        }
    
        public Message GetFlashPolicy(Message request)
        {

            HttpRequestMessageProperty httpRequestProperty;
            if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                httpRequestProperty = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                if (!httpRequestProperty.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    Message reply = Message.CreateMessage(MessageVersion.None, String.Empty);
                    HttpResponseMessageProperty responseProperty = new HttpResponseMessageProperty();
                    responseProperty.StatusCode = System.Net.HttpStatusCode.MethodNotAllowed;
                    responseProperty.SuppressEntityBody = true;
                    reply.Properties.Add(HttpResponseMessageProperty.Name, responseProperty);
                    return reply;
                }
            }

            string result = @"<?xml version=""1.0""?>
                                    <!DOCTYPE cross-domain-policy SYSTEM ""http://www.macromedia.com/xml/dtds/cross-domain-policy.dtd"">
                                    <cross-domain-policy>
                                        <allow-access-from domain=""*"" />
                                    </cross-domain-policy>";
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml";
            Message replyMessage = StreamMessageHelper.CreateMessage(MessageVersion.None, String.Empty, new MemoryStream(Encoding.UTF8.GetBytes(result)));
            HttpResponseMessageProperty replyProperty = new HttpResponseMessageProperty();
            replyProperty.StatusCode = System.Net.HttpStatusCode.OK;
            replyProperty.Headers[HttpResponseHeader.ContentType] = "text/xml;charset=utf-8";
            replyMessage.Properties.Add(HttpResponseMessageProperty.Name, replyProperty);
            return replyMessage;
        }
    }

}
