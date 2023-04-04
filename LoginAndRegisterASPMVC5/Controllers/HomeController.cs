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

namespace LoginAndRegisterASPMVC5.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        public static List<User> _users = new List<User>();
        public static List<string> _servicesAvailable;
        private List<Service> _servicesLogsList = new List<Service>();
        public static List<Service> _servicesInMonitor = new List<Service>();

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

            RemoveUnmonitoredTasks();
        }

        [HttpPost]
        public void AddService(Service GetInput) //Gets the User Input for adding a service
        {
            string serviceName = GetInput.ServiceName;
            if (serviceName == null) return;  

            if (!_servicesInMonitor.Any(s => s.ServiceName == serviceName))
            {
                try
                {
                    GetServiceLogs(serviceName, out string serviceStatus, out string hostName, out DateTime lastStart, out string lastEventLog);

                    using (SqlConnection connection = GetConnection())
                    using (SqlCommand command = new SqlCommand("UpdateServiceStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ServiceName", serviceName);
                        command.Parameters.AddWithValue("@ServiceStatus", serviceStatus ?? "NotFound");
                        command.Parameters.AddWithValue("@HostName", hostName ?? "NotFound");
                        command.Parameters.AddWithValue("@LogBy", Session["FullName"]?.ToString() ?? "NotFound");
                        command.Parameters.AddWithValue("@LastStart", lastStart);
                        command.Parameters.AddWithValue("@LastEventLog", string.IsNullOrEmpty(lastEventLog) ? "NotFound" : lastEventLog);   
                        command.ExecuteNonQuery();
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Check if a task with the given name already exists
                if (!TaskExists($"Watcher {serviceName}"))
                {
                    // If not, create a task for the service
                    //CreateTasksForMonitoredServices(serviceName);
                }
                // Call RemoveUnmonitoredTasks method
                RemoveUnmonitoredTasks();
            }
        }

        public static void GetServiceLogs(string serviceName, out string serviceStatus, out string hostName, out DateTime lastStart, out string lastEventLog)
        {
            ServiceController[] servicesInController = ServiceController.GetServices();
            ServiceController serviceInController = servicesInController.FirstOrDefault(ctr => ctr.ServiceName == serviceName);

            // Default values
            serviceStatus = "NotFound";
            hostName = "NotFound";
            lastStart = new DateTime(1900, 1, 1);
            lastEventLog = "NotFound";

            if (serviceInController != null)
            {
                serviceStatus = serviceInController.Status.ToString();
                hostName = Environment.MachineName;

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
                    throw ex;
                }
            }
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

        public static void CreateTasksForMonitoredServices(string serviceName)
        {
            using (TaskService ts = new TaskService())
            {
                string taskName = $"Watcher {serviceName}";
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = $"Task to monitor the {serviceName} service";

                td.Triggers.Add(new EventTrigger
                {
                    Subscription = $"<QueryList><Query Id='0' Path='Application'><Select Path='Application'>*[System[Provider[@Name='{serviceName}']]]</Select></Query></QueryList>",
                    Enabled = true
                });

                string loggerPath = ConfigurationManager.AppSettings.Get("LoggerPath");

                // Add escaped double quotes around the serviceName
                td.Actions.Add(new ExecAction(loggerPath, $"\"{serviceName}\""));

                td.Principal.RunLevel = TaskRunLevel.Highest;
                ts.RootFolder.RegisterTaskDefinition(taskName, td);
            }
        }
            
        private void RemoveUnmonitoredWatcherTasks((string ServiceName, string ServiceStatus)[] servicesInMonitor)
        {
            using (TaskService ts = new TaskService())
            {
                // Get all the tasks in the RootFolder
                var allTasks = ts.RootFolder.AllTasks;

                // Iterate through each task
                foreach (Task task in allTasks)
                {
                    // Check if the task starts with "Watcher "
                    if (task.Name.StartsWith("Watcher "))
                    {
                        // Extract the service name from the task name
                        string serviceName = task.Name.Substring("Watcher ".Length);

                        // Check if the service is not in the monitored list
                        if (!servicesInMonitor.Any(s => s.ServiceName == serviceName))
                        {
                            // If not in the list, delete the task
                            ts.RootFolder.DeleteTask(task.Name);
                        }
                    }
                }
            }
        }

        public void RemoveUnmonitoredTasks()
        {
            using (SqlConnection connection = GetConnection())
            {
                if (connection == null) return;

                // Get the services to monitor from the database
                (string ServiceName, string ServiceStatus)[] servicesInMonitor = GetDoubleColumn(connection, "GetServicesStatus").ToArray();

                // Remove tasks for unmonitored services
                RemoveUnmonitoredWatcherTasks(servicesInMonitor);
            }
        }

        private static bool TaskExists(string taskName)
        {
            using (TaskService taskService = new TaskService())
            {
                Task task = taskService.GetTask(taskName);
                return task != null;
            }
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
                                HostName = reader["sl_HostName"] == DBNull.Value ? "" : reader["sl_HostName"].ToString()
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

        public ActionResult GetAddedServices()//For checkboxes as reference
        {
            List<string> addedServicesList = new List<string>();

            foreach (Service service in _servicesInMonitor)
            {
                addedServicesList.Add(service.ServiceName.ToString());
            }
            return Json(addedServicesList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GetServiceLogsTB(string serviceName)
        {
            string query = $"SELECT * FROM ServicesLogs WHERE sl_ServiceName = '{serviceName}' ORDER BY sl_LastStart DESC";
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
                            HostName = reader["sl_HostName"].ToString()
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

        private void GetServiceEventLog(string serviceName, out DateTime lastStart, out string lastEventLog, out string hostName)
        {
            lastStart = new DateTime(1900, 1, 1);
            lastEventLog = "No Record";
            hostName = "Unknown";

            try
            {
                using (var eventLog = new EventLog("Application"))
                {
                    var serviceEvent = eventLog.Entries
                        .Cast<EventLogEntry>()
                        .Where(entry => entry.Source == serviceName)
                        .OrderByDescending(entry => entry.TimeGenerated)
                        .FirstOrDefault();

                    if (serviceEvent != null)
                    {
                        lastStart = serviceEvent.TimeGenerated;
                        lastEventLog = serviceEvent.Message;
                        hostName = serviceEvent.MachineName;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void ServiceAction(string serviceName, string command)
        {
            string hostName = Session["FullName"].ToString();
            DateTime lastStart = new DateTime(1900, 1, 1);
            string lastEventLog = "No Record";
            string logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;



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
                                GetServiceEventLog(serviceName, out lastStart, out lastEventLog, out hostName);
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
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
                                GetServiceEventLog(serviceName, out lastStart, out lastEventLog, out hostName);
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
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
                                GetServiceEventLog(serviceName, out lastStart, out lastEventLog, out hostName);
                                SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
                            }
                            break;

                        default:
                            GetServiceEventLog(serviceName, out lastStart, out lastEventLog, out hostName);
                            SP_UpdateServiceStatus(connection, serviceName, sc.Status.ToString(), hostName, logBy, lastStart, lastEventLog);
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

        public void InsertAllServices()
        {
            GetServicesAvailable();
            using (SqlConnection connection = GetConnection())
                // Insert each service name into the Services table
                foreach (var service in _servicesAvailable)
                {
                    StoreServiceName(connection, service);
                }

        }

        static void StoreServiceName(SqlConnection connection, string serviceName)
        {
            try
            {
                using (var command = new SqlCommand("INSERT INTO ServicesMonitored (sm_ServiceName) SELECT @ServiceName WHERE NOT EXISTS (SELECT 1 FROM ServicesMonitored WHERE sm_ServiceName = @ServiceName)", connection))
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




//string hostName = Session["FullName"].ToString();
//DateTime lastStart = new DateTime(1900, 1, 1);
//string lastEventLog = "No Record";
//string logBy = "Unknown";

//try
//{
//    logBy = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

//    string queryString = $"*[System/Provider/@Name='{serviceName}']";
//    EventLogQuery eventLogQuery = new EventLogQuery("Application", PathType.LogName, queryString);
//    EventLogReader eventLogReader = new EventLogReader(eventLogQuery);

//    int numEntriesToSearch = int.Parse(ConfigurationManager.AppSettings.Get("NumOfEntries"));
//    int entriesSearched = 0;
//    EventRecord latestEvent = null;

//    EventRecord currentEvent = eventLogReader.ReadEvent();
//    while (currentEvent != null && entriesSearched < numEntriesToSearch)
//    {
//        if (latestEvent == null || currentEvent.TimeCreated > latestEvent.TimeCreated)
//        {
//            latestEvent = currentEvent;
//        }
//        entriesSearched++;
//        currentEvent = eventLogReader.ReadEvent();
//    }

//    // Get the necessary information from the latest entry (if one was found)
//    if (latestEvent != null)
//    {
//        lastStart = (DateTime)latestEvent.TimeCreated;
//        lastEventLog = latestEvent.Properties[0].Value.ToString(); // Assumes log message is in the first property
//    }
//}
//catch (Exception ex)
//{
//    throw ex;
//}

//public ActionResult RealTimeTable()
//{
//    List<Service> _activeServicesCopy = new List<Service>(_activeServices); // create a copy of the services list
//    _activeServices.Clear();
//    foreach (Service service in _activeServicesCopy)
//    {
//        GetServicesTB(service.ServiceName);
//    }

//    return Json(_activeServices, JsonRequestBehavior.AllowGet);
//}

//public void GetServicesTB(string serviceName)
//{
//    try
//    {
//        using (SqlConnection connection = GetConnection())
//        using (SqlCommand command = new SqlCommand("GetServiceLogsByServiceName", connection))
//        {
//            command.CommandType = CommandType.StoredProcedure;
//            if (serviceName != null)
//            {
//                command.Parameters.AddWithValue("@ServiceName", serviceName);
//            }
//            else
//            {
//                command.Parameters.AddWithValue("@ServiceName", DBNull.Value);
//            }

//            SqlDataReader reader = command.ExecuteReader();

//            if (reader.HasRows)
//            {
//                while (reader.Read())
//                {
//                    _activeServices.Add(new Service()
//                    {
//                        ServiceName = reader["sl_ServiceName"].ToString(),
//                        LastStart = reader["sl_LastStart"].ToString(),
//                        ServiceStatus = reader["sl_ServiceStatus"].ToString(),
//                        LastEventLog = reader["sl_LastEventLog"].ToString(),
//                        HostName = reader["sl_HostName"].ToString()
//                    });
//                }
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        throw ex;
//    }
//}

