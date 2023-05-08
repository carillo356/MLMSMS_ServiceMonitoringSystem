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
using System.ServiceProcess;
using System.Data;
using Service = LoginAndRegisterASPMVC5.Models.Service;
using System.Web.UI.WebControls;
using System.Reflection;

namespace LoginAndRegisterASPMVC5.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<User> _users = new List<User>();
        public static List<dynamic> _servicesAvailable;
        public static List<Service> _servicesInMonitor = new List<Service>();
        public static List<dynamic> _servicesNotMonitored;
        private List<Service> _servicesLogsList = new List<Service>();

        public void GetServicesInMonitor()
        {
            _servicesInMonitor.Clear();

            try
            {
                using (SqlConnection connection = GetConnection())
                {
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
                                    PendingCommand = reader["sq_Command"] == DBNull.Value ? null : reader["sq_Command"].ToString(),
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
                // Sort the list by ServiceName
                _servicesInMonitor = _servicesInMonitor.OrderBy(service => service.ServiceName).ToList();
            }
            catch
            {

            }
        }


        [HttpGet]
        public JsonResult ServicesInMonitor()
        {
            try
            {
                GetServicesInMonitor();
                GetServicesAvailable();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, servicesInMonitor = _servicesInMonitor, servicesNotMonitored = _servicesNotMonitored }, JsonRequestBehavior.AllowGet);
        }

        public void GetServicesAvailable()
        {
            if (_servicesAvailable != null)
            {
                _servicesAvailable.Clear();
            }
            using (SqlConnection connection = GetConnection())
            {
                string query = "SELECT sa_ServiceName, sa_HostName FROM ServicesAvailable";
                _servicesAvailable = GetColumnsDynamic(connection, query, reader => new { ServiceName = reader.GetString(0), HostName = reader.GetString(1) });
            }

            // Update the following line based on the anonymous object structure
            _servicesNotMonitored = _servicesAvailable.Where(service => !_servicesInMonitor.Any(s => s.ServiceName == service.ServiceName && s.HostName == service.HostName)).ToList();
        }

        [HttpPost]
        public JsonResult CancelPendingCommand(string serviceName, string hostName)
        {
            try
            {
                // Call the function to cancel the pending command in the database
                CancelPendingCommandInDatabase(serviceName, hostName);

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        private void CancelPendingCommandInDatabase(string serviceName, string hostName)
        {
            // Implement the necessary logic to delete the pending command in the database
            // This function should execute an SQL query to remove the pending command for the specified service and host
            using (SqlConnection connection = GetConnection())
            {
                string query = "DELETE FROM ServicesStartStopQueue WHERE sq_ServiceName = @ServiceName AND sq_HostName = @HostName AND sq_DateExecuted IS NULL";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    command.Parameters.AddWithValue("@HostName", hostName);

                    command.ExecuteNonQuery();
                }
            }
        }


        [HttpPost]
        public JsonResult AddService(string serviceName, string hostName) // Gets the User Input for adding a service
        {
            if (serviceName == null) return Json(new { success = false, errorMessage = $"serviceName is null at " + MethodBase.GetCurrentMethod().Name });

            if (!_servicesInMonitor.Any(s => s.ServiceName == serviceName && s.HostName == hostName))
            {
                try
                {
                    using (SqlConnection connection = GetConnection())
                    {
                        using (SqlCommand command = new SqlCommand("UpdateServiceStatus", connection))
                        {
                            var servicesInstalled = GetTripleColumn(connection, GetServicesStatusQuery("ServicesAvailable", "sa")).Select(t => (ServiceName: t.Item1, ServiceStatus: t.Item2, HostName: t.Item3)).ToArray();
                            var serviceInstalled = servicesInstalled.FirstOrDefault(s => s.ServiceName == serviceName && s.HostName == hostName);

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
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errorMessage = $"{ex.Message}" });
                }
            }
            else
            {
                return Json(new { success = false, errorMessage = $"Service {serviceName} already exists in the monitor for the host {hostName}" });
            }
            return Json(new { success = true });
        }


        public JsonResult DeleteAddedService(string serviceName, string hostName) //Removes a service row
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    using (SqlCommand command = new SqlCommand("DeleteServiceFromMonitored", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ServiceName", serviceName);
                        command.Parameters.AddWithValue("@HostName", hostName);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" });
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetMonitoredServicesCount()
        {
            int totalMonitoredServices = _servicesInMonitor.Count;
            return Json(new { totalMonitoredServices }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTotalUsersCount()
        {
            int totalUsers = _db.Users.Count();
            return Json(new { totalUsers }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLogHistoryCount()
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
            catch
            {
            }

            return columns;
        }

        public static List<dynamic> GetColumnsDynamic(SqlConnection connection, string query, Func<SqlDataReader, dynamic> readColumns)
        {
            var columns = new List<dynamic>();

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
            catch
            {
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

        public bool IsServiceAvailable(string serviceName, string hostName)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    using (SqlCommand command = new SqlCommand("IsServiceAvailable", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ServiceName", serviceName);
                        command.Parameters.AddWithValue("@HostName", hostName);

                        object result = command.ExecuteScalar();
                        return Convert.ToBoolean(result);
                    }
                }
            }
            catch
            {
                return false;
            }
        }   

        [HttpPost]
        public JsonResult GetServiceLogsTB(string serviceName)
        {
            string query = $"SELECT * FROM ServicesLogs WHERE sl_ServiceName = '{serviceName}' ORDER BY sl_LogID DESC";
            _servicesLogsList.Clear();

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
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

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" });
            }

            return Json(new { success = true, servicesLogsList = _servicesLogsList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ServiceAction(string serviceName, string command, string hostName)
        {
            using (SqlConnection connection = GetConnection())
            {
                try
                {
                    if (!IsServiceAvailable(serviceName, hostName))
                    {
                        return Json(new { success = false, errorMessage = $"{serviceName} is not available" });
                    }

                    switch (command)
                    {
                        case "start":
                        case "stop":
                        case "restart":
                            InsertIntoServicesStartStopQueue(serviceName, hostName, command, connection);
                            break;

                        default:
                            break;
                    }

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { errorMessage = $"{ex.Message}" });
                }
            }
        }

        private void InsertIntoServicesStartStopQueue(string serviceName, string hostName, string command, SqlConnection connection)
        {
            string query = @"
            INSERT INTO [dbo].[ServicesStartStopQueue]
                ([sq_ServiceName], [sq_HostName], [sq_Command], [sq_IssuedBy], [sq_DateIssued])
            VALUES
                (@ServiceName, @HostName, @Command, @IssuedBy, GETDATE())";

            using (SqlCommand commandToExecute = new SqlCommand(query, connection))
            {
                commandToExecute.Parameters.AddWithValue("@ServiceName", serviceName);
                commandToExecute.Parameters.AddWithValue("@HostName", hostName);
                commandToExecute.Parameters.AddWithValue("@Command", command);
                commandToExecute.Parameters.AddWithValue("@IssuedBy", HttpContext.Session["IdUser"]); // Make sure to use the correct session variable name

                commandToExecute.ExecuteNonQuery();
            }
        }


        private void SP_UpdateServiceStatus(SqlConnection connection, string serviceName, string serviceStatus, string hostName, string logBy, DateTime lastStart, string lastEventLog)
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
            catch 
            {
            }
        }

        public ActionResult Users()
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
            catch 
            {

            }
        }

        public JsonResult RealTimeUsersTable()
        {
            try
            {
                GetUsersTB();
            }
            catch(Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, users = _users }, JsonRequestBehavior.AllowGet);
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
        public JsonResult AddUser(User _user)
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
            if (ModelState.IsValid)
            {
                var userToUpdate = _db.Users.FirstOrDefault(u => u.IdUser == _user.IdUser);

                if (userToUpdate != null)
                {
                    var newPassword = string.IsNullOrEmpty(Password) ? userToUpdate.Password : GetMD5(Password);

                    if (userToUpdate.FirstName == _user.FirstName &&
                        userToUpdate.LastName == _user.LastName &&
                        userToUpdate.Email == _user.Email &&
                        (string.IsNullOrEmpty(Password) || newPassword == userToUpdate.Password) &&
                        userToUpdate.IsAdmin == _user.IsAdmin)
                    {
                        return Json(new { success = true, message = "No changes" });
                    }

                    try
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
                    catch 
                    {
                        // If any exception occurs during the update process, return the "unsuccessfully updated" message
                        return Json(new { success = false, message = "User info update unsuccessfully!" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "User not found!" });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                              .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = errors });
        }

        [HttpPost]
        public ActionResult UpdateUserPassword(UserUpdatePassword _user, string currentPassword, string newPassword)
        {
            if (ModelState.IsValid)
            {
                var userToUpdate = _db.Users.FirstOrDefault(u => u.IdUser == _user.IdUser);

                if (userToUpdate != null)
                {
                    if (GetMD5(currentPassword) != userToUpdate.Password)
                    {
                        return Json(new { success = false, message = "Current password is incorrect!" });
                    }

                    try
                    {
                        // Update the user's password if it is not null
                        if (!string.IsNullOrEmpty(newPassword))
                        {
                            userToUpdate.Password = GetMD5(newPassword);
                        }

                        _db.Configuration.ValidateOnSaveEnabled = false;
                        _db.Entry(userToUpdate).State = EntityState.Modified;
                        _db.SaveChanges();

                        return Json(new { success = true, message = "User info updated successfully!" });
                    }
                    catch
                    {
                        // If any exception occurs during the update process, return the "unsuccessfully updated" message
                        return Json(new { success = false, message = "User info update unsuccessfully!" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "User not found!" });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                              .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = errors });
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
            try
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
                        return View("Index");
                    }
                    else
                    {
                        // If email or password is incorrect
                        ViewBag.validation = "Email/Password is incorrect";
                        return View("Login");
                    }
                }
            }
            catch
            {
                ViewBag.error = "Login failed";
                return View("Login");
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
            return View("Login");
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

