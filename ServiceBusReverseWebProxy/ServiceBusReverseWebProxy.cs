//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System.ServiceProcess;

    public partial class ServiceBusReverseWebProxy : ServiceBase
    {
        ReverseWebProxyHost host;

        public ServiceBusReverseWebProxy()
        {
            host = new ReverseWebProxyHost();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.RequestAdditionalTime(60000);
            host.Open();
        }

        protected override void OnStop()
        {
            host.Close();
        }
    }
}
