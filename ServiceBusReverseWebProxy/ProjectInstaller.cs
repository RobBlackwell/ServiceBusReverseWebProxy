//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System.ComponentModel;
    using System.Configuration.Install;

    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
