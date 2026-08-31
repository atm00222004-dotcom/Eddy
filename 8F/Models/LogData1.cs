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
    public class LogData1
    {
        public string BatchName { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string SrNo { get; set; } = string.Empty;
        public string TimeStamp { get; set; } = string.Empty;
        public string ResultStatus { get; set; } = string.Empty;
        public string Ch1Result { get; set; } = string.Empty;
        public string Ch2Result { get; set; } = string.Empty;
        public string Ch3Result { get; set; } = string.Empty;
        public string Ch4Result { get; set; } = string.Empty;

    }

}
