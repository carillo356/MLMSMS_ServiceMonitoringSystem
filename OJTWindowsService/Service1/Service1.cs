using System;
using System.Timers;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;

namespace Service1
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer(); 
        //int myCounter = 3;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //throw new Exception("Yup a test exception occurred! ");
            WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 3000;
            timer.Start();
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            //myCounter--;

            //if (myCounter == 0)
            //{
            //    timer.Stop();
            //}

            //if (myCounter == 0)
            //{
            //    try
            //    {
            //        throw new Exception("Yup a test exception occurred! ");
            //    }
            //    catch (Exception ex)
            //    {
            //        EventLog eventLog = new EventLog("Application");
            //        eventLog.Source = "Service1";
            //        eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            //        Environment.FailFast("Kill");
            //    }
            //}

            WriteToFile("Service is recall at " + DateTime.Now + " My Counter: " /*+ myCounter*/);
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}

//string[] servicesToMonitor = ConfigurationManager.AppSettings.Get("service").Split(' '); //Splits to fetch each services to monitors


//string[] servicesToMonitor = ConfigurationManager.AppSettings.Get("service").Split(' '); //Splits to fetch each services to monitors

//Boolean serviceMatch = false;


//foreach (ServiceController service in services) //Access installed service
//{
//    //if (service.ServiceName == serviceToMonitor) //Checks if Services to monitor exist in the installed services and get its Current Status.
//    //{
//    //    serviceMatch = true;
//    //}

//serviceMatch = service.ServiceName == serviceToMonitor;

//if (currentStatus != previousStatus) //Checks if the current status is different from previous status
//{
//    statusMatch = false;
//}


//                    try
//            {

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("An error occured while executing the query: " + ex.Message);
//            }
//            finally
//            {
//                if (command != null)
//                {
//                    command.Dispose();
//                }

//if (connection != null && connection.State == System.Data.ConnectionState.Open)
//{
//    connection.Close();
//}


//            }

//static bool CheckMatchingStatus(string currentStatus, string previousStatus)
//{
//    bool statusMatch = currentStatus == previousStatus;

//    return statusMatch;
//}


















//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Security.Policy;
//using System.ServiceProcess;
//using System.Text;
//using System.Threading.Tasks;
//using System.Timers;

//namespace OJTWindowsService
//{
//    public partial class Service1 : ServiceBase
//    {

//        public Service1()
//        {
//            InitializeComponent();
//        }

//        Timer timer = new Timer(); // name space(using System.Timers;)  
//        int myCounter = 3;


//        protected override void OnStart(string[] args)
//        {
//            WriteToFile("Service is started at " + DateTime.Now);
//            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
//            timer.Interval = 3000; //number in milisecinds  
//            timer.Start();
//            throw new Exception("Test1" + DateTime.Now);
//            //Process.Start("C:\\Users\\Aaron\\source\\repos\\OJTWindowsService\\ConsoleApp\\bin\\Debug\\net6.0\\ConsoleApp.exe", "OJTWindowsService Start");
//        }

//        protected override void OnStop()
//        {
//            WriteToFile("Service is stopped at " + DateTime.Now);
//            //Process.Start("C:\\Users\\Aaron\\source\\repos\\OJTWindowsService\\ConsoleApp\\bin\\Debug\\net6.0\\ConsoleApp.exe", " OJTWindowsService Stop \"The OJTWindows service has transitioned to the stopped state.\"");

//        }
//        private void OnElapsedTime(object source, ElapsedEventArgs e)
//        {
//                myCounter--;

//            if (myCounter == 0)
//            {
//            WriteToFile("Exception Occured! " + DateTime.Now );
//            }

//            WriteToFile("Service is recall at " + DateTime.Now + " My Counter: " + myCounter);


//        }

//        public void WriteToFile(string Message)
//            {
//                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
//                if (!Directory.Exists(path))
//                {
//                    Directory.CreateDirectory(path);
//                }
//                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
//                if (!File.Exists(filepath))
//                {
//                    // Create a file to write to.   
//                    using (StreamWriter sw = File.CreateText(filepath))
//                    {
//                        sw.WriteLine(Message);
//                    }
//                }
//                else
//                {
//                    using (StreamWriter sw = File.AppendText(filepath))
//                    {
//                        sw.WriteLine(Message);
//                    }
//                }
//            }


//    }
//}




