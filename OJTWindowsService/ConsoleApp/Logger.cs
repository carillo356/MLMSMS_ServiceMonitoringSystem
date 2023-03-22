﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Mail;  
using System.ServiceProcess;
using System.Threading.Tasks;
using CommonLibrary;

namespace Logger
{
    class Logger
    {
        static void Main(string[] args)
        {
            // Get the service name from the command line arguments
            string serviceName = args[0];

            try
            {
                // Get all the installed services
                ServiceController[] servicesInController = ServiceController.GetServices();

                // Find the service by name
                ServiceController serviceInController = servicesInController.FirstOrDefault(s => s.ServiceName == serviceName);

                // Create a comma-separated string of all service names
                string serviceNamesCsv = string.Join("/", servicesInController.Select(s => s.ServiceName));

                // Open a connection to the database
                using (SqlConnection connection = CommonMethods.GetConnection())
                {
                    if (connection == null) return;

                    // Update the list of available services in the database
                    CommonMethods.SP_UpdateServicesAvailable(connection, serviceNamesCsv);

                    // Update the status of the current service in the database
                    CommonMethods.GetServiceLogs(serviceInController, out ServiceControllerStatus serviceStatus, out string hostName, out string logBy, out DateTime lastStart, out string lastEventLog);
                    CommonMethods.SP_UpdateServiceStatus(connection, serviceName, serviceStatus.ToString(), hostName, logBy, lastStart, lastEventLog);

                    // Send an email if the service is stopped
                    if (serviceStatus == ServiceControllerStatus.Stopped)
                    {
                        CommonMethods.SendEmail(connection, "Service Name: " + serviceInController.ServiceName + "\nLogBy: " + logBy + "\nStatus: " + serviceStatus.ToString() + "\nLastStart: " + lastStart + "\nlastEventLog: " + lastEventLog);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonMethods.WriteToFile("Exception: main " + ex.Message);
            }
        }


    }

}

