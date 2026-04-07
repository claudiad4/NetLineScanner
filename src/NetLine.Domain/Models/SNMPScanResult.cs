using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Domain.Models
{
    public class SNMPScanResult
    {
        public bool Success { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Contact { get; set; }
        public string? UpTime { get; set; }
        public int? InterfacesCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
