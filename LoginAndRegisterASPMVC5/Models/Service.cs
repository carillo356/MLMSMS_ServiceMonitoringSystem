using System;
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

        public DateTime LastStart { get; set; }
        public string ServiceStatus { get; set; }
        public string LastEventLog { get; set; }

        public string HostName { get; set; }
    }
}