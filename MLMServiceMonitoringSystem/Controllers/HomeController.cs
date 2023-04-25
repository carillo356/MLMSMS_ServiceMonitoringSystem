using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using LoginAndRegisterASPMVC5.Models;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Data;
using Service = LoginAndRegisterASPMVC5.Models.Service;
using System.Web.UI.WebControls;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics.Eventing.Reader;
using System.IO;

namespace LoginAndRegisterASPMVC5.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<User> _users = new List<User>();
        public static List<string> _servicesAvailable;
        public static List<Service> _servicesInMonitor = new List<Service>();
        private List<Service> _servicesLogsList = new List<Service>();

        [HttpPost]
        public void AddService(Service GetInput) //Gets the User Input for adding a service
        {
            string serviceName = GetInput.ServiceName;
            if (serviceName == null) return;

            if (!_servicesInMonitor.Any(s => s.ServiceName == serviceName))
            {
                try
                {

                    using (SqlConnection connection = GetConnection())
                    using (SqlCommand command = new SqlCommand("UpdateServiceStatus", connection))
                    {
                        var servicesInstalled = GetTripleColumn(connection, GetServicesStatusQuery("ServicesAvailable", "sa")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();
                        var serviceInstalled = servicesInstalled.FirstOrDefault(s => s.ServiceName == serviceName);

                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ServiceName", serviceInstalled.ServiceName);
                        command.Parameters.AddWithValue("@ServiceStatus", serviceInstalled.ServiceStatus);
                        command.Parameters.AddWithValue("@HostName", serviceInstalled.HostName);
                        command.Parameters.AddWithValue("@LogBy", Session["FullName"]?.ToString() ?? "NotFound");
                        command.Parameters.AddWithValue("@LastStart", new DateTime(1900, 1, 1));
                        command.Parameters.AddWithValue("@LastEventLog", "NotFound");
                        command.ExecuteNonQuery();
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public ActionResult GetAddedServices()//For checkboxes as reference
        {
            List<string> addedServicesList = new List<string>();

            foreach (Service service in _servicesInMonitor)
            {
                addedServicesList.Add(service.ServiceName.ToString());
            }
            return Json(addedServicesList, JsonRequestBehavior.AllowGet);
        }

        public void DeleteAddedService(string serviceName) //Removes a service row
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                using (SqlCommand command = new SqlCommand("DeleteServiceFromMonitored", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ServiceName", serviceName);

                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public ActionResult GetMonitoredServicesCount()
        {
            int totalMonitoredServices = _servicesInMonitor.Count;
            return Json(new { totalMonitoredServices }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetTotalUsersCount()
        {
            int totalUsers = _db.Users.Count();
            return Json(new { totalUsers }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetLogHistoryCount()
        {
            int totalLogHistory = _servicesLogsList.Count;
            return Json(new { totalLogHistory }, JsonRequestBehavior.AllowGet);
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
                throw ex;
            }

            return columns;
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

        public static string GetServicesStatusQuery(string tableName, string columnNamePrefix)
        {
            return $"SELECT [{columnNamePrefix}_ServiceName], [{columnNamePrefix}_ServiceStatus], [{columnNamePrefix}_HostName] FROM {tableName}";
        }

        public void GetServicesInMonitor()
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                using (SqlCommand command = new SqlCommand("GetLatestLogsForAllServices", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            _servicesInMonitor.Add(new Service()
                            {
                                ServiceName = reader["sm_ServiceName"].ToString(),
                                LastStart = reader["sl_LastStart"] == DBNull.Value ? "" : reader["sl_LastStart"].ToString(),
                                ServiceStatus = reader["sl_ServiceStatus"] == DBNull.Value ? "" : reader["sl_ServiceStatus"].ToString(),
                                LastEventLog = reader["sl_LastEventLog"] == DBNull.Value ? "" : reader["sl_LastEventLog"].ToString(),
                                HostName = reader["sl_HostName"] == DBNull.Value ? "" : reader["sl_HostName"].ToString(),
                                LogBy = reader["sl_LogBy"] == DBNull.Value ? "" : reader["sl_LogBy"].ToString(),
                                Description = reader["sa_Description"] == DBNull.Value ? "" : reader["sa_Description"].ToString(),
                                StartupType = reader["sa_StartupType"] == DBNull.Value ? "" : reader["sa_StartupType"].ToString(),
                                LogOnAs = reader["sa_LogOnAs"] == DBNull.Value ? "" : reader["sa_LogOnAs"].ToString()
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
        public ActionResult ServicesInMonitor()
        { 
            _servicesInMonitor.Clear();
            if(_servicesInMonitor.Count == 0)
            {
                GetServicesInMonitor();
            }
            return Json(_servicesInMonitor, JsonRequestBehavior.AllowGet);
        }

        public void GetServicesAvailable()
        {
            if(_servicesAvailable != null)
            {
                _servicesAvailable.Clear();
            }
            using (SqlConnection connection = GetConnection())
            {
                _servicesAvailable = GetSingleColumn(connection, "GetServicesAvailable");
            }
        }
        [HttpPost]
        public ActionResult GetServicesInController()
        {
            // Get the list of available services from the database
            GetServicesAvailable();

            // Filter out the services that are already being monitored
            List<string> servicesToDisplay = _servicesAvailable.Where(service => !_servicesInMonitor.Any(s => s.ServiceName == service)).ToList();

            return Json(servicesToDisplay);
        }

        [HttpPost]
        public ActionResult GetServiceLogsTB(string serviceName)
        {
            string query = $"SELECT * FROM ServicesLogs WHERE sl_ServiceName = '{serviceName}' ORDER BY sl_LogID DESC";
            _servicesLogsList.Clear();

            try
            {
                using (SqlConnection connection = GetConnection())
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _servicesLogsList.Add(new Service()
                        {
                            ServiceName = reader["sl_ServiceName"].ToString(),
                            LastStart = reader["sl_LastStart"].ToString(),
                            ServiceStatus = reader["sl_ServiceStatus"].ToString(),
                            LastEventLog = reader["sl_LastEventLog"].ToString(),
                            HostName = reader["sl_HostName"].ToString(),
                            LogBy = reader["sl_LogBy"].ToString()
                        });

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(_servicesLogsList, JsonRequestBehavior.AllowGet);
        }

        public void ServiceAction(string serviceName, string command)
        {
            string hostName = Environment.MachineName;
            DateTime lastStart = new DateTime(1900, 1, 1);
            string lastEventLog = "Service is already ";
            string logBy = Session["FullName"].ToString() ?? "NotFound";



            using (SqlConnection connection = GetConnection())
            {
                List<string> ServicesAvailable = GetSingleColumn(connection, "GetServicesAvailable");

                string sa_ServiceName = ServicesAvailable.FirstOrDefault(s => s == serviceName);

                ServiceController sc = new ServiceController(sa_ServiceName);

                if (sa_ServiceName != null)
                {
                    switch (command)
                    {
                        case "start":
                            if (sc.Status == ServiceControllerStatus.Stopped)
                            {
                                sc.Start();
                                SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Running.ToString(), hostName, logBy, DateTime.Now, "Started at SMS");
                            }
                            else
                            {
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, "NotFound", lastStart, lastEventLog + sc.Status.ToString());
                            }
                            break;

                        case "stop":
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                sc.Stop();
                                SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Stopped.ToString(), hostName, logBy, DateTime.Now, "Stop at SMS");
                            }
                            else
                            {
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, "NotFound", lastStart, lastEventLog + sc.Status.ToString());
                            }
                            break;

                        case "restart":
                            if (sc.Status == ServiceControllerStatus.Running)
                            {
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                                sc.Start();
                                SP_UpdateServiceStatus(connection, serviceName, ServiceControllerStatus.Running.ToString(), hostName, logBy, DateTime.Now, "Restarted at SMS");
                            }
                            else
                            {
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, "NotFound", lastStart, lastEventLog + sc.Status.ToString());
                            }
                            break;

                        default:
                            SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, "NotFound", lastStart, lastEventLog + sc.Status.ToString());
                            break;
                    }
                }

                else throw new Exception($"Service {serviceName} is not available");

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
                throw ex;
            }
        }

        public ActionResult Users()
        {
            if (Session["IdUser"] != null)
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
            if (Session["IdUser"] != null)
            {
                return View();

            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public void UpdateEmailNotification(int IdUser)
        {
            using (SqlConnection connection = GetConnection())
            {
                using (var command = new SqlCommand("dbo.UpdateUserEmailNotification", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdUser", IdUser);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteUser(int IdUser)
        {
            using (SqlConnection connection = GetConnection())
            {
                using (var command = new SqlCommand("dbo.DeleteUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdUser", IdUser);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void GetUsersTB()
        {
            string query = $"SELECT [IdUser],[FirstName],[LastName],[Email],[Email_Notification],[IsAdmin] FROM Users";

            try
            {
                _users.Clear();
                using (SqlConnection connection = GetConnection())
                using (SqlCommand commandLatestRecord = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = commandLatestRecord.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            _users.Add(new User()
                            {
                                IdUser = (int)reader["IdUser"],
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

        //create a string MD5
        public static string GetMD5(string str)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] fromData = Encoding.Unicode.GetBytes(str); // Use UTF-16 encoding
                byte[] targetData = md5.ComputeHash(fromData);
                StringBuilder byte2String = new StringBuilder();

                for (int i = 0; i < targetData.Length; i++)
                {
                    byte2String.Append(targetData[i].ToString("x2"));
                }
                return byte2String.ToString();
            }
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

                    // Return success message as JSON
                    return Json(new { success = true, message = "User added successfully." });
                }
                else
                {
                    // Return error message as JSON
                    return Json(new { success = false, message = "Email already exists." });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                              .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = errors });
        }



        [HttpPost]
        public ActionResult UpdateUser(UserUpdate _user, string Password)
        {
            // Retrieve the user from the database
            var userToUpdate = _db.Users.FirstOrDefault(u => u.IdUser == _user.IdUser);

            if (ModelState.IsValid)
            {
                if (userToUpdate.FirstName == _user.FirstName && userToUpdate.LastName == _user.LastName && userToUpdate.Email == _user.Email && userToUpdate.IsAdmin == _user.IsAdmin)
                {
                    return Json(new { success = true, message = "No changes" });
                }
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

                    return Json(new { success = true, message = "User info updated successfully!" });
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return View("AdminUsers");
        }

        public ActionResult Index()
        {

            if (Session["IdUser"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Login()
        {
            //InsertAllServices();
            if (Session["FullName"] != null) // check if the user is already logged in
            {
                return RedirectToAction("Index", "Home"); // redirect to index page
            }
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
                    Session["IdUser"] = data.FirstOrDefault().IdUser;
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

        public ActionResult LoginWithSSOToken(string ssoToken)
        {
            if (string.IsNullOrEmpty(ssoToken))
            {
                return RedirectToAction("Login");
            }

            // Validate the ssoToken, for example by checking it against your database
            // If the token is valid, get the corresponding user email
            string userEmail = ValidateSSOTokenAndGetUserEmail(ssoToken);

            if (!string.IsNullOrEmpty(userEmail))
            {
                var user = _db.Users.SingleOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    // Add session values
                    Session["FullName"] = user.FirstName + " " + user.LastName;
                    Session["FirstName"] = user.FirstName;
                    Session["LastName"] = user.LastName;
                    Session["Email"] = user.Email;
                    Session["IdUser"] = user.IdUser;
                    Session["IsAdmin"] = user.IsAdmin;

                    DeleteServiceToken(ssoToken);
                    return RedirectToAction("Index");
                }
            }

            ViewBag.error = "Invalid SSO token";
            return RedirectToAction("Login");
        }


        private string ValidateSSOTokenAndGetUserEmail(string ssoToken)
        {

            using (SqlConnection connection = GetConnection())
            {
                using (var command = new SqlCommand("GetUserEmailBySSOToken", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Token", ssoToken);

                    SqlParameter emailParam = new SqlParameter("@Email", SqlDbType.NVarChar, 100);
                    emailParam.Direction = ParameterDirection.Output;
                    command.Parameters.Add(emailParam);

                    command.ExecuteNonQuery();

                    string userEmail = emailParam.Value as string;
                    return userEmail;
                }
            }
        }

        private void DeleteServiceToken(string ssoToken)
        {
            using (SqlConnection connection = GetConnection())
            {
                using (var command = new SqlCommand("DeleteServiceToken", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Token", ssoToken);

                    command.ExecuteNonQuery();
                }
            }
        }

        //Logout
        public ActionResult Logout()
        {
            Session.Clear();//remove session
            return RedirectToAction("Login");
        }

        public static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            SqlConnection connection = new SqlConnection(connectionString);
            connection.ConnectionString = connectionString;
            connection.Open();
            return connection;
        }
    }
}

