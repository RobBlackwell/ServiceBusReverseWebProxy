//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using Microsoft.Samples.ClientAccessPolicyService;
    using Microsoft.ServiceBus;

    class ReverseWebProxyHost
    {
        List<ReverseWebProxy> reverseWebProxies;
        ServiceHost policyHost;

        public ReverseWebProxyHost()
        {
            reverseWebProxies = new List<ReverseWebProxy>();
        }

        public void Open()
        {
            UsernameCredential usernameCredential = new UsernameCredential(
                Program.Settings.IssuerName,
                Program.Settings.IssuerSecret);

            foreach (PathMappingElement pathMapping in Program.Settings.PathMappings)
            {

                string serviceBusPath = pathMapping.NamespacePath;
                if (!serviceBusPath.EndsWith("/"))
                {
                    serviceBusPath = serviceBusPath + "/";
                }

                Uri proxyListenerUri = ServiceBusEnvironment.CreateServiceUri("http", Program.Settings.ServiceNamespace, serviceBusPath);
                string localUri = pathMapping.LocalUri;
                if (!localUri.EndsWith("/"))
                {
                    localUri = localUri + "/";
                }

                Uri siteUri = new Uri(localUri);
                reverseWebProxies.Add(new ReverseWebProxy(proxyListenerUri, siteUri, usernameCredential));
            }

            foreach (ReverseWebProxy proxy in reverseWebProxies)
            {
                proxy.Open();
            }

            try
            {
                if (Program.Settings.EnableSilverlightPolicy)
                {
                    policyHost = new PolicyServiceHost(Program.Settings.ServiceNamespace, Program.Settings.IssuerName, Program.Settings.IssuerSecret);
                    policyHost.Open();
                }
            }
            catch
            {
                foreach (ReverseWebProxy proxy in reverseWebProxies)
                {
                    try
                    {
                        proxy.Close();
                    }
                    catch
                    {
                        // absorb error
                    }
                }
                throw;
            }
        }

        public void Close()
        {
            if (policyHost != null && policyHost.State == CommunicationState.Opened)
            {
                try
                {
                    policyHost.Close();
                }
                catch
                {
                    policyHost.Abort();
                }
            }

            foreach (ReverseWebProxy proxy in reverseWebProxies)
            {
                try
                {
                    proxy.Close();
                }
                catch
                {
                    // absorb error
                }
            }
        }
    }
}