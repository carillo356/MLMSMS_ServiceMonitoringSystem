using Microsoft.Win32.TaskScheduler;
using System;
using System.Data.SqlClient;
using System.ServiceProcess;
using CommonLibrary;
using Task = Microsoft.Win32.TaskScheduler.Task;
using System.Timers;
using System.Configuration;
using System.Linq;

namespace ServicesTaskScheduler
{
    public partial class TaskSchedulerService : ServiceBase
    {
        //readonly Timer timer = new Timer();
        //public TaskSchedulerService()
        //{
        //    InitializeComponent();
        //}

        //protected override void OnStart(string[] args)
        //{
        //    if (int.TryParse(ConfigurationManager.AppSettings.Get("CheckServicesEveryXMinute"), out int interval))
        //    {
        //        timer.Interval = interval * 60 * 1000;
        //    }
        //    else
        //    {
        //        timer.Interval = 3600000; //1hour
        //    }

        //    timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);

        //    if (bool.TryParse(ConfigurationManager.AppSettings.Get("RunOnStart"), out bool runOnStart))
        //    {
        //        if (runOnStart)
        //        {
        //            CreateTasksForMonitoredServices();
        //        }
        //    }

        //    if (!runOnStart)
        //        timer.Start();
        //}

        //public void CreateTasksForMonitoredServices()
        //{
        //    timer.Stop();
        //    try
        //    {
        //        using (SqlConnection connection = CommonMethods.GetConnection())
        //        {
        //            if (connection == null) return;

        //            // Get the services to monitor from the database
        //            (string ServiceName, string ServiceStatus)[] servicesInMonitor = CommonMethods.GetDoubleColumn(connection, "GetServicesStatus").ToArray();

        //            // Iterate over the services to monitor
        //            foreach (var serviceInMonitor in servicesInMonitor)
        //            {
        //                string taskName = serviceInMonitor.ServiceName + " Watcher";

        //                // Check if a task with the given name already exists
        //                if (!TaskExists(taskName))
        //                {
        //                    // If not, create a task for the service
        //                    CreateTasksForMonitoredServices(serviceInMonitor.ServiceName);
        //                }
        //            }
        //            // Remove tasks for unmonitored services
        //            RemoveUnmonitoredWatcherTasks(servicesInMonitor);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonMethods.WriteToFile(ex.Message);
        //    }
        //    finally
        //    {
        //        timer.Stop();
        //    }
        //}

        //protected override void OnStop()
        //{
        //    timer.Dispose();
        //}

        //private void OnElapsedTime(object source, ElapsedEventArgs e)
        //{
        //    CreateTasksForMonitoredServices();
        //}

        //private static bool TaskExists(string taskName)
        //{
        //    using (TaskService taskService = new TaskService())
        //    {
        //        Task task = taskService.GetTask(taskName);
        //        return task != null;
        //    }
        //}

        //public static void CreateTasksForMonitoredServices(string serviceName)
        //{
        //    try
        //    {
        //        using (TaskService ts = new TaskService())
        //        {
        //            string taskName = $"Watcher {serviceName}";
        //            TaskDefinition td = ts.NewTask();
        //            td.RegistrationInfo.Description = $"Task to monitor the {serviceName} service";

        //            td.Triggers.Add(new EventTrigger
        //            {
        //                Subscription = $"<QueryList><Query Id='0' Path='Application'><Select Path='Application'>*[System[Provider[@Name='{serviceName}']]]</Select></Query></QueryList>",
        //                Enabled = true
        //            });

        //            string loggerPath = ConfigurationManager.AppSettings.Get("LoggerPath");

        //            // Add escaped double quotes around the serviceName
        //            td.Actions.Add(new ExecAction(loggerPath, $"\"{serviceName}\""));

        //            td.Principal.RunLevel = TaskRunLevel.Highest;
        //            ts.RootFolder.RegisterTaskDefinition(taskName, td);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonMethods.WriteToFile(ex.Message);
        //    }
        //}

        //private void RemoveUnmonitoredWatcherTasks((string ServiceName, string ServiceStatus)[] servicesInMonitor)
        //{
        //    try
        //    {
        //        using (TaskService ts = new TaskService())
        //        {
        //            // Get all the tasks in the RootFolder
        //            var allTasks = ts.RootFolder.AllTasks;

        //            // Iterate through each task
        //            foreach (Task task in allTasks)
        //            {
        //                // Check if the task starts with "Watcher "
        //                if (task.Name.StartsWith("Watcher "))
        //                {
        //                    // Extract the service name from the task name
        //                    string serviceName = task.Name.Substring("Watcher ".Length);

        //                    // Check if the service is not in the monitored list
        //                    if (!servicesInMonitor.Any(s => s.ServiceName == serviceName))
        //                    {
        //                        // If not in the list, delete the task
        //                        ts.RootFolder.DeleteTask(task.Name);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonMethods.WriteToFile(ex.Message);
        //    }

        //}

    }
}
