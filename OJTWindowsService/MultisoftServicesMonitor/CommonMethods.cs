﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Security.Authentication.ExtendedProtection;
using System.ServiceProcess;
using System.Text;

namespace MultisoftServicesMonitor
{
    public class CommonMethods
    {
        private static string _error = "Error";
        public static void SP_UpdateServiceStatus(SqlConnection connection, string serviceName, string serviceStatus, string hostName, string logBy, DateTime lastStart, string lastEventLog)
        {
            try
            {
                // Set default values for null inputs
                serviceStatus = serviceStatus ?? "NotFound";
                hostName = hostName ?? "NotFound";
                logBy = logBy ?? "NotFound";
                lastEventLog = lastEventLog ?? "NotFound";
                if (lastStart == default) lastStart = new DateTime(1900, 1, 1);

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
                CommonMethods.WriteToFile("Exception on SP_UpdateServiceStatus, singleupdate: " + ex.Message, _error);
            }
        }

        public static void SP_UpdateServiceStatus(SqlConnection connection, (string ServiceName, string ServiceStatus, string HostName, string LogBy)[] servicesToUpdate)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("ServiceName", typeof(string));
                dt.Columns.Add("ServiceStatus", typeof(string));
                dt.Columns.Add("HostName", typeof(string));
                dt.Columns.Add("LogBy", typeof(string));
                dt.Columns.Add("LastStart", typeof(DateTime));
                dt.Columns.Add("LastEventLog", typeof(string));

                string lastEventLog = "";
                DateTime lastStart = new DateTime(1900, 1, 1);


                foreach (var service in servicesToUpdate)
                {
                    dt.Rows.Add(service.ServiceName, service.ServiceStatus, service.HostName, service.LogBy, lastStart, lastEventLog);
                    sb.AppendLine($"ServiceName: {service.ServiceName}, ServiceStatus: {service.ServiceStatus}, HostName: {service.HostName}, LogBy: {service.LogBy}");
                }

                using (var command = new SqlCommand("UpdateServiceStatusBulk", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Updates", dt);
                    command.ExecuteNonQuery();
                }

                CommonMethods.WriteToFile(sb.ToString(), "Run Periodic Logger");
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception on SP_UpdateServiceStatus, bulkupdate: " + ex.Message, _error);
            }
        }

        public static void SP_UpdateServiceEventLogInfo(SqlConnection connection, string serviceName, DateTime lastStart, string lastEventLog)
        {
            try
            {
                if (lastEventLog == null) lastEventLog = "NotFound";
                if (lastStart == null) lastStart = new DateTime(1900, 1, 1);

                using (SqlCommand command = new SqlCommand("UpdateServiceEventLogInfo", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@LastStart", lastStart);
                    command.Parameters.AddWithValue("@LastEventLog", lastEventLog);
                    command.ExecuteNonQuery();
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"ServiceName: {serviceName}, LastStart: {lastStart}, LastEventLog: {lastEventLog}");
                CommonMethods.WriteToFile(sb.ToString(), "Run PeriodicLogger");
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception on SP_UpdateServiceEventLogInfo: " + ex.Message, _error);
            }
        }

        public static void GetServiceLogs(string serviceName, out DateTime lastStart, out string lastEventLog)
        {
            lastStart = new DateTime(1900, 1, 1);
            lastEventLog = "NotFound";

            try
            {
                using (var eventLog = new EventLog("Application"))
                {
                    var query = new EventLogQuery("Application", PathType.LogName, $"*[System/Provider/@Name='{serviceName}']")
                    {
                        ReverseDirection = true, // Sort by descending time
                    };

                    using (var reader = new EventLogReader(query))
                    {
                        using (var eventRecord = reader.ReadEvent())
                        {
                            if (eventRecord != null)
                            {
                                lastStart = eventRecord.TimeCreated.GetValueOrDefault();
                                lastEventLog = eventRecord.FormatDescription();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception on GetServiceLogs: " + ex.Message, _error);
            }
        }

        private static string GenerateSSOTokenForUser(SqlConnection connection, string userEmail)
        {
            // Generate a new GUID as the SSO token
            string ssoToken = Guid.NewGuid().ToString();

            // Set the expiration time for the token
            DateTime expirationTime = DateTime.UtcNow.AddHours(24);

            // Store the SSO token in your database along with the user's email and expiration time
            using (connection)
            {
                using (var command = new SqlCommand("InsertServicesToken", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Email", userEmail);
                    command.Parameters.AddWithValue("@Token", ssoToken);
                    command.Parameters.AddWithValue("@ExpirationTime", expirationTime);

                    command.ExecuteNonQuery();
                }
            }

            return ssoToken;
        }

        public static void SendEmail(SqlConnection connection, string[] messages, int? emailsToSend = 1, string serviceName = "")
        {
            string mailSubject = "";
            try
            {
                var recipients = GetEmailRecipients(connection);

                using (var smtpServer = new SmtpClient(ConfigurationManager.AppSettings["smtpServer"]))
                {
                    smtpServer.EnableSsl = true;
                    smtpServer.Port = Convert.ToInt32(ConfigurationManager.AppSettings["smtpPort"]);
                    smtpServer.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["smtpUsername"], ConfigurationManager.AppSettings["smtpPassword"]);
                    if (emailsToSend > 1)
                    {
                        string multiMailSubject = ConfigurationManager.AppSettings["multiMailSubject"];
                        mailSubject = string.Format(multiMailSubject, emailsToSend.ToString());
                    }
                    else
                    {
                        string singleMailSubject = ConfigurationManager.AppSettings["singleMailSubject"];
                        mailSubject = string.Format(singleMailSubject, serviceName);
                    }

                    foreach (var userEmail in recipients)
                    {
                        try
                        {
                            using (var conn = GetConnection())
                            {

                                string ssoToken = GenerateSSOTokenForUser(conn, userEmail);
                                string ssoLoginUrl = $"{ConfigurationManager.AppSettings["appUrl"]}?ssoToken={ssoToken}";

                                using (var mail = new MailMessage())
                                {
                                    mail.From = new MailAddress(ConfigurationManager.AppSettings["mailFrom"]);
                                    mail.Subject = mailSubject;
                                    mail.Body = string.Join("\n\n", messages) + $"\n\nLog in with your Single Sign-On token: {ssoLoginUrl}";
                                    mail.To.Add(userEmail);
                                    smtpServer.Send(mail);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteToFile($"Exception on sending email to {userEmail}: {ex.ToString()}", _error);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                WriteToFile("Exception on SendEmail: " + ex.Message, _error);
                Environment.FailFast("Forcefully stopping the service: " + ex.Message);
            }
        }

        public static void SendEmail(SqlConnection connection, string message, int? emailsToSend = 1, string serviceName = "")
        {
            SendEmail(connection, new string[] { message }, emailsToSend, serviceName);
        }


        private static List<string> GetEmailRecipients(SqlConnection connection)
        {
            return GetSingleColumn(connection, "SELECT [Email] FROM Users WHERE Email_Notification = 1");
        }

        public static void WriteToFile(string message, string title = null, string additionalMessage = null)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToString("yyyyMMdd").Replace('/', '_') + ".txt";
            using (StreamWriter sw = File.AppendText(filepath))
            {
                string logLine = $"{DateTime.Now.ToString("hh:mm tt")} - {title}";
                sw.WriteLine(logLine);
                sw.WriteLine(message);
                if (!string.IsNullOrWhiteSpace(additionalMessage))
                {
                    sw.WriteLine(additionalMessage);
                }
                sw.WriteLine();
            }
        }


        public static string GetServicesStatusQuery(string tableName, string columnNamePrefix)
        {
            return $"SELECT [{columnNamePrefix}_ServiceName], [{columnNamePrefix}_ServiceStatus], [{columnNamePrefix}_HostName] FROM {tableName}";
        }

        public static List<string> GetSingleColumn(SqlConnection connection, string query)
        {
            return GetColumns(connection, query, reader => reader.GetString(0));
        }

        public static List<(string, string)> GetDoubleColumn(SqlConnection connection, string query)
        {
            return GetColumns(connection, query, reader => (
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1)
            ));
        }

        public static List<(string, string, string)> GetTripleColumn(SqlConnection connection, string query)
        {
            return GetColumns(connection, query, reader => (
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2)
            ));
        }

        public static List<T> GetColumns<T>(SqlConnection connection, string query, Func<SqlDataReader, T> readColumns)
        {
            var columns = new List<T>();

            try
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            columns.Add(readColumns(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception on GetColumns: " + ex.Message, _error);
            }

            return columns;
        }

        public static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            SqlConnection connection = new SqlConnection(connectionString);
            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }

        public static void SP_UpdateServiceStatus(SqlConnection connection, string serviceName, string serviceStatus, string hostName, string logBy)
        {
            try
            {
                // Set default values for null inputs
                serviceStatus = serviceStatus ?? "NotFound";
                hostName = hostName ?? "NotFound";
                logBy = logBy ?? "NotFound";
                string lastEventLog = "NotFound";
                DateTime lastStart = new DateTime(1900, 1, 1);

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
                CommonMethods.WriteToFile("Exception on SP_UpdateServiceStatus: " + ex.Message, _error);
            }
        }

        public static void SP_UpdateServicesAvailable(SqlConnection connection, DataTable serviceInfoTable, string hostName)
        {
            try
            {
                using (var command = new SqlCommand("UpdateServicesAvailable", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var serviceInfoParam = new SqlParameter("@ServiceInfo", SqlDbType.Structured)
                    {
                        TypeName = "dbo.ServiceInfoTableType",
                        Value = serviceInfoTable
                    };
                    command.Parameters.Add(serviceInfoParam);
                    command.Parameters.AddWithValue("@HostName", hostName);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception on SP_UpdateServicesAvailable: " + ex.Message, _error);
            }
        }

        public static DataTable CreateServiceInfoTable(string hostName)
        {
            DataTable serviceInfoTable = new DataTable();
            serviceInfoTable.Columns.Add("ServiceName", typeof(string));
            serviceInfoTable.Columns.Add("ServiceStatus", typeof(string));
            serviceInfoTable.Columns.Add("Description", typeof(string));
            serviceInfoTable.Columns.Add("StartupType", typeof(string));
            serviceInfoTable.Columns.Add("LogOnAs", typeof(string));

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"\\\\{hostName}\\root\\cimv2", "SELECT * FROM Win32_Service"))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string serviceName = queryObj["Name"]?.ToString() ?? "";
                    string status = queryObj["State"]?.ToString() ?? "";
                    string description = queryObj["Description"]?.ToString() ?? "";
                    string startupType = queryObj["StartMode"]?.ToString() ?? "";
                    string logOnAs = queryObj["StartName"]?.ToString() ?? "";

                    serviceInfoTable.Rows.Add(serviceName, status, description, startupType, logOnAs);
                }
            }

            return serviceInfoTable;
        }


    }
}