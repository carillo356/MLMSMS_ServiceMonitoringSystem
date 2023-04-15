using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using CommonLibrary;

namespace ServiceMonitorRealTimeLogger
{
    public partial class ServiceMonitorRealTimeLogger : ServiceBase
    {
        private ManagementEventWatcher _eventWatcher;
        private List<(string ServiceName, string ServiceStatus, string HostName)> _servicesInMonitor;
        private string _connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
        public ServiceMonitorRealTimeLogger()
        {
            InitializeComponent();
            _servicesInMonitor = new List<(string ServiceName, string ServiceStatus, string HostName)>();
        }

        protected override void OnStart(string[] args)
        {
            GetServicesInMonitor();

            SqlDependency.Start(_connectionString);
            ServicesMonitoredListener();

        }

        protected override void OnStop()
        {
            _eventWatcher.Stop(); 
            _eventWatcher.Dispose();
            SqlDependency.Stop(_connectionString);

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

                    // Check if serviceName exists in _servicesInMonitor
                    if (_servicesInMonitor.Any(x => x.ServiceName == serviceInstalled && x.HostName == hostName))
                    {
                        CommonMethods.GetServiceLogs(serviceInstalled, out DateTime lastStart, out string lastEventLog);
                        CommonMethods.SP_UpdateServiceStatus(connection, serviceInstalled, serviceStatus, hostName, logBy, lastStart, lastEventLog);

                        if (serviceStatus == ServiceControllerStatus.Stopped.ToString())
                        {
                            try
                            {
                                string emailTemplate = ConfigurationManager.AppSettings["singleEmail"];

                                // Format the email message with the required values
                                string emailMessage = string.Format(emailTemplate, serviceInstalled, serviceStatus, lastStart, lastEventLog, hostName, logBy);
                                CommonMethods.SendEmail(connection, emailMessage);
                            }
                            catch(Exception ex) 
                            {
                                CommonMethods.WriteToFile("Exception: " + ex.Message);
                            }
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

        private void GetServicesInMonitor()
        {
            try
            {
                using (var connection = CommonMethods.GetConnection())
                {
                    if (connection == null) return;

                    using (SqlCommand command = new SqlCommand("dbo.GetServicesMonitored", connection))
                    {
                        // Execute the command to get the data
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            _servicesInMonitor.Clear(); // Clear the list before adding new entries

                            while (reader.Read())
                            {
                                _servicesInMonitor.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile($"Exception in GetServicesInMonitor: {ex.Message}");
            }

            InitializeEventWatcher();
        }

        private void InitializeEventWatcher()
        {
            // Stop the event watcher if it's already running
            _eventWatcher?.Stop();

            List<string> serviceNamesToMonitor = _servicesInMonitor.Select(x => x.ServiceName).ToList();

            // Create WQL Event Query
            string queryCondition = string.Join(" OR ", serviceNamesToMonitor.Select(serviceName => $"TargetInstance.Name = '{serviceName}'"));
            var query = new WqlEventQuery($"SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Service' AND ({queryCondition})");

            ManagementScope scope = new ManagementScope($"\\\\{Environment.MachineName}\\root\\CIMV2");

            // Initialize Event Watcher
            _eventWatcher = new ManagementEventWatcher(scope, query);
            _eventWatcher.EventArrived += OnServiceStatusChanged;

            // Start the event watcher
            _eventWatcher.Start();
        }

        public void ServicesMonitoredListener()
        {
            try
            {
                using (var connection = CommonMethods.GetConnection())
                {
                    if (connection == null) return;

                    // Set up the SqlDependency to listen for INSERTs and DELETEs
                    SqlCommand dependencyCommand = new SqlCommand("dbo.GetServicesMonitoredRowCount", connection);
                    SqlDependency dependency = new SqlDependency(dependencyCommand);

                    // Set up the OnChange event handler to only handle INSERTs and DELETEs
                    dependency.OnChange += (sender, e) =>
                    {
                        if (e.Info == SqlNotificationInfo.Insert || e.Info == SqlNotificationInfo.Delete)
                        {
                            GetServicesInMonitor();
                        }
                        ServicesMonitoredListener();
                    };

                    // Execute the dependencyCommand to start listening for notifications
                    SqlDataReader dependencyReader = dependencyCommand.ExecuteReader();
                    dependencyReader.Close();
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile($"Exception in ServicesMonitoredListener: {ex.Message}");
            }
        }

    }
}

