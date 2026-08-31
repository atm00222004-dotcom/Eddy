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
    public class Part
    {
        public string Name = "";
        public string Grade = "";
        public string CompanyName = "";
        public int BatchType= 1;
        public int BatchSize = 5;
        public int BatchNo = 1;

        //Common Properties for both new and old PartConfiguration
        public string CheckedBy = "";
        public string BatchName = "";

        //Properties for new PartConfiguration
        public string ProductionOrder = "";
        public string MachineNumber = "";
        public string PartNumber = "";
        public string PartFamily = "";
    }

}
