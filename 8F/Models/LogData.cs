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
    public class LogData
    {
        public string BatchName { get; set; } = string.Empty;
        public string LogStartDate { get; set; } = string.Empty;
        public string LogEndDate { get; set; } = string.Empty;
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int TotalCount { get; set; }

        // IsReNewConfig
        public DateTime LogDateRaw { get; set; }
        public string ProductionOrder { get; set; } = string.Empty;   
        public string Date { get; set; } = string.Empty;

    }

}
