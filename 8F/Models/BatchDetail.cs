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
    public class BatchDetail
    {
        public DateTime TimeStamp { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public bool Result { get; set; }
        public string FDData { get; set; } = string.Empty;
        public string PartData { get; set; } = string.Empty;
    }

}
