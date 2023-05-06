﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LoginAndRegisterASPMVC5.Models
{
    public class Service
    {
        [Key]
        public string ServiceName { get; set; }
        public string LogDate { get; set; }
        public string LogBy { get; set; }

        public string LastStart { get; set; }
        public string ServiceStatus { get; set; }
        public string LastEventLog { get; set; }

        public string HostName { get; set; }
        
        public string Description { get; set; }

        public string StartupType { get; set; }

        public string LogOnAs { get; set; }

        public string PendingCommand { get; set; }
    }
}