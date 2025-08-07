using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net;
using System.Printing;
using System.Windows.Threading;
using System.Net.Sockets;
using System.IO;
//using Npgsql;
using System.Diagnostics.Metrics;
using System.Windows.Media;
using System.Windows.Documents;
using OpenTK.Compute.OpenCL;
using SkiaSharp;



namespace Eddy
{
    public class DeviceCOM
    {
        public SerialPort port;

        public static string PortName;
        public static int BaudRate;
        public static Configuration Configuration;
        public static GraphData graphData;
        public static bool IsSystemBusy = false;
        public static DateTime busyStamp = System.DateTime.Now;
        public void InitialPort()
        {
            port = new SerialPort
            {
                BaudRate = BaudRate,
                DataBits = 8,
                Handshake = Handshake.None,
                Parity = Parity.None,
                PortName = PortName,
                StopBits = StopBits.One,
                ReadTimeout = 500,
                WriteTimeout = 2000
            };
        }
        public bool WriteData(string data)
        {
            try
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
                this.port.ReadExisting();
                this.port.Write(data);
                int toread = 1;
                int offset = 0;
                char[] result = new char[toread];
                while (toread > 0)
                {
                    int r = this.port.Read(result, offset, toread);
                    offset += r;
                    toread -= r;
                }

                if (port.IsOpen)
                {
                    port.Close();
                }

                if (result[0] == '0')
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
                return false;
            }
        }

        public bool GetSystemStatus(string data)
        {
            try
            {

                if (!port.IsOpen)
                {
                    port.Open();
                }
                this.port.ReadExisting();
                this.port.Write(data);
                int toread = 1;
                int offset = 0;
                char[] result = new char[toread];
                while (toread > 0)
                {
                    int r = this.port.Read(result, offset, toread);
                    offset += r;
                    toread -= r;
                }

                if (result[0] == 21)
                {
                    IsSystemBusy = true;
                    busyStamp = System.DateTime.Now;
                }
                else if(result[0] == 22)
                {
                    IsSystemBusy = false;
                }
                return true;
            }
            catch (Exception e)
            {
               
                return false;
            }
        }
    }

    public class Configuration
    {
        public Marker Marker { get; set; }
        public Frequency Frequency { get; set; }
        public Filter Filter { get; set; }
    }
    public class Marker
    {
        public int FC = 50;
        public int M1 = 100;
        public int M2 = 200;
        public int M3 = 300;
    }
    public class FD
    {
        public int FN = 0;
        public int E = 1;
        public int F = 5000;
        public int G = 30;
        public int UTH = 100;
        public int LTH = 100;
        public int PP = 200;
        public int PM = 200;
    }
    public class FilterFD
    {
        public int FN = 0;
        public int H = 10;
        public int L = 100;
    }
    public class Frequency
    {
        public int FC = 51;
        public int MS;
        public List<FD> FD;
    }
    public class Filter
    {
        public int FC = 52;
        public List<FilterFD> FD;
    }
    public class GraphData
    {
        public List<int> AmpD1 = new List<int>();
        public List<int> AmpD2 = new List<int>();
        public List<int> AmpD3 = new List<int>();
        public List<int> D1MarkerIndexs = new List<int>();
        public List<int> D2MarkerIndexs = new List<int>();
        public List<int> D3MarkerIndexs = new List<int>();
    }

    public class FNData
    {
        public int FN { get; set; }
        public List<Fdata> Data { get; set; }
    }

    public class Fdata
    {
        public int Amp { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int phase { get; set; }
    }

    public class Status
    {
        public int FC;
    }
}

