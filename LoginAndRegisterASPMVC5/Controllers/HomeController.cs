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
using System.Web.UI.WebControls;

namespace LoginAndRegisterASPMVC5.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<Service> _activeServices = new List<Service>();
        public static List<User> _users = new List<User>();

        public ActionResult Index()
        {
            if (Session["idUser"] != null)
            {
                return View();

            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        public void AddService(Service fetchInput) //Gets the User Input for adding a service
        {
            string serviceName = fetchInput.ServiceName;
            if (!_activeServices.Any(s => s.ServiceName == serviceName))
            {
                FetchServicesTB(serviceName);
            }
        }

        public void RemoveAddedService(string serviceName) //Removes a service row
        {
            var serviceToRemove = _activeServices.SingleOrDefault(r => r.ServiceName == serviceName);
            if (serviceToRemove != null)
                _activeServices.Remove(serviceToRemove);
        }

        public void FetchServicesTB(string serviceName)
        {
            string query = $"SELECT [ServiceName],[LastStart],[ServiceStatus],[LastLog],[ActionBy] FROM Services WHERE ServiceName = '{serviceName}'";

            try
            {
                using (SqlConnection connection = DatabaseManager.GetConnection())
                using (SqlCommand commandLatestRecord = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = commandLatestRecord.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            _activeServices.Add(new Service()
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
        }

        [HttpGet]
        public ActionResult GetServicesInController() // For the Service Checkboxes
        {
            ServiceController[] servicesInController = ServiceController.GetServices(); // create a copy of the services list
            List<string> servicesInControllerList = new List<string>();

            foreach (ServiceController serviceInController in servicesInController)
            {
                servicesInControllerList.Add(serviceInController.ServiceName.ToString());
            }
            return Json(servicesInControllerList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAddedServices()//For checkboxes as reference
        {
            List<string> addedServicesList = new List<string>();

            foreach (Service service in _activeServices)
            {
                addedServicesList.Add(service.ServiceName.ToString());
            }
            return Json(addedServicesList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult FetchServiceLogsTB(string serviceName)
        {
            string query = $"SELECT * FROM ServiceLogs WHERE ServiceName = '{serviceName}' ORDER BY LASTSTART DESC";
            List<Service> servicesLogsList = new List<Service>();

            try
            {
                using (SqlConnection connection = DatabaseManager.GetConnection())
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        servicesLogsList.Add(new Service()
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
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(servicesLogsList, JsonRequestBehavior.AllowGet);
        }

        public void ServiceAction(string serviceName, string command)
        {
            string actionBy = Session["FullName"].ToString();
            string logBy = null;
            DateTime lastStart = DateTime.MinValue;
            string lastLog = null;

            EventLog eventLog = new EventLog("Application");
            int numEntriesToSearch = int.Parse(ConfigurationManager.AppSettings.Get("NumOfEntries")); // Limit the number of entries to search
            EventLogEntryCollection entries = eventLog.Entries;

            int totalEntries = entries.Count;
            int startIndex = totalEntries - 1;
            int endIndex = Math.Max(startIndex - numEntriesToSearch, 0);
            bool entryFound = false;

            while (!entryFound && startIndex >= 0)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    EventLogEntry entry = entries[i];

                    if (entry.Source == serviceName && entry.TimeGenerated > lastStart)
                    {
                        logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        lastStart = entry.TimeGenerated;
                        lastLog = entry.Message;
                        entryFound = true;
                        break;
                    }
                }

                startIndex = endIndex - 1;
                endIndex = Math.Max(startIndex - numEntriesToSearch, 0);
            }

            if (!entryFound)
            {
                throw new Exception("Target entry not found in event log.");
            }

            using (SqlConnection connection = DatabaseManager.GetConnection())
            {
                ServiceController[] services = ServiceController.GetServices();
                ServiceController sc = services.FirstOrDefault(s => s.ServiceName == serviceName);

                switch (command)
                {
                    case "start":
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            //MessageBox.Show("Service started.");
                            StoreData(connection, logBy, serviceName, DateTime.Now, ServiceControllerStatus.Running.ToString(), "Started at SMS", actionBy);
                        }
                        else
                        {
                            //MessageBox.Show("Service is already running.");
                            StoreData(connection, logBy, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                        }
                        break;

                    case "stop":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            //MessageBox.Show("Service stopped.");
                            StoreData(connection, logBy, serviceName, DateTime.Now, ServiceControllerStatus.Stopped.ToString(), "Stop at SMS", actionBy);
                        }
                        else
                        {
                            //MessageBox.Show("Service is already stopped.");
                            StoreData(connection, logBy, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                        }
                        break;

                    case "restart":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                            sc.Start();
                            //MessageBox.Show("Service restarted.");
                            StoreData(connection, logBy, serviceName, DateTime.Now, ServiceControllerStatus.Running.ToString(), "Restarted at SMS", actionBy);
                        }
                        else
                        {
                            //MessageBox.Show("Service is not running.");
                            StoreData(connection, logBy, serviceName, lastStart, sc.Status.ToString(), lastLog, actionBy);
                        }
                        break;

                    default:
                        //MessageBox.Show("Invalid command from " + command);
                        break;
                }
            }
        }

        public ActionResult RealTimeTable()
        {
            List<Service> _activeServicesCopy = new List<Service>(_activeServices); // create a copy of the services list
            _activeServices.Clear();
            foreach (Service service in _activeServicesCopy)
            {
                FetchServicesTB(service.ServiceName);
            }

            return Json(_activeServices, JsonRequestBehavior.AllowGet);
        }

        static void StoreData(SqlConnection connection, string logBy, string serviceName, DateTime lastStart, string serviceStatus, string lastLog, string actionBy)
        {
            try
            {
                using (var command = new SqlCommand("InsertIntoServices", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@LogBy", logBy);
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@LastStart", lastStart);
                    command.Parameters.AddWithValue("@ServiceStatus", serviceStatus);
                    command.Parameters.AddWithValue("@LastLog", lastLog);
                    command.Parameters.AddWithValue("@ActionBy", actionBy);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult Users()
        {
            if (Session["idUser"] != null)
            {
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
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idUser", idUser);

                    command.ExecuteNonQuery();
                }
            }
        }

        //public void FetchUserWithNotif()
        //{
        //    List<User> users = new List<User>();
        //    string query = $"SELECT [Email] FROM Users WHERE Email_Notification = 1";

        //    try
        //    {
        //        _withNotif.Clear();
        //        using (SqlConnection connection = DatabaseManager.GetConnectionUsers())
        //        using (SqlCommand commandLatestRecord = new SqlCommand(query, connection))
        //        {
        //            SqlDataReader reader = commandLatestRecord.ExecuteReader();

        //            if (reader.HasRows)
        //            {
        //                while (reader.Read())
        //                {
        //                    users.Add(new User()
        //                    {
        //                        Email = reader["Email"].ToString(),
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle the exception appropriately, e.g. log it, rethrow it, or return an error message to the user.
        //        throw ex;
        //    }

        //    foreach (var user in users)
        //    {
        //        _withNotif.Add(user);
        //    }
        //}

        public void FetchUsersTB()
        {
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
                            _users.Add(new User()
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
                throw ex;
            }
        }

        public ActionResult GetUsers()
        {
            FetchUsersTB();
            return Json(_users, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Register()
        {
            if (Session["FullName"] != null) // check if the user is already logged in
            {
                return RedirectToAction("Index", "Home"); // redirect to index page
            }

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

}