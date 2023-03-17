using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Mail;  
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Logger
{
    class Logger
    {
        static void Main(string[] args)
        {
            string serviceName = args[0]; //Gets the service name from the main.

            try
            {
                ServiceController[] servicesInController = ServiceController.GetServices(); //3. Gets the installed services and stored it in services array
                ServiceController serviceInController = servicesInController.FirstOrDefault(s => s.ServiceName == serviceName);
                string serviceNamesCsv = string.Join("/", servicesInController.Select(s => s.ServiceName).ToArray());

                using (SqlConnection connection = GetConnection())
                {
                    if (connection == null) return;

                    SP_UpdateServicesAvailable(connection, serviceNamesCsv);
                    FetchServiceLogs(serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog);
                    SP_UpdateServiceStatus(connection, serviceName, serviceStatus.ToString(), hostName, logBy, lastStart, lastEventLog);

                    if (serviceStatus == ServiceControllerStatus.Stopped)
                    {
                        //SendEmail(connection,"Service Name: " + serviceInController.ServiceName + "\nStatus: " + serviceInController.Status + "\n" );
                    }

                }
            }
            catch(Exception ex) 
            {
                WriteToFile("Exception: main " + ex.Message);
            }

        }

        static void FetchServiceLogs(ServiceController serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog)
        {
            string serviceName = serviceInController.ServiceName;
            serviceStatus = serviceInController.Status;
            lastStart = new DateTime(1900, 1, 1);
            lastEventLog = "No Record";
            hostName = "No Record";
            logBy = "Unknown";

            try
            {
                    logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                    EventLog eventLog = new EventLog("Application");
                    EventLogEntry serviceEvent = eventLog.Entries.Cast<EventLogEntry>().Where(entry => entry.Source == serviceName).OrderByDescending(entry => entry.TimeGenerated).FirstOrDefault();
                    if (serviceEvent.TimeGenerated > lastStart)
                    {
                        lastStart = serviceEvent.TimeGenerated;
                    }
                    lastEventLog = serviceEvent.Message;
                    hostName = serviceEvent.MachineName;

            }
            catch (Exception ex)
            {
                WriteToFile("Exception: servicelogs" + ex.Message);
            }
        }

        static void SP_UpdateServicesAvailable(SqlConnection connection, string updateServicesAvailable)
        {
            try
            {
                using (var command = new SqlCommand("UpdateServicesAvailable", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceNameList", updateServicesAvailable);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Exception: storeservicesavailable " + ex.Message);
            }
        }

        static void SP_UpdateServiceStatus(SqlConnection connection, string serviceName, string serviceStatus, string hostName, string logBy, DateTime lastStart, string lastEventLog)
        {
            try
            {
                using (var command = new SqlCommand("UpdateServiceStatus", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                    command.Parameters.AddWithValue("@HostName", hostName);
                    command.Parameters.AddWithValue("@LogBy", logBy);
                    command.Parameters.AddWithValue("@LastStart", lastStart);
                    command.Parameters.AddWithValue("@LastEventLog", lastEventLog);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Exception: storedata " + ex.Message);
            }
        }

        static void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToString("yyyyMMdd").Replace('/', '_') + ".txt";
            using (StreamWriter sw = File.AppendText(filepath))
            {
                sw.WriteLine(Message);
                sw.WriteLine(DateTime.Now.ToString("hh:mm tt"));
                sw.WriteLine();
            }


        }

        static void SendEmail(SqlConnection connection, string message)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings.Get("smtpServer"));

                mail.From = new MailAddress(ConfigurationManager.AppSettings.Get("mailFrom"));

                string queryFetchEmail = $"SELECT [Email] FROM Users WHERE Email_Notification = 1";
                List<string> emails = FetchSingleColumn(connection, queryFetchEmail, "Email");
                foreach (string email in emails)
                {
                    mail.To.Add(email);
                }
                mail.Subject = "Windows Service Alert!";
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
                throw ex;
            }
        }

        static List<string> FetchSingleColumn(SqlConnection connection, string query, string columnName)
        {
            List<string> multipleData = new List<string>();

            try
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            multipleData.Add(reader[columnName].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return multipleData;
        }

        static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            SqlConnection connection = new SqlConnection(connectionString);
            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }

    }

}

//public sealed class DatabaseManager 
//{
//    private static readonly Lazy<SqlConnection> _lazyConnection = new(() => new SqlConnection());
//    private DatabaseManager()
//    {
//    }

//    public static SqlConnection GetConnection()
//    {
//        string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
//        SqlConnection connection = _lazyConnection.Value;
//        if (connection.State != System.Data.ConnectionState.Open)
//        {
//            connection.ConnectionString = connectionString;
//            connection.Open();
//        }

//        return connection;
//    }

//    public static void OpenConnection()
//    {
//        SqlConnection connection = _lazyConnection.Value;
//        string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
//        connection.ConnectionString = connectionString;
//        if (connection.State != System.Data.ConnectionState.Open)
//        {
//            connection.Open();
//        }
//    }

//    public static void CloseConnection()
//    {
//        if (_lazyConnection.Value.State != System.Data.ConnectionState.Closed)
//        {
//            _lazyConnection.Value.Close();
//        }
//    }
//}

//static void SendEmail(string message)
//{
//    try
//    {
//        MailMessage mail = new();
//        SmtpClient SmtpServer = new(ConfigurationManager.AppSettings.Get("smtpServer"));

//        mail.From = new MailAddress("");

//        string[] mailTos = ConfigurationManager.AppSettings.Get("mailTo").Split(" ");
//        foreach (string s in mailTos)
//        {
//            if (!string.IsNullOrWhiteSpace(s))
//                mail.To.Add(s);
//        }
//        mail.Subject = "Windows Service Alert";
//        mail.Body = message;

//        SmtpServer.Port = 587;
//        SmtpServer.UseDefaultCredentials = false;
//        SmtpServer.Credentials = new NetworkCredential(
//                                     ConfigurationManager.AppSettings.Get("smtpUsername"),
//                                     ConfigurationManager.AppSettings.Get("smtpPassword"));
//        SmtpServer.EnableSsl = true;

//        SmtpServer.Send(mail);
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine(ex.ToString());
//    }
//}//Closing of SendEmail()

//static void StoreServicesMonitored(SqlConnection connection, string serviceName)
//{
//    try
//    {
//        using (var command = new SqlCommand("INSERT INTO ServicesMonitored (sm_ServiceName) SELECT @ServiceName WHERE NOT EXISTS (SELECT 1 FROM ServicesMonitored WHERE sm_ServiceName = @ServiceName)", connection))
//        {
//            command.Parameters.AddWithValue("@ServiceName", serviceName);
//            command.ExecuteNonQuery();
//        }
//    }
//    catch (Exception ex)
//    {
//        throw ex;
//    }
//}

//static void StoreData(SqlConnection connection, string logBy, string serviceName, string lastStart, string serviceStatus, string lastLog, string actionBy)
//{
//    try
//    {
//        using (var command = new SqlCommand("sp_InsertIntoServices", connection))
//        {
//            command.CommandType = CommandType.StoredProcedure;
//            command.Parameters.AddWithValue("@LogBy", logBy);
//            command.Parameters.AddWithValue("@ServiceName", serviceName);
//            command.Parameters.AddWithValue("@LastStart", lastStart);
//            command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
//            command.Parameters.AddWithValue("@LastLog", lastLog);
//            command.Parameters.AddWithValue("@ActionBy", actionBy);
//            command.ExecuteNonQuery();
//        }
//    }
//    catch (Exception ex)
//    {
//        throw ex;
//    }
//}

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