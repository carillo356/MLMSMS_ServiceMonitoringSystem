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
using System.IO;
using System.Reflection;

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
            try
            {
                _realTimeLogger.Stop();
                _periodicLogger.Stop();

                using (var connection = GetConnection())
                {
                    string singleEmailTemplate = ConfigurationManager.AppSettings["multisoftServicesMonitorEmailStopped"].Replace("&#x0A;", "\n"); ;
                    string emailMessage = string.Format(singleEmailTemplate, Assembly.GetExecutingAssembly().GetName().Name, ServiceControllerStatus.Stopped.ToString());
                    SendEmail(connection, emailMessage, 1, GetType().Name);
                }

                WriteToFile(GetType().Name + " have stopped ", "Stop " + GetType().Name);
            }
            catch(Exception ex) 
            {
                Environment.FailFast("Forcefully stopping the service: " + ex.Message);
            }

        }

    }

    public class RealTimeLogger
    {
        private ManagementEventWatcher _eventWatcher;
        private readonly List<(string ServiceName, string ServiceStatus, string HostName)> _servicesInMonitor = new List<(string ServiceName, string ServiceStatus, string HostName)>();
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
        private string _error = "Error";
        private bool enabled_RealTimeLogger = false;


        public void Start()
        {
            if (bool.TryParse(ConfigurationManager.AppSettings.Get("enabled_RealTimeLogger"), out enabled_RealTimeLogger))
            {
                if (enabled_RealTimeLogger)
                {
                    WriteToFile("enabled_RealTimeLogger: " + enabled_RealTimeLogger, "Run " + GetType().Name);
                    try
                    {
                        SqlDependency.Start(_connectionString);
                        GetServicesInMonitor();
                        SM_TableChangeListener();
                    }
                    catch (Exception ex)
                    {
                        enabled_RealTimeLogger = false;
                        WriteToFile("Exception OnStart RealTimeLogger: " + ex.Message, _error);
                    }
                }
                else if (!enabled_RealTimeLogger)
                {
                    WriteToFile("enabled_RealTimeLogger: " + enabled_RealTimeLogger, "Run " + GetType().Name);
                }
            }
            else
            {
                WriteToFile("Config setting 'enabled_RealTimeLogger' not found.");
            }
        }

        public void Stop()
        {
            try
            {
                if (_eventWatcher != null)
                {
                    _eventWatcher.Stop();
                    _eventWatcher.Dispose();
                }

                if (!string.IsNullOrEmpty(_connectionString))
                {
                    SqlDependency.Stop(_connectionString);
                }
            }
            catch(Exception ex)
            {
                WriteToFile("Exception OnStop: " + ex.Message, _error);
    
            }
        }

        private void OnServiceStatusChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (!enabled_RealTimeLogger) return;

                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var serviceName = (string)targetInstance["Name"];
                var serviceStatus = (string)targetInstance["State"];
                var hostName = (string)targetInstance["SystemName"];
                var logBy = GetType().Name;

                if (targetInstance == null || serviceName == null || serviceStatus == null)
                    return;

                using (var connection = GetConnection())
                {
                    if (connection == null) return;

                    // Check if serviceName exists in _servicesInMonitor
                    if (_servicesInMonitor.Any(x => x.ServiceName == serviceName && x.HostName == hostName))
                    {
                        GetServiceLogs(serviceName, out DateTime lastStart, out string lastEventLog);
                        SP_UpdateServiceStatus(connection, serviceName, serviceStatus, hostName, logBy, lastStart, lastEventLog);

                        if (serviceStatus == ServiceControllerStatus.Stopped.ToString())
                        {
                            try
                            {
                                string singleEmailTemplate = ConfigurationManager.AppSettings["singleEmail"].Replace("&#x0A;", "\n");

                                // Format the email message with the required values
                                string emailMessage = string.Format(singleEmailTemplate, serviceName, serviceStatus, lastStart, lastEventLog, hostName, logBy);
                                SendEmail(connection, emailMessage, 1, serviceName);
                            }
                            catch (Exception ex)
                            {
                                WriteToFile("Exception: " + ex.Message, _error);
                            }
                        }
                    }
                }

                WriteToFile($"Service: {serviceName}, Status: {serviceStatus}", "Status Changed");
            }
            catch (Exception ex)
            {
                WriteToFile($"Exception in OnServiceStatusChanged: {ex.Message}", _error);
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
                WriteToFile($"Exception in GetServicesInMonitor: {ex.Message}", _error);
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

            List<string> servicesInMonitor = _servicesInMonitor.Select(x => x.ServiceName).ToList();

            // Create WQL Event Query
            string queryCondition = string.Join(" OR ", servicesInMonitor.Select(serviceName => $"TargetInstance.Name = '{serviceName}'"));
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
                WriteToFile($"Exception in SM_TableChangeListener: {ex.Message}", _error);
            }
        }

    }

    public class PeriodicLogger
    {
        readonly Timer checkServicesTimer = new Timer();
        readonly Timer deleteLogsTimer = new Timer();
        readonly Timer checkCommandsIssuedTimer = new Timer();

        private bool _isCheckServicesRunning = false;
        private bool _isCheckCommandsIssuedRunning = false;
        private bool _isQueueProcessing = false;


        private int _deleteLogsXDaysOld = 30;
        private string _error = "Error";
        public Queue<string> _qGetEventLogList = new Queue<string>();

        public void Start()
        {
            if (int.TryParse(ConfigurationManager.AppSettings.Get("checkServicesEveryXMinute"), out int interval))
            {
                checkServicesTimer.Interval = interval * 60 * 1000;
            }
            else
            {
                checkServicesTimer.Interval = 60 * 60 * 1000; // default is 1 Hour
                WriteToFile("Config setting 'checkServicesEveryXMinute' not found.");
            }

            WriteToFile($"checkServicesEveryXMinute: {checkServicesTimer.Interval / 60000}", $"Run {GetType().Name}");

            if (!int.TryParse(ConfigurationManager.AppSettings.Get("deleteLogsXDaysOld"), out int _deleteLogsXDaysOld))
                _deleteLogsXDaysOld = 30;
            if (_deleteLogsXDaysOld < 7)
                _deleteLogsXDaysOld = 7;
            if (_deleteLogsXDaysOld > 90)
                _deleteLogsXDaysOld = 90;

            checkCommandsIssuedTimer.Interval = /*5 * 60 * 1000*/18000; // every 12 seconds
            checkCommandsIssuedTimer.Elapsed += new ElapsedEventHandler(OnCheckCommandsIssuedElapsedTime);
            checkCommandsIssuedTimer.Start();

            deleteLogsTimer.Interval = 24 * 60 * 60 * 1000; // every 24 hours
            deleteLogsTimer.Elapsed += new ElapsedEventHandler(OnDeleteLogsElapsedTime);
            deleteLogsTimer.Start();

            checkServicesTimer.Elapsed += new ElapsedEventHandler(OnCheckServicesElapsedTime);

            if (bool.TryParse(ConfigurationManager.AppSettings.Get("runOnStart"), out bool runOnStart))
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
            checkServicesTimer?.Dispose();
        }

        private void OnCheckServicesElapsedTime(object source, ElapsedEventArgs e)
        {
            if (!_isCheckServicesRunning && !_isCheckCommandsIssuedRunning)
            {
                CheckServices();
            }
        }

        private void OnDeleteLogsElapsedTime(object source, ElapsedEventArgs e)
        {
            DeleteOldLogFiles(_deleteLogsXDaysOld);
            WriteToFile($"deleteLogsXDaysOld: {_deleteLogsXDaysOld}", $"Delete Logs");
        }

        private void DeleteOldLogFiles(int deleteLogsXDaysOld)
        {
            string logDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

            if (!Directory.Exists(logDirectory))
            {
                return;
            }

            DateTime cutoffDate = DateTime.Now.AddDays(-deleteLogsXDaysOld);

            foreach (string file in Directory.GetFiles(logDirectory, "*.txt"))
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                        CommonMethods.WriteToFile($"Deleted log file: {fileInfo.Name}", $"Delete Logs");
                    }
                    catch (Exception ex)
                    {
                        CommonMethods.WriteToFile($"Error deleting log file {fileInfo.Name}: {ex.Message}", _error);
                    }
                }
            }
        }

        public void CheckServices()
        {
            checkServicesTimer.Stop(); //Stopped the timer to ensure that the timer is stopped when checkservices is running.
            _isCheckServicesRunning = true;

            var servicesToUpdate = new List<(string ServiceName, string ServiceStatus, string HostName, string LogBy)>();
            var emailsToSend = new List<string>();

            try
            {
                using (SqlConnection connection = GetConnection()) // Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
                {
                    if (connection == null) return;
                    string hostName = Environment.MachineName;
                    string logBy = GetType().Name;

                    DataTable serviceInfoTable = CreateServiceInfoTable(hostName);
                    SP_UpdateServicesAvailable(connection, serviceInfoTable, hostName);

                    var servicesInstalled = GetTripleColumn(connection, GetServicesStatusQuery("ServicesAvailable", "sa")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();
                    var servicesMonitored = GetTripleColumn(connection, GetServicesStatusQuery("ServicesMonitored", "sm")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();

                    foreach (var serviceInMonitor in servicesMonitored)
                    {
                        var serviceName = servicesInstalled.FirstOrDefault(s => s.ServiceName == serviceInMonitor.ServiceName && s.HostName == serviceInMonitor.HostName);

                        if (serviceName != default) // Check if the service to monitor exists
                        {
                            string currentStatus = serviceName.ServiceStatus;
                            string previousStatus = serviceInMonitor.ServiceStatus;

                            if (currentStatus != previousStatus) // Check if the current status is the same as the previous status
                            {

                                servicesToUpdate.Add((serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy)); // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.
                                _qGetEventLogList.Enqueue(serviceInMonitor.ServiceName);

                                if (currentStatus == ServiceControllerStatus.Stopped.ToString())
                                {
                                    // Read the email body template from the app.config
                                    string multiEmailTemplate = ConfigurationManager.AppSettings["multiEmail"].Replace("&#x0A;", "\n");

                                    // Format the email body with the required values
                                    string emailBody = string.Format(multiEmailTemplate, serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy);

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
                        SendEmail(connection, emailsToSend.ToArray(), emailsToSend.Count());
                    }

                    if (connection.State != ConnectionState.Closed) connection.Close();
                }

            }

            catch (Exception ex)
            {
                WriteToFile("Exception on CheckServices: " + ex.Message, _error);

            }

            finally
            {
                checkServicesTimer.Start();
                if (!_isQueueProcessing && !_isCheckCommandsIssuedRunning) ProcessQueue();
                _isCheckServicesRunning = false; 
            }
        }

        public void ProcessQueue()
        {
            _isQueueProcessing = true;
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    while (_qGetEventLogList.Count > 0)
                    {
                        string serviceName = _qGetEventLogList.Dequeue();
                        GetServiceLogs(serviceName, out DateTime lastStart, out string lastEventLog);
                        SP_UpdateServiceEventLogInfo(connection, serviceName, lastStart, lastEventLog);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Exception on ProcessQueue: " + ex.Message, _error);
            }
            _isQueueProcessing = false;
        }

        private void OnCheckCommandsIssuedElapsedTime(object source, ElapsedEventArgs e)
        {
            if (!_isCheckServicesRunning && !_isQueueProcessing)
            {
                CheckCommandsIssued();
            }
        }

        public void CheckCommandsIssued()
        {
            checkCommandsIssuedTimer.Stop();
            _isCheckCommandsIssuedRunning = true;

            using (SqlConnection connection = GetConnection())
            {
                var servicesStartStopQueue = GetColumns(connection, GetCommandsIssuedQuery("ServicesStartStopQueue", "sq"), reader => (LogID: reader.GetInt32(0), ServiceName: reader.GetString(1), Command: reader.GetString(2), HostName: reader.GetString(3), IssuedBy: reader.GetInt32(4))).ToArray();
                foreach (var (logId, serviceName, command, hostName, issuedBy) in servicesStartStopQueue)
                {
                    if (hostName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    {
                        ExecuteCommandIssued(logId, serviceName, command.ToLower(), hostName, issuedBy);
                    }
                    else
                    {
                        WriteToFile($"Command '{command}' for service '{serviceName}' was issued on host '{hostName}', current machine's hostname: " + Environment.MachineName, "Monitored ServiceName on Different Host");
                    }
                }
            }

            _isCheckCommandsIssuedRunning = false;
            checkCommandsIssuedTimer.Start();
        }

        public void ExecuteCommandIssued(int logID, string serviceName, string command, string hostName, int issuedBy)
        {
            using (SqlConnection connection = GetConnection())
            {
                try
                {
                    if (!IsServiceAvailable(serviceName, hostName))
                    {
                        WriteToFile(serviceName + " + " + hostName);
                        return;
                    }
                    ServiceController sc = new ServiceController(serviceName);

                    string serviceStatus;
                    string lastEventLog = "Command Issued in the website";
                    switch (command)
                    {
                        case "start":
                            if (sc.Status == ServiceControllerStatus.Stopped)
                            {
                                sc.Start();
                                serviceStatus = ServiceControllerStatus.Running.ToString();
                            }
                            else
                            {
                                serviceStatus = ServiceControllerStatus.Running.ToString();
                                lastEventLog = serviceName + " is already running.";
                                WriteToFile(serviceName + " is already running ", "start");
                            }
                            UpdateDateExecuted(logID);
                            SP_UpdateServiceStatus(connection, serviceName, serviceStatus, hostName, "User" + issuedBy, DateTime.Now, lastEventLog);
                            break;

                        case "stop":
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                sc.Stop();
                                serviceStatus = ServiceControllerStatus.Stopped.ToString();
                            }
                            else
                            {
                                serviceStatus = ServiceControllerStatus.Stopped.ToString();
                                lastEventLog = serviceName + " is already stopped ";
                                WriteToFile(serviceName + " is already stopped ", "stop");
                            }
                            UpdateDateExecuted(logID);
                            SP_UpdateServiceStatus(connection, serviceName, serviceStatus, hostName, "User" + issuedBy, DateTime.Now, lastEventLog);
                            break;

                        case "restart":
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                                sc.Start();
                                serviceStatus = ServiceControllerStatus.Running.ToString();
                            }
                            else
                            {
                                serviceStatus = sc.Status.ToString();
                                lastEventLog = "Failed to restart " + serviceName + ", current Status is " + sc.Status.ToString();
                                WriteToFile("Failed to restart " + serviceName, "restart", "Current Status: " + sc.Status.ToString());
                            }
                            UpdateDateExecuted(logID);
                            SP_UpdateServiceStatus(connection, serviceName, serviceStatus, hostName, "User" + issuedBy, DateTime.Now, lastEventLog);
                            break;

                        default:
                            WriteToFile("The command '" + command + "' is invalid", serviceName);
                            break;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    WriteToFile("Exception OnExecuteCommandIssued: " + ex.Message, _error);
                    return;
                }

            }

        }

        public void UpdateDateExecuted(int logID)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    string updateQuery = "UPDATE [dbo].[ServicesStartStopQueue] SET [sq_DateExecuted] = @DateExecuted WHERE [sq_LogID] = @LogID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@DateExecuted", DateTime.Now);
                        command.Parameters.AddWithValue("@LogID", logID);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex) 
            {
                WriteToFile("Exception OnUpdateDateExecuted: " + ex.Message, _error);
            }

        }

        public bool IsServiceAvailable(string serviceName, string hostName)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    using (SqlCommand command = new SqlCommand("IsServiceAvailable", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ServiceName", serviceName);
                        command.Parameters.AddWithValue("@HostName", hostName);

                        object result = command.ExecuteScalar();
                        return Convert.ToBoolean(result);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

    }

}
