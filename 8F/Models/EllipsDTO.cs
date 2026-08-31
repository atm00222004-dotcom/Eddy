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
    public class EllipsDTO
    {
        public int Id { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public double ex { get; set; }
        public double ey { get; set; }
        public double angel { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
    }

}
