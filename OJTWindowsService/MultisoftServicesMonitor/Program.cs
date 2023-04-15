using System.ServiceProcess;

namespace MultisoftServicesMonitor
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MultisoftServicesMonitor()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
