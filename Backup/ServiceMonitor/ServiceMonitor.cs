using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration;
using System.Data.SqlClient;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;  
using System.ServiceProcess;
using System.Threading.Tasks;


namespace ServiceMonitor
{
    public partial class ServiceMonitor : ServiceBase
    {
        Timer timer = new Timer();

        public ServiceMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 3600000; //number in milisecinds/1 hour
            timer.Start();
        }

        protected override void OnStop()
        {
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            ServiceController[] services = ServiceController.GetServices();
            string currentStatus = "";
            string previousStatus = "";
            Boolean serviceMatch = false;
            Boolean statusMatch = true;

            string[] servicesToMonitor = ConfigurationManager.AppSettings.Get("service").Split(' '); //Splits to fetch each services to monitors
            foreach (string serviceToMonitor in servicesToMonitor)  //Access each services to monitor
            {
                foreach (ServiceController service in services) //Access installed service
                {
                    if (service.ServiceName == serviceToMonitor) //Checks if Services to monitor exist in the installed services and get its Current Status.
                    {
                        serviceMatch = true;
                    }

                    if (serviceMatch == true)
                    {

                        EventLog eventLog = new EventLog("Application");
                        EventLogEntry latestEntry = eventLog.Entries.Cast<EventLogEntry>().Where(logEntry => logEntry.Source == serviceToMonitor).OrderByDescending(logEntry => logEntry.TimeGenerated).FirstOrDefault();
                        DateTime lastStart = latestEntry.TimeGenerated;
                        string lastLog = latestEntry.Message;

                        SqlConnection connection = null;
                        SqlCommand command = null;
                        string query = $"SELECT TOP 1 ServiceStatus FROM ServiceTB WHERE ServiceName = '{serviceToMonitor}' ORDER BY LastStart DESC";

                        try
                        {
                            connection = new SqlConnection(ConfigurationManager.AppSettings.Get("connectionString"));
                            connection.Open();
                            command = new SqlCommand(query, connection);
                            SqlDataReader reader = command.ExecuteReader(); //Gets the Previous Status.

                            if (reader.Read())
                            {
                                currentStatus = service.Status.ToString();
                                previousStatus = reader["ServiceStatus"].ToString();

                                if (currentStatus != previousStatus) //Checks if the current status is different from previous status
                                {
                                    statusMatch = false;
                                }

                                if (statusMatch == false && service.Status == ServiceControllerStatus.Stopped)
                                {
                                    //SendEmail("Service Name: " + service.ServiceName + "\nStatus: "
                                    //          + service.Status + "\nLastStart: " + lastStart + "\nLastLog: " + lastLog);
                                    StoreData(service.ServiceName, lastStart, service.Status.ToString(), lastLog);
                                }

                                if (statusMatch == false && !(service.Status == ServiceControllerStatus.Stopped))
                                {
                                    StoreData(service.ServiceName, lastStart, service.Status.ToString(), lastLog);
                                }
                            }
                            else StoreData(service.ServiceName, lastStart, service.Status.ToString(), lastLog);



                            reader.Close();
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
                        serviceMatch = false;
                    }


                }
            }
        }//Closing of OnElapsedTimel()

        static void SendEmail(string message)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings.Get("smtpServer"));

                mail.From = new MailAddress("carillo.aaronjoseph@gmail.com");

                string[] mailTos = ConfigurationManager.AppSettings.Get("mailTo").Split(' ');
                foreach (string s in mailTos)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        mail.To.Add(s);
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

    }
}
