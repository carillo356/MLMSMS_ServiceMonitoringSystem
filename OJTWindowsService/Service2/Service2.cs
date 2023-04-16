using System;
using System.Timers;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;

namespace Service2
{
    public partial class Service2 : ServiceBase
    {
        Timer timer = new Timer();
        int myCounter = 4;

        public Service2()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
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
            //        eventLog.Source = "Service2";
            //        eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            //        Environment.FailFast("Kill");
            //    }
            //}

            WriteToFile("Service is recall at " + DateTime.Now + " My Counter: "/* + myCounter*/);
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




