using System;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using Timer = System.Timers.Timer;
using System.Threading;

namespace ServiceMonitorPeriodicLogger
{
    public partial class ServiceMonitorPeriodicLogger : ServiceBase
    {
        readonly Timer checkServicesTimer = new Timer();
        readonly Timer processQueueTimer = new Timer();

        public Queue<string> qGetEventLogList = new Queue<string>();
        private bool isCheckServicesRunning = false; // flag to indicate whether CheckServices() is currently running

        public ServiceMonitorPeriodicLogger()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (int.TryParse(ConfigurationManager.AppSettings.Get("CheckServicesEveryXMinute"), out int interval))
            {
                checkServicesTimer.Interval = 5000;
            }
            else
            {
                checkServicesTimer.Interval = (60) * 60 * 1000; //1 Hour
            }

            processQueueTimer.Interval = checkServicesTimer.Interval;
            checkServicesTimer.Elapsed += new ElapsedEventHandler(OnCheckServicesElapsedTime);
            processQueueTimer.Elapsed += new ElapsedEventHandler(OnProcessQueueElapsedTime);

            if (bool.TryParse(ConfigurationManager.AppSettings.Get("RunOnStart"), out bool runOnStart))
            {
                if (runOnStart)
                {
                    CheckServices(); 
                    ProcessQueue();
                }
                else if (!runOnStart)
                {
                    checkServicesTimer.Start(); 
                    processQueueTimer.Start();
                }
            }
        }

        protected override void OnStop()
        {
            checkServicesTimer.Dispose(); 
            processQueueTimer.Dispose();
        }

        private void OnCheckServicesElapsedTime(object source, ElapsedEventArgs e)
        {
            CheckServices();
        }
        private void OnProcessQueueElapsedTime(object source, ElapsedEventArgs e)
        {
            while (isCheckServicesRunning)
            {
                Thread.Sleep(500); 
            }
            ProcessQueue();
        }

        public void CheckServices()
        {
            checkServicesTimer.Stop(); //Stopped the timer to ensure that the timer is stopped when checkservices is running.
            isCheckServicesRunning = true; // set flag to indicate that CheckServices() is running

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

                                if (currentStatus == ServiceControllerStatus.Stopped.ToString())
                                {
                                    // Read the email body template from the app.config
                                    string emailBodyTemplate = ConfigurationManager.AppSettings["bulkEmail"];

                                    // Format the email body with the required values
                                    string emailBody = string.Format(emailBodyTemplate, serviceInMonitor.ServiceName, currentStatus, serviceInMonitor.HostName, logBy);

                                    qGetEventLogList.Enqueue(serviceInMonitor.ServiceName);
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
                isCheckServicesRunning = false; // set flag to indicate that CheckServices() has finished running
            }
        }

        public void ProcessQueue()
        {
            processQueueTimer.Stop();

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    if (qGetEventLogList.Count > 0)
                    {
                        string serviceName = qGetEventLogList.Dequeue();
                        GetServiceLogs(serviceName, out DateTime lastStart, out string lastEventLog);
                        SP_UpdateServiceEventLogInfo(connection, serviceName, lastStart, lastEventLog);

                    }

                    if (qGetEventLogList.Count > 0)
                        processQueueTimer.Interval = 1500;
                    else
                        processQueueTimer.Interval = checkServicesTimer.Interval; // if none, set to the same interval as the other timer
                }

            }
            catch (Exception ex)
            {
                WriteToFile("Exception: " + ex.Message);
            }
            finally
            {
                checkServicesTimer.Start(); 
                processQueueTimer.Start();
            }
        }

    }

}


