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
using System.Text;


namespace MultisoftServicesMonitor
{
    public partial class MultisoftServicesMonitor : ServiceBase
    {
        private readonly RealTimeLogger _realTimeLogger = new RealTimeLogger();
        //private readonly PeriodicLogger _periodicLogger = new PeriodicLogger();
        public MultisoftServicesMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _realTimeLogger.Start();
            //_periodicLogger.Start();
        }

        protected override void OnStop()
        {
            _realTimeLogger.Stop();
            //_periodicLogger.Stop();

            using (var connection = GetConnection())
            {
                string singleEmailTemplate = ConfigurationManager.AppSettings["multisoftServicesMonitorEmailStopped"].Replace("&#x0A;", "\n"); ;
                string emailMessage = string.Format(singleEmailTemplate, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, ServiceControllerStatus.Stopped.ToString());
                SendEmail(connection, emailMessage);
            }

        }
    }

    public class RealTimeLogger
    {
        private List<ManagementEventWatcher> _eventWatchers = new List<ManagementEventWatcher>();
        private readonly List<(string ServiceName, string ServiceStatus, string HostName)> _servicesInMonitor = new List<(string ServiceName, string ServiceStatus, string HostName)>();
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;

        private int numMachines;
        private string[,] machineDetails;

        public void Start()
        {
            SqlDependency.Start(_connectionString);
            GetServicesInMonitor();
            SM_TableChangeListener();

        }

        public void Stop()
        {
            foreach (var eventWatcher in _eventWatchers)
            {
                eventWatcher?.Stop();
                eventWatcher?.Dispose();
            }

            if (!string.IsNullOrEmpty(_connectionString))
            {
                SqlDependency.Stop(_connectionString);
            }
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
                                string singleEmailTemplate = ConfigurationManager.AppSettings["singleEmail"].Replace("&#x0A;", "\n");

                                // Format the email message with the required values
                                string emailMessage = string.Format(singleEmailTemplate, serviceInstalled, serviceStatus, lastStart, lastEventLog, hostName, logBy);
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
            foreach (var eventWatcher in _eventWatchers)
            {
                eventWatcher?.Stop();
                eventWatcher?.Dispose();
            }

            _eventWatchers.Clear();

            // Create WQL Event Query
            List<string> servicesInMonitor = _servicesInMonitor.Select(x => x.ServiceName).ToList();
            string queryCondition = string.Join(" OR ", _servicesInMonitor.Select(service => $"TargetInstance.Name = '{service.ServiceName}' AND TargetInstance.SystemName = '{service.HostName}'"));
            var query = new WqlEventQuery($"SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Service' AND ({queryCondition})");

            string machineName = ConfigurationManager.AppSettings.Get("machineName");
            string[] machineNameArray = machineName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string machineUsername = ConfigurationManager.AppSettings.Get("machineUsername");
            string[] machineUsernameArray = machineUsername.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string machinePassword = ConfigurationManager.AppSettings.Get("machinePassword");
            string[] machinePasswordArray = machinePassword.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int numMachines = machineNameArray.Length;
            string[,] machineDetails = new string[numMachines, 3];
            for (int i = 0; i < numMachines; i++)
            {
                machineDetails[i, 0] = machineNameArray[i];
                machineDetails[i, 1] = machineUsernameArray[i];
                machineDetails[i, 2] = machinePasswordArray[i];
            }

            for (int i = 0; i < numMachines; i++)
            {
                try
                {
                    string machine = machineDetails[i, 0];
                    string username = machineDetails[i, 1];
                    string password = machineDetails[i, 2];

                    ConnectionOptions options = null;
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        options = new ConnectionOptions
                        {
                            Username = username,
                            Password = password,
                            Impersonation = ImpersonationLevel.Impersonate,
                            Authentication = AuthenticationLevel.PacketPrivacy,
                            EnablePrivileges = true
                        };
                    }

                    ManagementScope scope = new ManagementScope($"\\\\{machine}\\root\\CIMV2", options);
                    scope.Connect();

                    // Initialize Event Watcher
                    var eventWatcher = new ManagementEventWatcher(scope, query);
                    eventWatcher.EventArrived += OnServiceStatusChanged;

                    // Add the event watcher to the list
                    _eventWatchers.Add(eventWatcher);

                    // Start the event watcher
                    eventWatcher.Start();
                }
                catch (Exception ex)
                {
                    WriteToFile($"Exception in InitializeEventWatcher for machine {machineDetails[i, 0]}: {ex.Message}");
                    continue;
                }
            }
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

    //public class PeriodicLogger
    //{
    //    readonly Timer checkServicesTimer = new Timer();

    //    public void Start()
    //    {
    //        if (int.TryParse(ConfigurationManager.AppSettings.Get("checkServicesEveryXMinute"), out int interval))
    //        {
    //            checkServicesTimer.Interval = /*(interval) * 60 * 1000*/5000;
    //        }
    //        else
    //        {
    //            checkServicesTimer.Interval = (60) * 60 * 1000; //1 Hour
    //        }

    //        checkServicesTimer.Elapsed += new ElapsedEventHandler(OnCheckServicesElapsedTime);

    //        if (bool.TryParse(ConfigurationManager.AppSettings.Get("runOnStart"), out bool runOnStart))
    //        {
    //            if (runOnStart)
    //            {
    //                CheckServices();
    //            }
    //            else if (!runOnStart)
    //            {
    //                checkServicesTimer.Start();
    //            }
    //        }
    //    }

    //    public void Stop()
    //    {
    //        checkServicesTimer.Dispose();
    //    }

    //    private void OnCheckServicesElapsedTime(object source, ElapsedEventArgs e)
    //    {
    //        CheckServices();
    //    }

    //    public void CheckServices()
    //    {
    //        checkServicesTimer.Stop(); //Stopped the timer to ensure that the timer is stopped when checkservices is running.

    //        var servicesToUpdate = new List<(string ServiceName, string ServiceStatus, string HostName, string LogBy)>();
    //        var emailsToSend = new List<string>();

    //        try
    //        {
    //            using (SqlConnection connection = GetConnection()) // Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
    //            {
    //                if (connection == null) return;
    //                string logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

    //                string machineName = ConfigurationManager.AppSettings.Get("machineName");
    //                string[] machineNameArray = machineName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //                string machineUsername = ConfigurationManager.AppSettings.Get("machineUsername");
    //                string[] machineUsernameArray = machineUsername.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //                string machinePassword = ConfigurationManager.AppSettings.Get("machinePassword");
    //                string[] machinePasswordArray = machinePassword.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


    //                int numMachines = machineNameArray.Length;
    //                string[,] machineDetails = new string[numMachines, 3];
    //                for (int i = 0; i < numMachines; i++)
    //                {
    //                    machineDetails[i, 0] = machineNameArray[i];
    //                    machineDetails[i, 1] = machineUsernameArray[i];
    //                    machineDetails[i, 2] = machinePasswordArray[i];
    //                }

    //                for (int i = 0; i < numMachines; i++)
    //                {
    //                    try
    //                    {
    //                        string machine = machineDetails[i, 0];
    //                        string username = machineDetails[i, 1];
    //                        string password = machineDetails[i, 2];

    //                        ConnectionOptions options = null;
    //                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
    //                        {
    //                            options = new ConnectionOptions
    //                            {
    //                                Username = username,
    //                                Password = password,
    //                                Impersonation = ImpersonationLevel.Impersonate,
    //                                Authentication = AuthenticationLevel.PacketPrivacy
    //                            };
    //                        }

    //                        ManagementScope scope = new ManagementScope($"\\\\{machine}\\root\\cimv2", options);
    //                        scope.Connect();

    //                        ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Service");
    //                        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

    //                        DataTable serviceInfoTableRemote = CreateServiceInfoTable(searcher.Get());
    //                        SP_UpdateServicesAvailable(connection, serviceInfoTableRemote, machine);
    //                    }
    //                    catch
    //                    {
    //                        continue;
    //                    }
    //                }

    //                var localMachine = Environment.MachineName;

    //                ServiceController[] servicesInController = ServiceController.GetServices(localMachine);
    //                DataTable serviceInfoTableLocal = CreateServiceInfoTable(servicesInController);
    //                SP_UpdateServicesAvailable(connection, serviceInfoTableLocal, localMachine);


    //                var servicesInstalled = GetTripleColumn(connection, GetServicesStatusQuery("ServicesAvailable", "sa")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();
    //                var _servicesInMonitor = GetTripleColumn(connection, GetServicesStatusQuery("ServicesMonitored", "sm")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();

    //                foreach (var serviceInMonitor in _servicesInMonitor)
    //                {
    //                    var serviceInstalled = servicesInstalled.FirstOrDefault(s => s.ServiceName == serviceInMonitor.ServiceName && s.HostName == serviceInMonitor.HostName);

    //                    if (serviceInstalled != default) // Check if the service to monitor exists
    //                    {
    //                        string currentStatus = serviceInstalled.ServiceStatus;
    //                        string previousStatus = serviceInMonitor.ServiceStatus;

    //                        if (currentStatus != previousStatus) // Check if the current status is the same as the previous status
    //                        {

    //                            servicesToUpdate.Add((serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy)); // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.

    //                            if (currentStatus == ServiceControllerStatus.Stopped.ToString())
    //                            {
    //                                // Read the email body template from the app.config
    //                                string bulkEmailTemplate = ConfigurationManager.AppSettings["bulkEmail"].Replace("&#x0A;", "\n");

    //                                // Format the email body with the required values
    //                                string emailBody = string.Format(bulkEmailTemplate, serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy);

    //                                emailsToSend.Add(emailBody);
    //                            }
    //                        }
    //                    }
    //                    else
    //                    {
    //                        servicesToUpdate.Add((serviceInMonitor.ServiceName, "NotFound", serviceInMonitor.HostName, logBy));
    //                    }
    //                }

    //                if (servicesToUpdate.Any())
    //                {
    //                    SP_UpdateServiceStatus(connection, servicesToUpdate.ToArray());
    //                }
    //                if (emailsToSend.Any())
    //                {
    //                    SendEmail(connection, emailsToSend.ToArray());
    //                }

    //                if (connection.State != ConnectionState.Closed) connection.Close();
    //            }

    //        }

    //        catch (Exception ex)
    //        {
    //            WriteToFile("Exception: " + ex.Message);

    //        }

    //        finally
    //        {
    //            checkServicesTimer.Start();
    //        }
    //    }

    //}

}
