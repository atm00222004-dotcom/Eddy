using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _8F.Models
{
    public class PartConfiguration
    {
        public string BatchName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public string ProductionOrder { get; set; } = string.Empty;
        public string MachineNumber { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string PartFamily { get; set; } = string.Empty;
    }

}
