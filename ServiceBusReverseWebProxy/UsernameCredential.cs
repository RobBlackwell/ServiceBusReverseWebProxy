//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using Microsoft.ServiceBus;
    
    class UsernameCredential
    {
        TransportClientEndpointBehavior credentials;

        public UsernameCredential(string issuerName, string issuerSecret)
        {
            credentials = new TransportClientEndpointBehavior();
            credentials.TokenProvider = TokenProvider.CreateSharedSecretTokenProvider(issuerName, issuerSecret);
        }

        public static implicit operator TransportClientEndpointBehavior(UsernameCredential c)
        {
            return c.credentials;
        }
    }
}
