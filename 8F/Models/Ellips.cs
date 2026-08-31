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
    public class Ellips
    {
        public int Id = 0;
        public double height = DeviceCOM.DefaultHeight;
        public double width = DeviceCOM.DefaultWidth;
        public double ex = 0;
        public double ey = 0;
        public double angel = 0;

        public bool IsValid() => height >= 0 && width >= 0 && !double.IsNaN(ex) && !double.IsNaN(ey);

        public void EnforceBounds(double minDimension = 100.0)
        {
            if (height < minDimension) height = minDimension;
            if (width < minDimension) width = minDimension;
        }

        public bool Contains(double x, double y)
        {
            double rad = angel * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            double dx = x - ex;
            double dy = y - ey;

            double rotX = dx * cos + dy * sin;
            double rotY = -dx * sin + dy * cos;

            double a = width / 2.0;
            double b = height / 2.0;

            if (a <= 0 || b <= 0) return false;
            return ((rotX * rotX) / (a * a) + (rotY * rotY) / (b * b)) <= 1.0;
        }
    }

}
