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
    public class GraphData
    {
        //public int Id = 0;
        //public string Name = "D";
        //public int freq = 400;
        //public int gain = 10;
        //public int phase = 0;
        //public bool isEnable = true;
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "D";
        public int freq { get; set; } = 400;
        public int gain { get; set; } = 10;
        public int phase { get; set; } = 0;
        public int strength = 100;
        public int postGain = 60;
        public bool isEnable { get; set; } = true;
        public double height = DeviceCOM.DefaultHeight;
        public double width = DeviceCOM.DefaultWidth;
        public double ex = 30;
        public double ey = 30;
        public double angel = 30;
        public List<Ellips> ellipses = new List<Ellips>();

        public double height_O = DeviceCOM.DefaultHeight_O;
        public double width_O = DeviceCOM.DefaultWidth_O;
        public double ex_O = 0;
        public double ey_O = 0;
        public double angel_O = DeviceCOM.DefaultAngel_O;
    }

}
