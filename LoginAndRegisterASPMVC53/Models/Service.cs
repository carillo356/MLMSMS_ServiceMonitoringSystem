using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LoginAndRegisterASPMVC5.Models
{
    public class Service
    {
        public string ServiceName { get; set; }
        public string LastStart { get; set; }
        public string ServiceStatus { get; set; }
        public string LastLog { get; set; }
        public string ActionBy { get; set; }
    }
}