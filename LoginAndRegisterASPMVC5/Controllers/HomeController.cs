using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using LoginAndRegisterASPMVC5.Models;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data.Entity;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.Ajax.Utilities;
using System.Net;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Forms;
using System.Data;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Security.Policy;
using System.Net.Mail;
using System.Web.Services.Description;
using Service = LoginAndRegisterASPMVC5.Models.Service;

namespace LoginAndRegisterASPMVC5.Controllers
{
    public sealed class DatabaseManager
    {
        private static readonly Lazy<SqlConnection> _lazyConnection = new Lazy<SqlConnection>(() => new SqlConnection());
        private static readonly Lazy<SqlConnection> _lazyConnectionUsers = new Lazy<SqlConnection>(() => new SqlConnection());

        private DatabaseManager()
        {
        }

        public static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            SqlConnection connection = _lazyConnection.Value;
            connection.ConnectionString = connectionString;
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }

            return connection;
        }

        public static SqlConnection GetConnectionUsers()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            SqlConnection connection = _lazyConnectionUsers.Value;
            connection.ConnectionString = connectionString;
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }

            return connection;
        }

        public static void CloseConnection()
        {
            if (_lazyConnection.Value.State != System.Data.ConnectionState.Closed)
            {
                _lazyConnection.Value.Close();
            }
        }

        public static void CloseConnectionUsers()
        {
            if (_lazyConnectionUsers.Value.State != System.Data.ConnectionState.Closed)
            {
                _lazyConnectionUsers.Value.Close();
            }
        }
    }

    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<Service> services = new List<Service>();
        public static List<User> _users = new List<User>();
        public static List<User> _withNotif = new List<User>();

        public ActionResult Index()
        {
            if (Session["idUser"] != null)
            {
                FetchUserWithNotif();
                return View();

            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Users()
        {
            if (Session["idUser"] != null)
            {
                FetchUserData();
                return View();

            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public void UpdateEmailNotification(int idUser)
        {
            using (SqlConnection connection = DatabaseManager.GetConnectionUsers())
            {
                using (var command = new SqlCommand("dbo.UpdateUserEmailNotification", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idUser", idUser);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void SendEmail(string message)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings.Get("smtpServer"));

                mail.From = new MailAddress("carillo.aaronjoseph@gmail.com");

                foreach (User user in _withNotif)
                {
                    string emailAddress = user.Email;
                    mail.To.Add(emailAddress);
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
        }

        public void FetchUserWithNotif()
        {
            List<User> users = new List<User>();
            string query = $"SELECT [Email] FROM Users WHERE Email_Notification = 1";

            try
            {
                _withNotif.Clear();
                using (SqlConnection connection = DatabaseManager.GetConnectionUsers())
                using (SqlCommand commandLatestRecord = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = commandLatestRecord.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            users.Add(new User()
                            {
                                Email = reader["Email"].ToString(),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately, e.g. log it, rethrow it, or return an error message to the user.
                throw ex;
            }

            foreach (var user in users)
            {
                _withNotif.Add(user);
            }
        }


        public void ManageServices(string serviceName, string command)
        {
            using (SqlConnection connection = DatabaseManager.GetConnection())
            {
                EventLog eventLog = new EventLog("Application");
                int numEntriesToSearch = int.Parse(ConfigurationManager.AppSettings.Get("NumOfEntries")); // Limit the number of entries to search
                DateTime lastStart = DateTime.MinValue;
                string lastLog = null;

                // Get the collection of entries from the event log
                EventLogEntryCollection entries = eventLog.Entries;

                // Iterate over the last numEntriesToSearch entries in reverse order
                int count = entries.Count;
                for (int i = count - 1; i >= Math.Max(count - numEntriesToSearch, 0); i--)
                {
                    EventLogEntry entry = entries[i];

                    // Check if the entry matches the service we're monitoring and is more recent than the previous one
                    if (entry.Source == serviceName && entry.TimeGenerated > lastStart)
                    {
                        lastStart = entry.TimeGenerated;
                        lastLog = entry.Message;
                    }
                }
                ServiceController[] services = ServiceController.GetServices();
                string actionBy = Session["FullName"].ToString();
                ServiceController sc = services.FirstOrDefault(s => s.ServiceName == serviceName);

                switch (command)
                {
                    case "start":
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            //MessageBox.Show("Service started.");
                            StoreData(connection, serviceName, DateTime.Now, ServiceControllerStatus.Running.ToString(), "Started at SMS", actionBy);
                        }
                        else
                        {
                            //MessageBox.Show("Service is already running.");
                            StoreData(connection, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                        }
                        break;

                    case "stop":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            //MessageBox.Show("Service stopped.");
                            StoreData(connection, serviceName, DateTime.Now, ServiceControllerStatus.Stopped.ToString(), "Stop at SMS", actionBy);
                            SendEmail("Service Name: " + serviceName + "\nLastStart: " + DateTime.Now + "\nStatus: "
                                      + ServiceControllerStatus.Stopped.ToString() + "\nLastLog: Stop at SMS" + "");
                        }
                        else
                        {
                            //MessageBox.Show("Service is already stopped.");
                            StoreData(connection, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                            SendEmail("Service Name: " + serviceName + "\nLastStart: " + lastStart + "\nStatus: "
                                      + ServiceControllerStatus.Stopped.ToString() + "\nLastLog: " + lastLog + "Actionby: " + actionBy);
                        }
                        break;

                    case "restart":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                            sc.Start();
                            //MessageBox.Show("Service restarted.");
                            StoreData(connection, serviceName, DateTime.Now, ServiceControllerStatus.Running.ToString(), "Restarted at SMS", actionBy);
                        }
                        else
                        {
                            //MessageBox.Show("Service is not running.");
                            StoreData(connection, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                        }
                        break;

                    default:
                        MessageBox.Show("Invalid command from " + command);
                        break;
                }
            }
        }

        public void FetchUserData()
        {
            List<User> users = new List<User>();
            string query = $"SELECT [idUser],[FirstName],[LastName],[Email],[Email_Notification] FROM Users";

            try
            {
                _users.Clear();
                using (SqlConnection connection = DatabaseManager.GetConnectionUsers())
                using (SqlCommand commandLatestRecord = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = commandLatestRecord.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            users.Add(new User()
                            {
                                idUser = (int)reader["idUser"],
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Email_Notification = (bool)reader["Email_Notification"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately, e.g. log it, rethrow it, or return an error message to the user.
                throw ex;
            }

            foreach (var user in users)
            {
                _users.Add(user);
            }
        }

        public ActionResult GetUsers()
        {
            FetchUserData();
            return Json(_users, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetServices()
        {
            List<Service> refreshServices = new List<Service>(services); // create a copy of the services list
            services.Clear();
            foreach (Service service in refreshServices)
            {
                FetchData(service.ServiceName);
            }

            return Json(refreshServices, JsonRequestBehavior.AllowGet);
        }


        public void GetLatestRecord(Service fetchInput)//Gets the latest record of a service
        {
            string serviceName = fetchInput.ServiceName;

            // Check if the service already exists in the list
            if (!services.Any(s => s.ServiceName == serviceName))
            {
                FetchData(serviceName);
            }
        }

        [HttpGet]
        public ActionResult GetServiceNames() // For the Service Checkboxes
        {
            ServiceController[] services = ServiceController.GetServices(); // create a copy of the services list
            List<string> checkedServices = new List<string>();

            foreach (ServiceController service in services)
            {
                checkedServices.Add(service.ServiceName.ToString());
            }
            return Json(checkedServices, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMonitoredServices()//For checkboxes as reference
        {
            List<Service> servicesCopy = new List<Service>(services); // create a copy of the services list
            List<string> checkedServices = new List<string>();

            foreach (Service service in servicesCopy)
            {
                checkedServices.Add(service.ServiceName.ToString());
            }
            return Json(checkedServices, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Refresh() //Refeshes the table
        {
            List<Service> refreshServices = new List<Service>(services); // create a copy of the services list
            services.Clear();
            foreach (Service service in refreshServices)
            {
                FetchData(service.ServiceName);
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult AddService(Service fetchInput) //Gets the User Input for adding a service
        {
            string serviceName = fetchInput.ServiceName;

            // Check if the service already exists in the list
            if (!services.Any(s => s.ServiceName == serviceName))
            {
                FetchData(serviceName);
            }

            return RedirectToAction("Index");
        }

        public void FetchData(string serviceName)
        {
            List<Service> newServices = new List<Service>();
            string queryLatestRecord = $"SELECT [ServiceName],[LastStart],[ServiceStatus],[LastLog],[ActionBy] FROM Services WHERE ServiceName = '{serviceName}'";

            try
            {
                using (SqlConnection connection = DatabaseManager.GetConnection())
                using (SqlCommand commandLatestRecord = new SqlCommand(queryLatestRecord, connection))
                {
                    SqlDataReader reader = commandLatestRecord.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            newServices.Add(new Service()
                            {
                                ServiceName = reader["ServiceName"].ToString(),
                                LastStart = reader["LastStart"].ToString(),
                                ServiceStatus = reader["ServiceStatus"].ToString(),
                                LastLog = reader["LastLog"].ToString(),
                                ActionBy = reader["ActionBy"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately, e.g. log it, rethrow it, or return an error message to the user.
                throw ex;
            }

            foreach (var newService in newServices)
            {
                services.Add(newService);
            }
        }


        [HttpPost]
        public ActionResult GetLogHistory(string serviceName)
        {
            string query = $"SELECT * FROM ServiceTB WHERE ServiceName = '{serviceName}' ORDER BY LASTSTART DESC";
            List<Service> services = new List<Service>();

            try
            {
                using (SqlConnection connection = DatabaseManager.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            services.Add(new Service()
                            {
                                ServiceName = reader["ServiceName"].ToString(),
                                LastStart = reader["LastStart"].ToString(),
                                ServiceStatus = reader["ServiceStatus"].ToString(),
                                LastLog = reader["LastLog"].ToString(),
                                ActionBy = reader["ActionBy"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(services, JsonRequestBehavior.AllowGet);
        }

        public void DeleteServices(string serviceName) //Removes a service row
        {
            var serviceToRemove = services.SingleOrDefault(r => r.ServiceName == serviceName);
            if (serviceToRemove != null)
                services.Remove(serviceToRemove);
        }

        static void StoreData(SqlConnection connection, string serviceName, DateTime entryTime, string serviceStatus, string exceptionDetails, string actionBy)
        {
            try
            {
                // Use the singleton SqlConnection instance
                using (var command = new SqlCommand("InsertIntoServiceTB", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@LastStart", entryTime);
                    command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                    command.Parameters.AddWithValue("@LastLog", exceptionDetails);
                    command.Parameters.AddWithValue("@ActionBy", actionBy);

                    command.ExecuteNonQuery();
                }

                // Use the singleton SqlConnection instance
                using (var command = new SqlCommand("InsertIntoServices", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@LastStart", entryTime);
                    command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                    command.Parameters.AddWithValue("@LastLog", exceptionDetails);
                    command.Parameters.AddWithValue("@ActionBy", actionBy);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while executing the query: " + ex.Message);
            }
        }

        public ActionResult Register()
        {
            //if (Session["FullName"] != null) // check if the user is already logged in
            //{
            //    return RedirectToAction("Index", "Home"); // redirect to index page
            //}

            return View();
        }

        //POST: Register

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Register(User _user)
        {
            if (ModelState.IsValid)
            {
                var check = _db.Users.FirstOrDefault(s => s.Email == _user.Email);
                if (check == null)
                {
                    _user.Password = GetMD5(_user.Password);
                    _db.Configuration.ValidateOnSaveEnabled = false;
                    _db.Users.Add(_user);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.error = "Email already exists";
                    return View();
                }


            }
            return View();
        }

        public ActionResult AddUser(User _user)
        {
            if (ModelState.IsValid)
            {
                var check = _db.Users.FirstOrDefault(s => s.Email == _user.Email);
                if (check == null)
                {
                    _user.Password = GetMD5(_user.Password);
                    _db.Configuration.ValidateOnSaveEnabled = false;
                    _db.Users.Add(_user);
                    _db.SaveChanges();
                    return RedirectToAction("Users");
                }
                else
                {
                    ViewBag.error = "Email already exists";
                    return View("Users");
                }


            }
            return View("Index");
        }

        //create a string MD5
        public static string GetMD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = Encoding.UTF8.GetBytes(str);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x2");

            }
            return byte2String;
        }

        /*
         LOGIN
         */

        public ActionResult Login()
        {
            //if (Session["FullName"] != null) // check if the user is already logged in
            //{
            //    return RedirectToAction("Index", "Home"); // redirect to index page
            //}

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin _userlogin)
        {
            if (ModelState.IsValid)
            {
                var f_password = GetMD5(_userlogin.Password);
                var data = _db.Users.Where(s => s.Email.Equals(_userlogin.Email) && s.Password.Equals(f_password)).ToList();
                if (data.Count() > 0)
                {
                    //add session
                    Session["FullName"] = data.FirstOrDefault().FirstName + " " + data.FirstOrDefault().LastName;
                    Session["Email"] = data.FirstOrDefault().Email;
                    Session["idUser"] = data.FirstOrDefault().idUser;
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.error = "Login failed";
                    return RedirectToAction("Login");
                }
            }
            return View();
        }

        //Logout
        public ActionResult Logout()
        {
            Session.Clear();//remove session
            return RedirectToAction("Login");
        }

    }

}

