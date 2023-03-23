using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using CommonLibrary;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Collections.Concurrent;

namespace ServiceMonitor
{
    public partial class ServiceMonitor : ServiceBase
    {
        readonly Timer timer = new Timer();
        public ServiceMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (int.TryParse(ConfigurationManager.AppSettings.Get("CheckServicesEveryXMinute"), out int interval))
            {
                timer.Interval = interval * 60 * 1000;
            }
            else
            {
                timer.Interval = 3600000; //1hour
            }

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);

            if (bool.TryParse(ConfigurationManager.AppSettings.Get("RunOnStart"), out bool runOnStart))
            {
                if (runOnStart)
                {
                    CheckServices();
                }
            }

            if (!runOnStart)
                timer.Start();
        }

        protected override void OnStop()
        {
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            CheckServices();
        }

        public void CheckServices()
        {
            timer.Stop(); //1. Stopped the timer to ensure that the timer is stopped when checkservices is running.

            try
            {
                using (SqlConnection connection = CommonMethods.GetConnection()) //2. Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
                {
                    if (connection == null) return;

                    ServiceController[] servicesInController = ServiceController.GetServices(); //3. Gets the installed services and stored it in services array
                    string serviceNamesCsv = string.Join("/", servicesInController.Select(s => s.ServiceName).ToArray()); // here, update all services list in database with values from servicesInController
                    CommonMethods.SP_UpdateServicesAvailable(connection, serviceNamesCsv); // call SP here

                    List<(string ServiceName, string ServiceStatus, string HostName, string LogBy, DateTime LastStart, string LastEventLog)> servicesToUpdate = new List<(string ServiceName, string ServiceStatus, string HostName, string LogBy, DateTime LastStart, string LastEventLog)>();

                    List<(string ServiceName, string ServiceStatus)> servicesInMonitor = GetDoubleColumn(connection, "GetServicesStatus"); //4. Gets the services from the database that we specified to monitor.
                    List<string> emailsToSend = new List<string>();

                    try
                    {
                        Parallel.ForEach(servicesInMonitor, (serviceInMonitor, state) =>
                        {
                            // Check if the service to monitor exists
                            ServiceController serviceInController = servicesInController.FirstOrDefault(ctr => ctr.ServiceName == serviceInMonitor.ServiceName);

                            if (serviceInController != null)
                            {
                                // Check if the current status is the same as the previous status
                                string previousStatus = serviceInMonitor.ServiceStatus;
                                string currentStatus = serviceInController.Status.ToString();

                                if (currentStatus != previousStatus)
                                {
                                    // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.
                                    CommonMethods.GetServiceLogs(serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog);
                                    servicesToUpdate.Add((serviceInController.ServiceName, serviceStatus.ToString(), hostName, logBy, lastStart, lastEventLog));

                                    if (serviceInController.Status == ServiceControllerStatus.Stopped)
                                    {
                                        string emailBody = "Service Name: " + serviceInController.ServiceName + "\nLogBy: " + logBy + "\nStatus: " + serviceStatus.ToString() + "\nLastStart: " + lastStart + "\nlastEventLog: " + lastEventLog;
                                        emailsToSend.Add(emailBody);
                                    }
                                }
                            }
                            else
                            {
                                // If the service is not found, update its status in the database
                                try
                                {
                                    servicesToUpdate.Add((serviceInMonitor.ServiceName, "NotFound", "NotFound", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, new DateTime(1900, 1, 1), "NotFound"));
                                }
                                catch(Exception ex)
                                {
                                    CommonMethods.WriteToFile(ex.Message);
                                }
                                
                            }
                        });

                        //Bulk UpdateServiceStatus
                        if (servicesToUpdate.Any())
                        {
                            CommonMethods.SP_UpdateServiceStatus(connection, servicesToUpdate);
                        }//Bulk SendEmail when ServiceStatus is Stopped
                        if (emailsToSend.Any())
                        {
                            CommonMethods.SendEmail(connection, emailsToSend);
                        }

                    }

                    catch (Exception ex)
                    {
                        CommonMethods.WriteToFile("Exception: localscope " + ex.Message);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed) connection.Close();
                    }
                }

            }

            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception: big scope" + ex.Message);

            }
            finally
            {
                timer.Start();
            }
        }

        static List<(string, string)> GetDoubleColumn(SqlConnection connection, string query)
        {
            List<(string, string)> doubleColumn = new List<(string, string)>();

            try
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string Column1 = reader.GetString(0);
                            string Column2 = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            doubleColumn.Add((Column1, Column2));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception: double" + ex.Message);
            }

            return doubleColumn;
        }

    }
}

//private async Task CheckServices()
//{
//    timer.Stop(); //1. Stopped the timer to ensure that the timer is stopped when checkservices is running.

//    try
//    {
//        using (SqlConnection connection = CommonMethods.GetConnection()) //2. Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
//        {
//            if (connection == null) return;

//            ServiceController[] servicesInController = ServiceController.GetServices(); //3. Gets the installed services and stored it in services array
//            string serviceNamesCsv = string.Join("/", servicesInController.Select(s => s.ServiceName).ToArray()); // here, update all services list in database with values from servicesInController
//            CommonMethods.SP_UpdateServicesAvailable(connection, serviceNamesCsv); // call SP here

//            List<(string ServiceName, string ServiceStatus)> servicesInMonitor = GetDoubleColumn(connection, "GetServicesStatus"); //4. Gets the services from the database that we specified to monitor.

//            var tasks = new List<Task>();

//            try
//            {
//                foreach ((string serviceInMonitor, string serviceInMonitorStatus) in servicesInMonitor)
//                {
//                    tasks.Add(Task.Run(() =>
//                    {
//                        // Check if the service to monitor exists
//                        ServiceController serviceInController = servicesInController.FirstOrDefault(ctr => ctr.ServiceName == serviceInMonitor);

//                        if (serviceInController != null)
//                        {
//                            // Check if the current status is the same as the previous status
//                            string previousStatus = serviceInMonitorStatus;
//                            string currentStatus = serviceInController.Status.ToString();

//                            if (currentStatus != previousStatus)
//                            {
//                                // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.
//                                CommonMethods.GetServiceLogs(serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog);
//                                CommonMethods.SP_UpdateServiceStatus(connection, serviceInController.ServiceName, serviceStatus.ToString(), hostName, logBy, lastStart, lastEventLog);

//                                if (serviceInController.Status == ServiceControllerStatus.Stopped)
//                                {
//                                    //CommonMethods.SendEmail(connection, "Service Name: " + serviceInController.ServiceName + "\nLogBy: " + logBy + "\nStatus: " + serviceStatus.ToString() + "\nLastStart: " + lastStart + "\nlastEventLog: " + lastEventLog);
//                                }
//                            }
//                        }
//                        else
//                        {
//                            // If the service is not found, update its status in the database
//                            CommonMethods.SP_UpdateServiceStatus(connection, serviceInMonitor, "NotFound", "NotFound", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, new DateTime(1900, 1, 1), "NotFound");
//                        }
//                    }));
//                }

//                await Task.WhenAll(tasks); // Start all tasks and wait for them to complete
//            }
//            catch (Exception ex)
//            {
//                CommonMethods.WriteToFile("Exception: localscope " + ex.Message);
//            }
//            finally
//            {
//                if (connection.State != ConnectionState.Closed) connection.Close();
//            }
//        }

//    }

//try
//{
//    using (SqlConnection connection = CommonMethods.GetConnection()) //2. Opened a connection and wrapped it in the using statement to ensure the connection is disposed properly after executing the code inside its body.
//    {
//        if (connection == null) return;

//        ServiceController[] servicesInController = ServiceController.GetServices(); //3. Gets the installed services and stored it in services array
//        string serviceNamesCsv = string.Join("/", servicesInController.Select(s => s.ServiceName).ToArray()); // here, update all services list in database with values from servicesInController
//        CommonMethods.SP_UpdateServicesAvailable(connection, serviceNamesCsv); // call SP here

//        List<(string ServiceName, string ServiceStatus, string HostName, string LogBy, DateTime LastStart, string LastEventLog)> servicesToUpdate = new List<(string ServiceName, string ServiceStatus, string HostName, string LogBy, DateTime LastStart, string LastEventLog)>();
//        List<(string ServiceName, string ServiceStatus)> servicesInMonitor = GetDoubleColumn(connection, "GetServicesStatus"); //4. Gets the services from the database that we specified to monitor.

//        try
//        {

//            foreach ((string serviceInMonitor, string serviceInMonitorStatus) in servicesInMonitor)
//            {

//                // Check if the service to monitor exists
//                ServiceController serviceInController = servicesInController.FirstOrDefault(ctr => ctr.ServiceName == serviceInMonitor);

//                if (serviceInController != null)
//                {
//                    // Check if the current status is the same as the previous status
//                    string previousStatus = serviceInMonitorStatus;
//                    string currentStatus = serviceInController.Status.ToString();

//                    if (currentStatus != previousStatus)
//                    {
//                        // If not, get the latest entry, last start, and last log from the event logs, and record it in the database using StoreData(), then if the current status is false, send email to the registered users in the website.
//                        CommonMethods.GetServiceLogs(serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog);
//                        servicesToUpdate.Add((serviceInController.ServiceName, serviceStatus.ToString(), hostName, logBy, lastStart, lastEventLog));

//                        if (serviceInController.Status == ServiceControllerStatus.Stopped)
//                        {
//                            //CommonMethods.SendEmail(connection, "Service Name: " + serviceInController.ServiceName + "\nLogBy: " + logBy + "\nStatus: " + serviceStatus.ToString() + "\nLastStart: " + lastStart + "\nlastEventLog: " + lastEventLog);
//                        }
//                    }
//                }
//                else
//                {
//                    // If the service is not found, update its status in the database
//                    servicesToUpdate.Add((serviceInMonitor, "NotFound", "NotFound", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, new DateTime(1900, 1, 1), "NotFound"));
//                }
//            }



//            if (servicesToUpdate.Any())
//            {
//                CommonMethods.SP_UpdateServiceStatus(connection, servicesToUpdate);
//            }
//        }

//        catch (Exception ex)
//        {
//            CommonMethods.WriteToFile("Exception: localscope " + ex.Message);
//        }
//        finally
//        {
//            if (connection.State != ConnectionState.Closed) connection.Close();
//        }
//    }

//}