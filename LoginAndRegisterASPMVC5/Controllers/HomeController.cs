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
using System.Runtime.Remoting.Contexts;
using System.Data.Entity.Validation;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;

namespace LoginAndRegisterASPMVC5.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<Service> _activeServices = new List<Service>();
        public static List<User> _users = new List<User>();

        public ActionResult Index()
        {
            //InsertAllServices();
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
        public void AddService(Service GetInput) //Gets the User Input for adding a service
        {

            string serviceName = GetInput.ServiceName;
            if (!_activeServices.Any(s => s.ServiceName == serviceName))
            {
                GetServicesTB(serviceName);
            }
        }

        public void RemoveAddedService(string serviceName) //Removes a service row
        {
            var serviceToRemove = _activeServices.SingleOrDefault(r => r.ServiceName == serviceName);
            if (serviceToRemove != null)
                _activeServices.Remove(serviceToRemove);
        }

        public void GetServicesTB(string serviceName)
        {
            try
            {
                using (SqlConnection connection = DatabaseManager.GetConnection())
                using (SqlCommand command = new SqlCommand("GetServiceLogsByServiceName", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            _activeServices.Add(new Service()
                            {
                                ServiceName = reader["sl_ServiceName"].ToString(),
                                LastStart = reader["sl_LastStart"].ToString(),
                                ServiceStatus = reader["sl_ServiceStatus"].ToString(),
                                LastEventLog = reader["sl_LastEventLog"].ToString(),
                                HostName = reader["sl_HostName"].ToString()
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
        public ActionResult GetServiceLogsTB(string serviceName)
        {
            string query = $"SELECT * FROM ServicesLogs WHERE sl_ServiceName = '{serviceName}' ORDER BY sl_LastStart DESC";
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
                            ServiceName = reader["sl_ServiceName"].ToString(),
                            LastStart = reader["sl_LastStart"].ToString(),
                            ServiceStatus = reader["sl_ServiceStatus"].ToString(),
                            LastEventLog = reader["sl_LastEventLog"].ToString(),
                            HostName = reader["sl_HostName"].ToString()
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
            string hostName = Session["FullName"].ToString();
            DateTime lastStart = new DateTime(1900, 1, 1);
            string lastEventLog = "No Record";
            string logBy = "Unknown";

            try
            {
                logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                string queryString = $"*[System/Provider/@Name='{serviceName}']";
                EventLogQuery eventLogQuery = new EventLogQuery("Application", PathType.LogName, queryString);
                EventLogReader eventLogReader = new EventLogReader(eventLogQuery);

                int numEntriesToSearch = int.Parse(ConfigurationManager.AppSettings.Get("NumOfEntries"));
                int entriesSearched = 0;
                EventRecord latestEvent = null;

                EventRecord currentEvent = eventLogReader.ReadEvent();
                while (currentEvent != null && entriesSearched < numEntriesToSearch)
                {
                    if (latestEvent == null || currentEvent.TimeCreated > latestEvent.TimeCreated)
                    {
                        latestEvent = currentEvent;
                    }
                    entriesSearched++;
                    currentEvent = eventLogReader.ReadEvent();
                }

                // Get the necessary information from the latest entry (if one was found)
                if (latestEvent != null)
                {
                    lastStart = (DateTime)latestEvent.TimeCreated;
                    lastEventLog = latestEvent.Properties[0].Value.ToString(); // Assumes log message is in the first property
                }
            }
            catch (Exception ex)
            { 
                throw ex;
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
                            SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Running.ToString(), hostName, logBy, DateTime.Now, "Started at SMS");
                        }
                        else
                        {
                            //MessageBox.Show("Service is already running.");
                            SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
                        }
                        break;

                    case "stop":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            //MessageBox.Show("Service stopped.");
                            SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Stopped.ToString(), hostName, logBy, DateTime.Now, "Stop at SMS");
                        }
                        else
                        {
                            //MessageBox.Show("Service is already stopped.");
                            SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
                        }
                        break;

                    case "restart":
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                            sc.Start();
                            //MessageBox.Show("Service restarted.");
                            SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Running.ToString(), hostName, logBy, DateTime.Now, "Restarted at SMS");
                        }
                        else
                        {
                            //MessageBox.Show("Service is not running.");
                            SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
                        }
                        break;

                    default:
                        SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
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
                GetServicesTB(service.ServiceName);
            }

            return Json(_activeServices, JsonRequestBehavior.AllowGet);
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
                throw ex;
            }
        }

        public ActionResult Users()
        {
            if (Session["idUser"] != null)
            {

                bool isAdmin = false;
                if (Session["IsAdmin"] != null)
                {
                    isAdmin = (bool)Session["IsAdmin"];
                }

                if (isAdmin)
                {
                    return RedirectToAction("AdminUsers");
                }
                else
                {
                    return View();

                }

            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult AdminUsers()
        {
            return View();
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

        public void DeleteUser(int idUser)
        {
            using (SqlConnection connection = DatabaseManager.GetConnectionUsers())
            {
                using (var command = new SqlCommand("dbo.DeleteUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idUser", idUser);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void GetUsersTB()
        {
            string query = $"SELECT [idUser],[FirstName],[LastName],[Email],[Email_Notification],[IsAdmin] FROM Users";

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
                                Email_Notification = (bool)reader["Email_Notification"],
                                IsAdmin = (bool)reader["IsAdmin"]
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

        public ActionResult RealTimeUsersTable()
        {
            GetUsersTB();
            return Json(_users, JsonRequestBehavior.AllowGet);
        }

        static void StoreServiceName(SqlConnection connection, string serviceName)
        {
            try
            {
                using (var command = new SqlCommand("INSERT INTO Services (ServiceName) SELECT @ServiceName WHERE NOT EXISTS (SELECT 1 FROM Services WHERE ServiceName = @ServiceName)", connection))
                {
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void InsertAllServices()
        {
            using (SqlConnection connection = DatabaseManager.GetConnection())
            {
                // Get a list of all the installed services
                var services = ServiceController.GetServices();

                // Insert each service name into the Services table
                foreach (var service in services)
                {
                    StoreServiceName(connection, service.ServiceName);
                }
            }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                }


            }
            return View("AdminUsers");
        }

        [HttpPost]
        public ActionResult UpdateUser(UserUpdate _user, string Password)
        {
            // Retrieve the user from the database
            var userToUpdate = _db.Users.FirstOrDefault(u => u.idUser == _user.idUser);

            if (ModelState.IsValid)
            {
                if (userToUpdate != null)
                {
                    // Update the user's properties
                    userToUpdate.FirstName = _user.FirstName;
                    userToUpdate.LastName = _user.LastName;
                    userToUpdate.Email = _user.Email;
                    userToUpdate.IsAdmin = _user.IsAdmin;

                    // Update the user's password if it is not null
                    if (!string.IsNullOrEmpty(Password))
                    {
                        userToUpdate.Password = GetMD5(Password);

                    }
                    _db.Configuration.ValidateOnSaveEnabled = false;
                    _db.Entry(userToUpdate).State = EntityState.Modified;
                    _db.SaveChanges();

                    return RedirectToAction("AdminUsers");
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return View("AdminUsers");
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
                    Session["FirstName"] = data.FirstOrDefault().FirstName;
                    Session["LastName"] = data.FirstOrDefault().LastName;
                    Session["Email"] = data.FirstOrDefault().Email;
                    Session["idUser"] = data.FirstOrDefault().idUser;
                    Session["IsAdmin"] = data.FirstOrDefault().IsAdmin;
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