//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ClientAccessPolicyService
{
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ServiceBus;

    public class PolicyServiceHost : ServiceHost
    {
        string issuerName;
        string issuerSecret;
        string serviceNameSpace;

        public PolicyServiceHost(string serviceNameSpace, string issuerName, string issuerSecret)
            : base(typeof(PolicyService))
        {
            this.serviceNameSpace = serviceNameSpace;
            this.issuerName = issuerName;
            this.issuerSecret = issuerSecret;
        }

        protected override void InitializeRuntime()
        {
            TransportClientEndpointBehavior credentials = new TransportClientEndpointBehavior();
            credentials.TokenProvider = TokenProvider.CreateSharedSecretTokenProvider(issuerName, issuerSecret);

            var policyBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None);
            var clientAccessPolicyXml = AddServiceEndpoint(typeof(IClientAccessPolicyXml), policyBinding, ServiceBusEnvironment.CreateServiceUri("http", this.serviceNameSpace, "ClientAccessPolicy.xml"));
            clientAccessPolicyXml.Behaviors.Add(credentials);
            var crossDomainXml = AddServiceEndpoint(typeof(ICrossDomainXml), policyBinding, ServiceBusEnvironment.CreateServiceUri("http", this.serviceNameSpace, "crossdomain.xml"));
            crossDomainXml.Behaviors.Add(credentials);

            base.InitializeRuntime();
        }
    }
}
