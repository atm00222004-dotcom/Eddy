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



namespace _8F
{
    public class DeviceCOM
    {
        public SerialPort port;
        public static List<ChannelData> channelDatas;
        public static List<Response> responses;
        public static bool IsResponseRefreshRequired = false;
        public static bool IsResponseClearRequired = false;
        public static int ResultCount = 0;
        public static int ResultOkCount = 0;
        public static int ResultOkNotCount = 0;
        public static int CommunicationType;
        public static string PortName;
        public static int BaudRate;
        public static string IpAddress;
        public static int SPort;
        public static int ChannelNo = 4;
        DispatcherTimer dispatcherTimer;
        TcpClient client;
        NetworkStream stream;
        public void InitialPort(int communicationType, string portName, int baudRate, string ipAddress, int sport )
        {
            CommunicationType = communicationType;
            PortName = portName;
            BaudRate = baudRate;
            IpAddress = ipAddress;
            SPort = sport;

            if (CommunicationType == 0)
            {
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
            else if (CommunicationType == 1)
            {
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(10000000);
                dispatcherTimer.Start();

                client = new TcpClient();
            }

        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            client.NoDelay = false;
            if (!client.Connected)
            {
                client = new TcpClient();
                IPAddress iPAddress = IPAddress.Parse(IpAddress);
                var ipEndPoint = new IPEndPoint(iPAddress, SPort);
                client.Connect(ipEndPoint);
            }
            if (client.Connected)
            {
                stream = client.GetStream();
                if (stream.DataAvailable)
                {                    
                    try
                    {
                        var buffer = new byte[client.Available];
                        int received = stream.Read(buffer);
                        var message = Encoding.UTF8.GetString(buffer, 0, received);
                        new Thread(() =>
                        {
                            ProcessPortData(message);
                        }).Start();

                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
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

                    if (res.FC == 19)
                    {
                        IsResponseClearRequired = true;
                    }
                    else if (res.FC == 20)
                    {
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
            }
            catch (Exception ex)
            {

            }
        }
        public bool WriteData(string data)
        {
            try
            {
                if (CommunicationType == 0)
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }

                    InitialPort(CommunicationType, PortName, BaudRate, IpAddress, SPort);

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
                else if (CommunicationType == 1)
                {
                    dispatcherTimer.Stop();
                    if (!client.Connected)
                    {
                        client = new TcpClient();
                        IPAddress iPAddress = IPAddress.Parse(IpAddress);
                        var ipEndPoint = new IPEndPoint(iPAddress, SPort);
                        client.Connect(ipEndPoint);
                    }

                    if (client.Connected)
                    {
                        var messageBytes = Encoding.UTF8.GetBytes(data);
                        stream = client.GetStream();
                        stream.Write(messageBytes, 0, messageBytes.Length);
                        stream = client.GetStream();
                        var buffer = new byte[client.Available];
                        int received = stream.Read(buffer);
                        stream.Flush();
                        dispatcherTimer.Start();
                        return true;
                    }
                    dispatcherTimer.Start();
                    return false;
                }
                return false;
            }
            catch (Exception e)
            {
                if (CommunicationType == 1)
                {
                    dispatcherTimer.Start();
                }

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
