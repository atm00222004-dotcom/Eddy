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

namespace _8F.Helpers
{
    public class MyColor
    {
        public string ColorName { get; set; } = string.Empty;

        public static string GetColorName(int index)
        {
            string MyColor = "Black";
            if (index == 0)
            {
                MyColor = "Black";
            }
            else if (index == 1)
            {
                MyColor = "Blue"; 
            }
            else if (index == 2)
            {
                MyColor = "Red";
            }

            else if (index == 3)
            {
                MyColor = "Green";  
            }

            else if (index == 4)
            {
                MyColor = "Brown";
            }

            else if (index == 5)
            {
                MyColor = "Yellow";
            }

            else if (index == 6)
            {
                MyColor = "Blue"; 
            }

            return MyColor;

        }
        public static Color GetColor(int index)
        {
            Color MyColor = Colors.Black;
            if (index == 0)
            {
                MyColor = Colors.Black;
            }
            else if (index == 1)
            {
                MyColor = Colors.Blue;
            }
            else if (index == 2)
            {
                MyColor = Colors.Red;
            }

            else if (index == 3)
            {
                MyColor = Colors.Green;
            }

            else if (index == 4)
            {
                MyColor = Colors.Brown;
            }

            else if (index == 5)
            {
                MyColor = Colors.Yellow;
            }

            else if (index == 6)
            {
                MyColor = Colors.Blue;
            }

            return MyColor;

        }

    }

}
