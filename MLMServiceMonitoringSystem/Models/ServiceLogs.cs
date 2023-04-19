using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LoginAndRegisterASPMVC5.Models
{
    public class ServiceLogs
    {
    [Key]
        public int LogID { get; set; }
        public string ServiceName { get; set; }
        public DateTime LogDate { get; set; }
        public string LogBy { get; set; }
        public DateTime LastStart { get; set; }
        public string ServiceStatus { get; set; }
        public string LastLog { get; set; }
        public string ActionBy { get; set; }
    }
}