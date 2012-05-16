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
        string solutionName;
        string solutionPassword;

        public PolicyServiceHost(string solutionName, string solutionPassword)
            : base(typeof(PolicyService))
        {
            this.solutionName = solutionName;
            this.solutionPassword = solutionPassword;
        }

        protected override void InitializeRuntime()
        {
            TransportClientEndpointBehavior credentials = new TransportClientEndpointBehavior();
            credentials.CredentialType = TransportClientCredentialType.UserNamePassword;
            credentials.Credentials.UserName.UserName = this.solutionName;
            credentials.Credentials.UserName.Password = this.solutionPassword;

            var policyBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None);
            var clientAccessPolicyXml = AddServiceEndpoint(typeof(IClientAccessPolicyXml), policyBinding, ServiceBusEnvironment.CreateServiceUri("http", this.solutionName, "ClientAccessPolicy.xml"));
            clientAccessPolicyXml.Behaviors.Add(credentials);
            var crossDomainXml = AddServiceEndpoint(typeof(ICrossDomainXml), policyBinding, ServiceBusEnvironment.CreateServiceUri("http", this.solutionName, "crossdomain.xml"));
            crossDomainXml.Behaviors.Add(credentials);
           
            base.InitializeRuntime();
        }
    }
}
