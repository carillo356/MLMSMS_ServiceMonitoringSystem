using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using CommonLibrary;

namespace ServiceMonitorRealTimeLogger
{
    public partial class ServiceMonitorRealTimeLogger : ServiceBase
    {
        private List<ManagementEventWatcher> _eventWatcher;
        public ServiceMonitorRealTimeLogger()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _eventWatcher = new List<ManagementEventWatcher>();
            // Create WQL Event Query
            var query = new WqlEventQuery("SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Service'");

            // Future development - error "Access De currently
            //var remoteMachines = ConfigurationManager.AppSettings.Get("RemoteMachines").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //var remoteUsername = ConfigurationManager.AppSettings.Get("RemoteUsername");
            //var remotePassword = ConfigurationManager.AppSettings.Get("RemotePassword");

            // Add local machine to the list of machines
            List<string> machines = new List<string> { Environment.MachineName };
            //machines.AddRange(remoteMachines);

            foreach (string machine in machines)
            {
                try
                {
                    //ConnectionOptions options = null;
                    //if (!string.IsNullOrEmpty(remoteUsername) && !string.IsNullOrEmpty(remotePassword) && !machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    //{
                    //    options = new ConnectionOptions
                    //    {
                    //        Username = remoteUsername,
                    //        Password = remotePassword,
                    //        Impersonation = ImpersonationLevel.Impersonate,
                    //        Authentication = AuthenticationLevel.Packet
                    //    };
                    //}

                    ManagementScope scope = new ManagementScope($"\\\\{machine}\\root\\CIMV2"/*, options*/);
                    // Initialize Event Watcher
                    ManagementEventWatcher eventWatcher = new ManagementEventWatcher(scope, query);
                    eventWatcher.EventArrived += OnServiceStatusChanged;

                    // Start the event watcher
                    eventWatcher.Start();
                    _eventWatcher.Add(eventWatcher);
                }
                catch (Exception ex)
                {
             
                    continue;
                }
            }

            CommonMethods.WriteToFile("ServiceMonitorRealTimeLogger has started.");
        }

        protected override void OnStop()
        {

            if (_eventWatcher != null)
            {
                foreach (var eventWatcher in _eventWatcher)
                {
                    eventWatcher.Stop(); eventWatcher.Dispose();
                }
            }

            CommonMethods.WriteToFile("ServiceMonitorRealTimeLogger has stopped.");
        }

        private void OnServiceStatusChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var serviceInstalled = (string)targetInstance["Name"];
                var serviceStatus = (string)targetInstance["State"];
                var hostName = (string)targetInstance["SystemName"];
                var logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                if (targetInstance == null || serviceInstalled == null || serviceStatus == null)
                    return;

                using (var connection = CommonMethods.GetConnection())
                {
                    if (connection == null) return;

                    (string ServiceName, string ServiceStatus, string HostName)[] servicesInMonitor = CommonMethods.GetTripleColumn(connection, "GetServicesStatus").ToArray();

                    // Check if serviceName exists in servicesInMonitor
                    if (servicesInMonitor.Any(x => x.ServiceName == serviceInstalled && x.HostName == hostName))
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

