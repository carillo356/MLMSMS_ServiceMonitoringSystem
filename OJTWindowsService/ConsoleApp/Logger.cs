using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;  
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Logger
    {
        static void Main(string[] args)
        {
                string serviceName = args[0]; //Gets the service name from the main.
                string lastLog = "";

                ServiceController[] services = ServiceController.GetServices();

                foreach (ServiceController service in services)
                {
                    if (service.ServiceName == serviceName)
                    {
                        EventLog eventLog = new EventLog("Application");
                        EventLogEntry latestEntry = eventLog.Entries.Cast<EventLogEntry>().Where(entry => entry.Source == serviceName).OrderByDescending(entry => entry.TimeGenerated).FirstOrDefault();
                        DateTime entryTime = latestEntry.TimeGenerated;
                        lastLog = latestEntry.Message;

                        if (service.Status == ServiceControllerStatus.Stopped)
                        {
                        //SendEmail("Service Name: " + service.ServiceName + "\nStatus: " + service.Status + "\n" + exceptionDetails);
                        StoreData(service.ServiceName, entryTime, service.Status.ToString(), lastLog);
                    }


                    if (!(service.Status == ServiceControllerStatus.Stopped))
                        {
                        StoreData(service.ServiceName, entryTime, service.Status.ToString(), lastLog);
                    }
                    }
                }

            static void SendEmail(string message)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings.Get("smtpServer"));

                    mail.From = new MailAddress("carillo.aaronjoseph@gmail.com");

                    string[] mailTos = ConfigurationManager.AppSettings.Get("mailTo").Split(" ");
                    foreach (string s in mailTos)
                    {
                        if (!string.IsNullOrWhiteSpace(s))
                            mail.To.Add(s);
                    }
                    mail.Subject = "Windows Service Alert";
                    mail.Body = message;

                    SmtpServer.Port = 587;
                    SmtpServer.UseDefaultCredentials = false;
                    SmtpServer.Credentials = new NetworkCredential(
                                                 ConfigurationManager.AppSettings.Get("smtpUsername"),
                                                 ConfigurationManager.AppSettings.Get("smtpPassword"));
                    SmtpServer.EnableSsl = true;

                    SmtpServer.Send(mail);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }//Closing of SendEmail()

            static void StoreData(string serviceName, DateTime entryTime, string serviceStatus, string exceptionDetails)
            {
                SqlConnection connection = null;
                SqlCommand command = null;

                try
                {
                    connection = new SqlConnection(ConfigurationManager.AppSettings.Get("connectionString"));
                    connection.Open();


                    command = new SqlCommand("InsertIntoServiceTB", connection);
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@LastStart", entryTime);
                    command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                    command.Parameters.AddWithValue("@LastLog", exceptionDetails);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occured while executing the query: " + ex.Message);
                }
                finally
                {
                    if (command != null)
                    {
                        command.Dispose();
                    }

                    if (connection != null && connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }//Closing of StoreData()


        }//Closing of Main()
    }//Closing of Logger class
}

//EventLog eventLog = new EventLog("Application");

//var serviceEntries = from entry in eventLog.Entries.Cast<EventLogEntry>()
//                     where entry.Source == serviceName && entry.EntryType == EventLogEntryType.Error
//                     select entry;

//EventLogEntry latestEntry = serviceEntries.OrderByDescending(x => x.TimeGenerated).FirstOrDefault();
//exceptionDetails = latestEntry != null ? "ExceptionDate: " + latestEntry.TimeGenerated + "\nMessage: " + latestEntry.Message : "No exception found";

//var manualStopEntries = from entry in eventLog.Entries.Cast<EventLogEntry>()
//                        where entry.Source == serviceName && entry.EntryType == EventLogEntryType.Information && entry.EventID == 0
//                        select entry;

//EventLogEntry manualStopEvent = manualStopEntries.OrderByDescending(x => x.TimeGenerated).FirstOrDefault();

//if (manualStopEvent != null && latestEntry != null && latestEntry.TimeGenerated > manualStopEvent.TimeGenerated)
//{
//    exceptionDetails = "ExceptionDate: " + latestEntry.TimeGenerated + "\nMessage: " + latestEntry.Message;
//}
//else if (manualStopEvent != null)
//{
//    exceptionDetails = "Service stopped manually on " + manualStopEvent.TimeGenerated;
//}

//if (service.Status == ServiceControllerStatus.Running)
//{
//    string serviceStatus = "start";
//    StoreData(serviceName, serviceStatus);
//}
//else if (service.Status == ServiceControllerStatus.Stopped)
//{
//    string serviceStatus = "stop";
//    SendEmail("Service stopped");
//    StoreData(serviceName, serviceStatus);
//}


/* public static void Main(string[] args)
        {
            string serviceName = args[0];
            string serviceStatus = args[1];

            StoreData(serviceName, serviceStatus);

            string toAddress = "fernandez.norvin01@gmail.com";
            string subject = "Alert from " + serviceName;
            string body = args[2];
            SendEmail(toAddress, subject, body);
        }

        private static void StoreData(string serviceName, string serviceStatus)
        {
            string connectionString = @"Data Source=LAPTOP-F2U9549V\ServiceServer;Initial Catalog=ServiceDB;User ID=sa2;Password=123";
            SqlConnection connection = null;
            SqlCommand command = null;

            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();

                command = new SqlCommand("InsertIntoServiceTB", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ServiceName", serviceName);
                command.Parameters.AddWithValue("@RecordedDateTime", DateTime.Now);
                command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while executing the query: " + ex.Message);
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }

                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        private static void SendEmail(string toAddress, string subject, string body)
        {
            using (var client = new SmtpClient("smtp.gmail.com"))
            {
                client.Port = 587;
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential("carillo.aaronjoseph@gmail.com", "goufkxzyggdcznsp");

                var message = new MailMessage
                {
                    From = new MailAddress("carillo.aaronjoseph@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toAddress);

                try
                {
                    client.Send(message);
                }
                catch (SmtpException ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }
            }
        }
*/  