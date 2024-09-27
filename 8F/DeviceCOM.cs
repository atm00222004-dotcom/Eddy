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



namespace _8F
{
    public class DeviceCOM
    {
        public SerialPort port;
        public static List<ChannelData> channelDatas;
        public static List<Response> responses;
        public static bool IsResponseRefreshRequired = false;
        public static bool IsBalanceAll = false;
        public static bool IsBalanceBusyEnable = false;
        public static bool IsResponseClearRequired = false;
        public static int CommunicationType;
        public static string PortName;
        public static int BaudRate;
        public static string IpAddress;
        public static int SPort;
        public static int ChannelNo = 4;
        public static int DefaultHeight = 0;
        public static int DefaultWidth = 0;
        public static string ConnectionString;
        public static bool IsLogEnable = false;
        public static bool IsSystemBusy = false;
        public static DateTime busyStamp = System.DateTime.Now; 
        public static Part part;
        public static List<Counter> counter;

        DispatcherTimer dispatcherTimer;
        TcpClient client;
        NetworkStream stream;

        public void InitialPort(int communicationType, string portName, int baudRate, string ipAddress, int sport)
        {
            DeviceCOM.part = new Part();
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

            if (counter == null)
            {
                counter = new List<Counter>();
                var cnt1 = new Counter();
                cnt1.Id = 1;
                counter.Add(cnt1);

                var cnt2 = new Counter();
                cnt2.Id = 2;
                counter.Add(cnt2);

                var cnt3 = new Counter();
                cnt3.Id = 3;
                counter.Add(cnt3);

                var cnt4 = new Counter();
                cnt4.Id = 4;
                counter.Add(cnt4);
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
        private void ProcessPortData(string indata)
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
                            var cnt = counter.FirstOrDefault(c => c.Id == res.CN);
                            if (res.OR == 1)
                            {
                                cnt.ResultOkCount = cnt.ResultOkCount + 1;
                            }
                            else
                            {
                                cnt.ResultOkNotCount = cnt.ResultOkNotCount + 1;
                            }
                            cnt.ResultCount = cnt.ResultOkCount + cnt.ResultOkNotCount;
                            IsResponseRefreshRequired = true;
                            // Maintain logs ==> ChId, Overall Result, File Name, TimeStamp 
                            if (IsLogEnable)
                            {
                                WriteLog(res.CN, Convert.ToBoolean(res.OR), DateTime.Now);
                            }
                        }
                    }
                    else if (res.FC == 21)
                    {
                        IsSystemBusy = true;
                        busyStamp = System.DateTime.Now;
                    }
                    else if (res.FC == 22)
                    {
                        IsSystemBusy = false;
                        if (IsBalanceBusyEnable)
                        {
                            IsResponseClearRequired = true;
                            IsBalanceBusyEnable = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void WriteLog(int ChId, bool Result, DateTime TimeStamp)
        {
            try
            {
                using (var con = new NpgsqlConnection(ConnectionString))
                {
                    string sql = string.Empty;
                    con.Open();
                    var fdData = JsonConvert.SerializeObject(DeviceCOM.channelDatas.FirstOrDefault( r=> r.Id == ChId ).graphDatas);
                    var partData = JsonConvert.SerializeObject(DeviceCOM.part);
                    sql = "INSERT INTO public.\"Logs\"(\"ChId\", \"Result\", \"FDData\", \"PartData\", \"PartName\", \"BatchNo\", \"TimeStamp\")\r\n\t" +
                        "VALUES (" +
                        ChId + ", '" +
                        Result + "', '" +
                        fdData + "', '" +
                        partData + "', '" +
                        DeviceCOM.part.Name + "', " +
                        DeviceCOM.part.BatchNo + ", '" +
                        TimeStamp + "'); SELECT count(1) \r\n\tFROM public.\"Logs\" where \"PartName\" = '" + DeviceCOM.part.Name + "' and \"BatchNo\" = "+DeviceCOM.part.BatchNo + " ;";

                    var cmd = new NpgsqlCommand(sql, con);
                    var count = cmd.ExecuteScalar();

                    if (DeviceCOM.part.BatchType == 1 && Convert.ToInt32(count) >= DeviceCOM.part.BatchSize)
                    {
                        DeviceCOM.part.BatchNo = DeviceCOM.part.BatchNo + 1;
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
                    if (result[0] == '0' || result[0] == '2')
                    {
                        if (result[0] == '2')
                        {
                            DeviceCOM.IsSystemBusy = true;
                            busyStamp = System.DateTime.Now;
                        }
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
                        if (buffer.Length > 0)
                        {
                            if (buffer[0] == '0' || buffer[0] == '2')
                            {
                                if (buffer[0] == '2')
                                {
                                    DeviceCOM.IsSystemBusy = true;
                                    busyStamp = System.DateTime.Now;
                                }
                                return true;
                            }
                        }
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

        public bool GetSystemStatus(string data)
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
                    if (result[0] == 21)
                    {
                        DeviceCOM.IsSystemBusy = true;
                        busyStamp = System.DateTime.Now;
                    }
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
                        if (buffer[0] == 21)
                        {
                            DeviceCOM.IsSystemBusy = true;
                        }
                    }
                    dispatcherTimer.Start();
                }
                return true;
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

    public class Counter
    {
        public int Id = 0;
        public int ResultCount = 0;
        public int ResultOkCount = 0;
        public int ResultOkNotCount = 0;
    }
    public class GraphData
    {
        public int Id = 0;
        public string Name = "D";
        public int freq = 400;
        public int gain = 10;
        public int phase = 0;
        public double height = DeviceCOM.DefaultHeight;
        public double width = DeviceCOM.DefaultWidth;
        public double ex = 0;
        public double ey = 0;
        public double angel = 0;
    }
    public class Response
    {
        public int FC;
        public int CN;
        public int OR;
        public bool IsBalacenced = false;
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
    public class Status
    {
        public int FC;        
    }
    public class BalanceTest
    {
        public int FC;
        public int CN;
    }

    public class Part
    {
        public string Name = "";
        public string Grade = "";
        public string CheckedBy = "";
        public string CompanyName = "";
        public int BatchType= 0;
        public int BatchSize = 5;
        public int BatchNo = 1;
    }
}
