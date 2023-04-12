using System;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using CommonLibrary;

namespace ServiceMonitorLogger
{
    public partial class ServiceMonitorLogger : ServiceBase
    {
        private ManagementEventWatcher _eventWatcher;
        public ServiceMonitorLogger()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Create WQL Event Query
            var query = new WqlEventQuery("SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Service'");

            // Initialize Event Watcher
            _eventWatcher = new ManagementEventWatcher(query);

            // Subscribe to the EventArrived event
            _eventWatcher.EventArrived += OnServiceStatusChanged;

            // Start the event watcher
            _eventWatcher.Start();

            CommonMethods.WriteToFile("ServiceMonitorLogger has started.");
        }

        protected override void OnStop()
        {
            if (_eventWatcher != null)
            {
                _eventWatcher.Stop(); _eventWatcher.Dispose();
            }

            CommonMethods.WriteToFile("ServiceMonitorLogger has stopped.");
        }

        private void OnServiceStatusChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string hostName = Environment.MachineName, logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var serviceInstalled = (string)targetInstance["Name"];
                var serviceStatus = (string)targetInstance["State"];

                if (targetInstance == null || serviceInstalled == null || serviceStatus == null)
                    return;

                using (var connection = CommonMethods.GetConnection())
                {
                    if (connection == null) return;

                    (string ServiceName, string ServiceStatus)[] servicesInMonitor = CommonMethods.GetDoubleColumn(connection, "GetServicesStatus").ToArray();

                    // Check if serviceName exists in servicesInMonitor
                    if (servicesInMonitor.Any(x => x.ServiceName == serviceInstalled))
                    {
                        CommonMethods.GetServiceLogs(serviceInstalled, out DateTime lastStart, out string lastEventLog);
                        CommonMethods.SP_UpdateServiceStatus(connection, serviceInstalled, serviceStatus, hostName, logBy, lastStart, lastEventLog);

                        if (serviceStatus == ServiceControllerStatus.Stopped.ToString())
                        {
                            CommonMethods.SendEmail(connection, "Service Name: " + serviceInstalled +  "\nStatus: " + serviceStatus + "\nLastStart: " + lastStart + "\nLastEventLog: " + lastEventLog + "\nHostName: " + hostName + "\nLogBy: " + logBy);
                        }
                    }
                }

                CommonMethods.WriteToFile($"Service: {serviceInstalled}, Status: {serviceStatus}");
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile($"Exception in OnServiceStatusChanged: {ex.Message}");
            }
        }
    }
}

