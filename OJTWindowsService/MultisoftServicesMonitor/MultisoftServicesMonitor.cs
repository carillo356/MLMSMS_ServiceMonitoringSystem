using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using Timer = System.Timers.Timer;
using static MultisoftServicesMonitor.CommonMethods;
using System.Timers;
using System.Threading.Tasks;

namespace MultisoftServicesMonitor
{
    public partial class MultisoftServicesMonitor : ServiceBase
    {
        private readonly RealTimeLogger _realTimeLogger = new RealTimeLogger();
        private readonly PeriodicLogger _periodicLogger = new PeriodicLogger();
        public MultisoftServicesMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _realTimeLogger.Start();
            _periodicLogger.Start();
        }

        protected override void OnStop()
        {
            _realTimeLogger.Stop();
            _periodicLogger.Stop();
        }
    }

    public class RealTimeLogger
    {
        private ManagementEventWatcher _eventWatcher;
        private readonly List<(string ServiceName, string ServiceStatus, string HostName)> _servicesInMonitor = new List<(string ServiceName, string ServiceStatus, string HostName)>();
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;

        public void Start()
        {
            SqlDependency.Start(_connectionString);
            GetServicesInMonitor();
            SM_TableChangeListener();

        }

        public void Stop()
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

                using (var connection = GetConnection())
                {
                    if (connection == null) return;

                    // Check if serviceName exists in _servicesInMonitor
                    if (_servicesInMonitor.Any(x => x.ServiceName == serviceInstalled && x.HostName == hostName))
                    {
                        GetServiceLogs(serviceInstalled, out DateTime lastStart, out string lastEventLog);
                        SP_UpdateServiceStatus(connection, serviceInstalled, serviceStatus, hostName, logBy, lastStart, lastEventLog);

                        if (serviceStatus == ServiceControllerStatus.Stopped.ToString())
                        {
                            try
                            {
                                string emailTemplate = ConfigurationManager.AppSettings["singleEmail"];

                                // Format the email message with the required values
                                string emailMessage = string.Format(emailTemplate, serviceInstalled, serviceStatus, lastStart, lastEventLog, hostName, logBy);
                                SendEmail(connection, emailMessage);
                            }
                            catch (Exception ex)
                            {
                                WriteToFile("Exception: " + ex.Message);
                            }
                        }
                    }
                }

                WriteToFile($"Service: {serviceInstalled}, Status: {serviceStatus}");
            }
            catch (Exception ex)
            {
                WriteToFile($"Exception in OnServiceStatusChanged: {ex.Message}");
            }
        }

        private void GetServicesInMonitor()
        {
            try
            {
                using (var connection = GetConnection())
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
                WriteToFile($"Exception in GetServicesInMonitor: {ex.Message}");
            }

            if (_servicesInMonitor != null && _servicesInMonitor.Count > 0)
            {
                InitializeEventWatcher();
            }
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

        public void SM_TableChangeListener()
        {
            try
            {
                using (var connection = GetConnection())
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
                        SM_TableChangeListener();
                    };

                    // Execute the dependencyCommand to start listening for notifications
                    SqlDataReader dependencyReader = dependencyCommand.ExecuteReader();
                    dependencyReader.Close();
                }
            }
            catch (Exception ex)
            {
                WriteToFile($"Exception in SM_TableChangeListener: {ex.Message}");
            }
        }

    }

    public class PeriodicLogger
    {
        readonly Timer checkServicesTimer = new Timer();

        public Queue<string> qGetEventLogList = new Queue<string>();
        private bool isCheckServicesRunning = false;
        public void Start()
        {
            if (int.TryParse(ConfigurationManager.AppSettings.Get("CheckServicesEveryXMinute"), out int interval))
            {
                checkServicesTimer.Interval = (interval) * 60 * 1000;
            }
            else
            {
                checkServicesTimer.Interval = (60) * 60 * 1000; //1 Hour
            }

            checkServicesTimer.Elapsed += new ElapsedEventHandler(OnCheckServicesElapsedTime);

            if (bool.TryParse(ConfigurationManager.AppSettings.Get("RunOnStart"), out bool runOnStart))
            {
                if (runOnStart)
                {
                    CheckServices();
                }
                else if (!runOnStart)
                {
                    checkServicesTimer.Start();
                }
            }
        }

        public void Stop()
        {
            checkServicesTimer.Dispose();
        }

        private void OnCheckServicesElapsedTime(object source, ElapsedEventArgs e)
        {
            if (!isCheckServicesRunning)
            {
                CheckServices();
            }
        }

        public async void CheckServices()
        {
            checkServicesTimer.Stop(); //Stopped the timer to ensure that the timer is stopped when checkservices is running.
            isCheckServicesRunning = true;

            var servicesToUpdate = new List<(string ServiceName, string ServiceStatus, string HostName, string LogBy)>();
            var emailsToSend = new List<string>();

            try
            {
                using (SqlConnection connection = GetConnection()) // Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
                {
                    if (connection == null) return;
                    string hostName = Environment.MachineName;
                    string logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                    ServiceController[] servicesInController = ServiceController.GetServices(hostName);
                    DataTable serviceInfoTable = CreateServiceInfoTable(servicesInController);
                    SP_UpdateServicesAvailable(connection, serviceInfoTable, hostName);

                    var servicesInstalled = GetTripleColumn(connection, GetServicesStatusQuery("ServicesAvailable", "sa")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();
                    var _servicesInMonitor = GetTripleColumn(connection, GetServicesStatusQuery("ServicesMonitored", "sm")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();

                    foreach (var serviceInMonitor in _servicesInMonitor)
                    {
                        var serviceInstalled = servicesInstalled.FirstOrDefault(s => s.ServiceName == serviceInMonitor.ServiceName && s.HostName == serviceInMonitor.HostName);

                        if (serviceInstalled != default) // Check if the service to monitor exists
                        {
                            string currentStatus = serviceInstalled.ServiceStatus;
                            string previousStatus = serviceInMonitor.ServiceStatus;

                            if (currentStatus != previousStatus) // Check if the current status is the same as the previous status
                            {

                                servicesToUpdate.Add((serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy)); // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.
                                qGetEventLogList.Enqueue(serviceInMonitor.ServiceName);

                                if (currentStatus == ServiceControllerStatus.Stopped.ToString())
                                {
                                    // Read the email body template from the app.config
                                    string emailBodyTemplate = ConfigurationManager.AppSettings["bulkEmail"];

                                    // Format the email body with the required values
                                    string emailBody = string.Format(emailBodyTemplate, serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy);

                                    emailsToSend.Add(emailBody);
                                }
                            }
                        }
                        else
                        {
                            servicesToUpdate.Add((serviceInMonitor.ServiceName, "NotFound", serviceInMonitor.HostName, logBy));
                        }
                    }

                    if (servicesToUpdate.Any())
                    {
                        SP_UpdateServiceStatus(connection, servicesToUpdate.ToArray());
                    }
                    if (emailsToSend.Any())
                    {
                        SendEmail(connection, emailsToSend.ToArray());
                    }

                    if (connection.State != ConnectionState.Closed) connection.Close();
                }

            }

            catch (Exception ex)
            {
                WriteToFile("Exception: " + ex.Message);

            }

            finally
            {
                checkServicesTimer.Start();
                await ProcessQueue();
                isCheckServicesRunning = false; // Add this line
            }
        }

        public async Task ProcessQueue()
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    while (qGetEventLogList.Count > 0)
                    {
                        await Task.Delay(1000);
                        if (qGetEventLogList.Count > 0) 
                        {
                            string serviceName = qGetEventLogList.Dequeue();
                            GetServiceLogs(serviceName, out DateTime lastStart, out string lastEventLog);
                            SP_UpdateServiceEventLogInfo(connection, serviceName, lastStart, lastEventLog);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Exception: " + ex.Message);
            } 
        }
    }

}
