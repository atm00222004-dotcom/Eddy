using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace _8F
{
    public class PortCOM
    {
        public SerialPort port;
        public static List<ChannelData> channelDatas;
        public static List<Response> responses;
        public static bool IsResponseRefreshRequired = false;
        public static int ResultCount = 0;
        public static int ResultOkCount = 0;
        public static int ResultOkNotCount = 0;
        public static string PortName;
        public static int BaudRate;
        public static int ChannelNo = 4;
        public void InitialPort(string portName, int baudRate = 115200)
        {
            PortName = portName;
            BaudRate = baudRate;

            port = new SerialPort
            {
                BaudRate = BaudRate,
                DataBits = 8,
                Handshake = Handshake.None,
                Parity = Parity.None,
                PortName = portName,
                StopBits = StopBits.One,
                ReadTimeout = 500,
                WriteTimeout = 2000
            };
        }
        private void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(50);
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadExisting();
                new Thread(() =>
                {
                    ProcessPortData(indata);
                }).Start();

            }
            catch (Exception ex)
            {

            }
        }
        private static void ProcessPortData(string indata)
        {
            try
            {
                if (!string.IsNullOrEmpty(indata))
                {
                    var res = JsonConvert.DeserializeObject<Response>(indata);
                    if (ChannelNo >= res?.CN)
                    {
                        responses.Add(res);
                        if (res.OR == 1)
                        {
                            ResultOkCount = ResultOkCount + 1;
                        }
                        else
                        {
                            ResultOkNotCount = ResultOkNotCount + 1;
                        }
                        ResultCount = ResultOkCount + ResultOkNotCount;
                        IsResponseRefreshRequired = true;
                        // Maintain logs 
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        public bool WriteData(string data)
        {
            try
            {
                if (port.IsOpen)
                {
                    port.Close();
                }

                InitialPort(PortName, BaudRate);

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

                port.DataReceived += serialPort_DataReceived;
                if (!port.IsOpen)
                {
                    port.Open();
                }
                if (result[0] == '0')
                {

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
    public class ChannelData
    {
        public int Id = 0;
        public bool IsSeleted = false;
        public List<GraphData> graphDatas;
    }
    public class GraphData
    {
        public int Id = 0;
        public string Name = "D";
        public int freq = 400;
        public int gain = 10;
        public int phase = 0;
        public double height = 2000;
        public double width = 1400;
        public double ex = 0;
        public double ey = 0;
        public double angel = 0;
    }
    public class Response
    {
        public int FC;
        public int CN;
        public int OR;
        public List<FreqResult> FD;
    }
    public class FreqResult
    {
        public int FN;
        public int R;
        public int X;
        public int Y;
    }
    public class FrequencyWrite
    {
        public int FC;
        public int CN;
        public List<Frequency> FD;
    }
    public class Frequency
    {
        public int FN;
        public int F;
        public int G;
        public int P;
    }
    public class ElliplseWrite
    {
        public int FC;
        public int CN;
        public List<Elliplse> ED;
    }
    public class Elliplse
    {
        public int FN;
        public int EId;
        public double a;
        public double b;
        public double t;
        public double x;
        public double y;
    }
    public class FrequencyCount
    {
        public int FC;
        public int C;
        public int NC;
    }
    public class Mode
    {
        public int FC;
        public int M;
    }
    public class BalanceTest
    {
        public int FC;
        public int CN;
    }
}
