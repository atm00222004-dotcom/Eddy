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
using Npgsql;
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
        public static int MaxValue;
        public static int Factor;
        public static Configuration Configuration;
        public static GraphData graphData;
        public static bool IsTestOn = false;
        public static bool IsTubeSatart = false;
        public static DateTime busyStamp = System.DateTime.Now;
        public static Part part;
        public static bool IsLogEnable = false;
        public static byte[] receiveBytes;
        public static double[] dataBuffer;
        public static bool IsCalibarationStart = false;

        public static int Ok = 0;
        public static int NoOk = 0;


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
                    IsTestOn = true;
                    busyStamp = System.DateTime.Now;
                }
                else if(result[0] == 22)
                {
                    IsTestOn = false;
                }
                return true;
            }
            catch (Exception e)
            {
               
                return false;
            }
        }
    }

    public class Part
    {
        public string Name = "";
        public string CheckedBy = "";
        public string CompanyName = "";
        public int BatchSize = 5;
        public string Placce = "";
        public string Grade = "";
    }
    public class Configuration
    {
        public Marker Marker { get; set; }
        public Frequency Frequency { get; set; }
        public Filter Filter { get; set; }

        public int TestTime = 10;
        public int SamplePerSecond = 3050;
    }

    public class ConfigurationToWrite
    {
        public int FC = 57;
        //public Marker Marker { get; set; }
        public Frequency Frequency { get; set; }
        public Filter Filter { get; set; }

    }
    public class Marker
    {
        public int FC = 50;
        public int M1 = 600;
        public int M2 = 1000;
        public int FmS = 500;
        public int RmS = 500;
        public int P1mS = 200;

        public int C1C2 = 245;
        public int C2E = 1060;
        public int CC2 = 110;
    }

    public class FD
    {
        public int FN = 0;
        public int E = 1;
        public int F = 50000;
        public int G = 30;
        public int UTH = 90;
        public int LTH = 40;
        public int TH = 0;
        public int PP = 100;
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
        public List<FD> FD;
    }
    public class Filter
    {
        public int FC = 52;
        public List<FilterFD> FD;
    }
    public class GraphData
    {
        public bool Result = true;
        public List<Fdata> AmpD1 = new List<Fdata>();
        //public List<Fdata> AmpD2 = new List<Fdata>();
        //public List<Fdata> AmpD3 = new List<Fdata>();
        //public List<int> D1MarkerIndexs = new List<int>();
        //public List<int> D2MarkerIndexs = new List<int>();
        //public List<int> D3MarkerIndexs = new List<int>();
    }

    public class FNData
    {
        public int FN { get; set; }
        public List<Fdata> Data { get; set; }
    }

    public class Fdata
    {
        public int Amp { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public int phase { get; set; }
        public bool IsMarked { get; set; }
    }

    public class Status
    {
        public int FC;
    }
}

