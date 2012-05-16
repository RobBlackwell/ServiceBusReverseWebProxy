//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using Microsoft.ServiceBus;
    
    class UsernameCredential
    {
        TransportClientEndpointBehavior credentials;

        public UsernameCredential(string username, string password)
        {
            credentials = new TransportClientEndpointBehavior();
            credentials.CredentialType = TransportClientCredentialType.UserNamePassword;
            credentials.Credentials.UserName.UserName = username;
            credentials.Credentials.UserName.Password = password;
        }

        public static implicit operator TransportClientEndpointBehavior(UsernameCredential c)
        {
            return c.credentials;
        }
    }
}
