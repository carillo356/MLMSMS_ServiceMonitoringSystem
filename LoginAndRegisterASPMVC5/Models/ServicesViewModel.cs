using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LoginAndRegisterASPMVC5.Models
{
    public class ServicesViewModel
    {
        public List<Service> Services { get; set; }
        public List<ServiceLogs> ServicesLogs { get; set; }
    }
}