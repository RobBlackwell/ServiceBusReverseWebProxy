//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System;
    using System.Configuration;
    using System.ServiceProcess;

    static class Program
    {
        public static ReverseWebProxySection Settings;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            bool runOnConsole = true;
            bool showHelp = false;

            try
            {
                Settings = ConfigurationManager.GetSection("reverseWebProxy") as ReverseWebProxySection;
                if (Settings == null)
                {
                    Console.WriteLine("Missing section 'reverseWebProxy' in configuration file.");
                    return;
                }
                else if (string.IsNullOrEmpty(Settings.ServiceNamespace))
                {
                    Console.WriteLine("Configuration attribute 'netServiceProjectName' is not set or empty.");
                    return;
                }
                else if (string.IsNullOrEmpty(Settings.IssuerName))
                {
                    Console.WriteLine("Configuration attribute 'netServiceProjectPassword' is not set or empty.");
                    return;
                }
            }
            catch (ConfigurationErrorsException ce)
            {
                Console.WriteLine("Configuration exception: {0}", ce.Message);
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-' || args[i][0] == '/')
                {
                    switch (args[i].Substring(1).ToUpperInvariant())
                    {
                        case "C":
                        case "CONSOLE":
                            runOnConsole = true;
                            break;
                        case "?":
                        case "H":
                        case "HELP":
                            showHelp = true;
                            break;
                    }
                }
            }

            // only run in OS Service mode if registered
            if (!runOnConsole)
            {
                ServiceController sc = new ServiceController("ServiceBusReverseWebProxy");
                try
                {
                    var status = sc.Status;
                }
                catch (SystemException)
                {
                    runOnConsole = true;
                }
            }

            if (showHelp)
            {
                Console.WriteLine(".NET Service Bus Reverse Proxy");
                Console.WriteLine("ServiceBusReverseWebProxy.exe [/console] [/help]");
                Console.WriteLine("  /console  Run service in console window. Short /c");
                Console.WriteLine("  /help     Show help. Short /?");
            }
            else if (runOnConsole)
            {
                ReverseWebProxyHost host = new ReverseWebProxyHost();
                host.Open();

                Console.WriteLine(".NET Service Bus Reverse Proxy is running.");
                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();

                host.Close();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new ServiceBusReverseWebProxy() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
