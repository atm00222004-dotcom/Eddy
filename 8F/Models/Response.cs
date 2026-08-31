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
    public class Response
    {
        public int FC;
        public int CN;
        public int OR;
        public bool IsBalacenced = false;
        public bool IsAutoEllipseTest = false;
        public List<FreqResult> FD = new();
        public int ERR { get; set; }
    }

}
