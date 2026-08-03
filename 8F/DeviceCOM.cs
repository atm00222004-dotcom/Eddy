using Newtonsoft.Json;
using Npgsql;
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;



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
        public static int DefaultHeight_O = 0;
        public static int DefaultWidth_O = 0;
        public static int DefaultAngel_O = 0;
        public static string ConnectionString;
        public static bool IsLogEnable = false;
        public static bool IsSystemBusy = false;
        public static DateTime busyStamp = System.DateTime.Now; 
        public static Part part;
        public static List<Counter> counter;
        public static bool IsLogDisable = false;
        public static bool IsBalanceRequired = false;
        public static bool IsBinRequired = false;
        public static int ERRCode = 0;
        public static string Code;
        public static bool IsJSON = false;
        public static bool IsLogRequiredOnBalance = false;
        DispatcherTimer dispatcherTimer;
        TcpClient client;
        NetworkStream stream;
        public static bool PortAck = false ;
        public static string PortData = "";
        private ManualResetEvent _ackEvent = new ManualResetEvent(false);

        public void InitialPort(int communicationType, string portName, int baudRate, string ipAddress, int sport)
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
                    WriteTimeout = 2000,
                    DtrEnable = true,
                    RtsEnable = true,
                };
                port.ReceivedBytesThreshold = 1;
                port.DataReceived += serialPort_DataReceived;
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
                short[] data;
                string indata = string.Empty;
                
                if (PortAck)
                {
                    indata = sp.ReadExisting();
                    PortData = indata;
                    PortAck = false;
                    _ackEvent.Set();  // signal that data is received
                }
                else
                {
                    if (IsJSON)
                    {
                        indata = sp.ReadExisting();
                        data = new short[indata.Length];
                    }
                    else
                    {
                        byte[] buffer = new byte[sp.BytesToRead];
                        sp.Read(buffer, 0, buffer.Length);

                        data = buffer.Select(b => (short)b).ToArray();
                    }

                    new Thread(() =>
                    {
                        if(IsJSON)
                        {
                            ProcessPortData(indata);
                        }
                        else
                        {
                            ProcessPortDataBytpe(data);
                        }
                            
                        
                    }).Start();
                }

            }
            catch (Exception ex)
            {

            }
        }

        public static void Test()
        {

            short[] testData = new short[]
{
    0x02, 0x14, 0x32, 0x01, 0x00,

    0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
    0x02, 0x01, 0x01, 0x00, 0xFF, 0xFF,
    0x03, 0x01, 0x02, 0x00, 0xFE, 0xFF,
    0x04, 0x01, 0x03, 0x00, 0xFD, 0xFF,
    0x05, 0x01, 0x04, 0x00, 0xFC, 0xFF,
    0x06, 0x01, 0x05, 0x00, 0xFB, 0xFF,
    0x07, 0x01, 0x06, 0x00, 0xFA, 0xFF,
    0x08, 0x01, 0x07, 0x00, 0xF9, 0xFF,

    0x38, 0x9A
};
                DeviceCOM deviceCOM = new DeviceCOM();
                deviceCOM.ProcessPortDataBytpe(testData);
        }

        private void ProcessPortDataBytpe(short[] indata)
        {
            try
            {
                if (indata.Length>1)
                {
                    

                    if (indata[1] == 19)
                    {
                        IsResponseClearRequired = true;
                    }
                    else if (indata[1] == 20)
                    {
                        //var res = JsonConvert.DeserializeObject<Response>(indata);
                        Response res = new Response();
                        res.CN = indata[3];
                        res.OR = indata[4];
                        res.FC = 20;
                        int length = indata[2]/6;
                        res.FD = new List<FreqResult>();
                        for (int i = 0; i < length; i++)
                        {
                            FreqResult fd = new FreqResult();
                            fd.FN = indata[5 + (i * 6)];
                            fd.R = indata[6 + (i * 6)];
                            int offset = 7 + (i * 6);
                            short x = (short)(indata[offset] | (indata[offset + 1] << 8));
                            short y = (short)(indata[offset + 2] | (indata[offset + 3] << 8));
                            fd.X = x;
                            fd.Y = y;
                            fd.X = (short)(indata[7 + (i * 6)] | (indata[8 + (i * 6)] << 8));
                            fd.Y = (short)(indata[9 + (i * 6)] | (indata[10 + (i * 6)] << 8));
                            res.FD.Add(fd);
                        }

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

                            if (!string.IsNullOrEmpty(Code))
                            {
                                Task.Run(() =>
                                {
                                    WriteLogCSV(Convert.ToBoolean(res.OR), DateTime.Now, res);
                                });
                            }

                            if (!IsLogDisable && IsLogEnable)
                            {
                                Task.Run(() =>
                                {
                                    WriteLog(res.CN, Convert.ToBoolean(res.OR), DateTime.Now, res);
                                });
                            }

                            DeviceCOM.IsLogDisable = false;

                        }
                    }
                    else if (indata[1] == 21)
                    {
                        IsSystemBusy = true;
                        busyStamp = System.DateTime.Now;
                    }
                    else if (indata[1] == 22)
                    {
                        IsSystemBusy = false;                        
                        if (indata[3] == 0)
                        {                            
                            if (IsBalanceBusyEnable)
                            {
                                IsResponseClearRequired = true;
                                IsBalanceBusyEnable = false;
                            }
                        }
                        else
                        {
                            ERRCode = indata[4];
                        }
                    }
                }
                else
                {

                }
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

                            if (!string.IsNullOrEmpty(Code))
                            {
                                Task.Run(() =>
                                {
                                    WriteLogCSV(Convert.ToBoolean(res.OR), DateTime.Now, res);
                                });
                            }

                            if (!IsLogDisable && IsLogEnable)
                            {
                                Task.Run(() =>
                                {
                                    WriteLog(res.CN, Convert.ToBoolean(res.OR), DateTime.Now, res);                                    
                                });
                            }

                            DeviceCOM.IsLogDisable = false;

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
                        ERRCode = res.ERR;
                        if (res.ERR != 16 & res.ERR != 17)
                        {
                            if (IsBalanceBusyEnable)
                            {
                                IsResponseClearRequired = true;
                                IsBalanceBusyEnable = false;
                            }
                        }
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }
        }

        public void WriteLog(int ChId, bool Result, DateTime TimeStamp, Response res)
        {
            try
            {
                using (var con = new NpgsqlConnection(ConnectionString))
                {
                    string sql = string.Empty;
                    con.Open();
                    var fdData = JsonConvert.SerializeObject(DeviceCOM.channelDatas.FirstOrDefault(r => r.Id == ChId).graphDatas);

                    var partData = JsonConvert.SerializeObject(DeviceCOM.part);
                    if (ChId == 1)
                    {
                        sql = "INSERT INTO public.\"Logs\"(\"ChId\", \"Result\", \"FDData\", \"PartData\", \"PartName\", \"BatchName\",\"SrNo\", \"BatchNo\" , \"TimeStamp\")\r\n\t" +
                            "VALUES (" +
                            ChId + ", '" +
                            Result + "', '" +
                            fdData + "', '" +
                            partData + "', '" +
                            DeviceCOM.part.Name + "', '" +
                            DeviceCOM.part.BatchName + "', '" +
                            Code + "', " +
                            DeviceCOM.part.BatchNo + ", '" +
                            TimeStamp + "'); SELECT count(1) \r\n\tFROM public.\"Logs\" where \"BatchName\" = '" + DeviceCOM.part.BatchName + "' and \"BatchNo\" = " + DeviceCOM.part.BatchNo + " ;";

                        var cmd = new NpgsqlCommand(sql, con);
                        var count = cmd.ExecuteScalar();

                        if (DeviceCOM.part.BatchType == 1)
                        {
                            if (Convert.ToInt32(count) == DeviceCOM.part.BatchSize)
                            {
                                // stop the logging 
                            }

                            DeviceCOM.part.BatchNo = DeviceCOM.part.BatchNo + 1;
                        }
                    }
                    else
                    {
                        if (!Result)
                        {
                            sql = "update public.\"Logs\"  set \"Result\" = 'false' where \"Id\" = (select max(\"Id\") from public.\"Logs\"); select 1";
                            var cmd = new NpgsqlCommand(sql, con);
                            var count = cmd.ExecuteScalar();
                        }

                        //if (ChId == 2)
                        //{
                        //    sql = "update public.\"Logs\"  set \"Ch2Result\" = '"+ Result + "' where \"Id\" = (select max(\"Id\") from public.\"Logs\"); select 1";
                        //    var cmd = new NpgsqlCommand(sql, con);
                        //    var count = cmd.ExecuteScalar();
                        //}
                        //else if (ChId == 3)
                        //{
                        //    sql = "update public.\"Logs\"  set \"Ch3Result\" = '" + Result + "' where \"Id\" = (select max(\"Id\") from public.\"Logs\"); select 1";
                        //    var cmd = new NpgsqlCommand(sql, con);
                        //    var count = cmd.ExecuteScalar();
                        //}
                        //else if (ChId == 4)
                        //{
                        //    sql = "update public.\"Logs\"  set \"Ch4Result\" = '" + Result + "' where \"Id\" = (select max(\"Id\") from public.\"Logs\"); select 1";
                        //    var cmd = new NpgsqlCommand(sql, con);
                        //    var count = cmd.ExecuteScalar();
                        //}
                    }
                }
            }
            catch (Exception ex)
            {

            }

            //Code = string.Empty;
        }

        private static void WriteLogCSV(bool Result, DateTime TimeStamp, Response res)
        {
            try
            {
                // Write to CSV File
                var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.Id == res.CN);
                if (ch != null)
                {
                    List<string> lines = new List<string>();
                    var FileName = "EddyLog_" + System.DateTime.Now.ToShortDateString();
                    string FilePath = System.Configuration.ConfigurationSettings.AppSettings["CSVPath"].ToString() + FileName + ".csv";
                    if (!File.Exists(FilePath))
                    {
                        string line = "TimeStamp,Code,Operator Name,Result";
                        foreach (var fd in ch.graphDatas)
                        {
                            line = line + ",Frequency Result_" + fd.Id.ToString() + ",Frequency_" + fd.Id.ToString();
                        }
                        lines.Add(line);
                    }

                    string data = System.DateTime.Now.ToString() + ","+ Code.Replace("\n", "").Replace("\r","") + "," + DeviceCOM.part.CheckedBy + "," + (Result == true ? "Ok" : "No Ok");

                    foreach (var fd in res.FD)
                    {
                        var Gdata = ch.graphDatas.FirstOrDefault(d => d.Id == fd.FN);
                        if (Gdata != null)
                        {
                            data = data + "," + (fd.R == 1 ? "Ok" : "No Ok") + "," + Gdata.freq.ToString();
                        }
                    }

                    lines.Add(data);

                    if (lines.Count > 0)
                    {
                        File.AppendAllLines(FilePath, lines);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public bool WriteDataInBytes(byte[] data, bool isFrombak = false)
        {

            try
            {
                IsBalanceRequired = false;
                if (CommunicationType == 0)
                {
                    if (isFrombak)
                    {
                        if (port.IsOpen)
                        {
                            port.Close();
                        }
                    }

                    if (!port.IsOpen)
                    {
                        port.Open();
                        if (isFrombak)
                        {
                            port.DtrEnable = true;
                            port.RtsEnable = true;
                            // Optional: short delay to let device recognize the signals
                            Thread.Sleep(10);
                        }
                    }

                    //this.port.ReadExisting();
                    PortAck = true;
                    PortData = "";

                    ushort crc = ComputeCRC(data, 2);

                    byte crcLow = (byte)(crc & 0xFF);
                    byte crcHigh = (byte)(crc >> 8);

                    data[data.Length - 2] = crcLow; 
                    data[data.Length - 1] = crcHigh; 

                    this.port.Write(data, 0 , data.Length);

                    if (isFrombak)
                    {
                        bool received = _ackEvent.WaitOne(1000);

                        if (!received)
                        {
                            this.port.Write(data, 0, data.Length);
                        }

                        received = _ackEvent.WaitOne(1000);

                        if (!received)
                        {
                            this.port.Write(data, 0, data.Length);
                        }

                        if (!received)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        System.DateTime dateTime = DateTime.Now;
                        while (PortAck)
                        {

                            if ((DateTime.Now - dateTime).TotalMilliseconds > 500)
                            {
                                PortAck = false;
                            }
                        }
                    }

                    byte[] result = new byte[1];
                    result[0] = Convert.ToByte(PortData[0]);

                    if (result[0] == '0' || result[0] == '2' || result[0] == '3' || result[0] == '4')
                    {
                        if (result[0] == '2')
                        {
                            DeviceCOM.IsSystemBusy = true;
                            busyStamp = System.DateTime.Now;
                        }
                        else if (result[0] == '3')
                        {
                            IsBalanceRequired = true;
                        }
                        else if (result[0] == '4')
                        {
                            IsBinRequired = true;
                        }
                        return true;
                    }

                    return false;
                }                
                return false;
            }
            catch (Exception ex)
            {
                if (CommunicationType == 1)
                {
                    dispatcherTimer.Start();
                }

                return false;
            }
        }

        public bool WriteData(string data, bool isFrombak = false)
        {
            try
            {
                IsBalanceRequired = false;
                if (CommunicationType == 0)
                {
                    if (isFrombak)
                    {
                        if (port.IsOpen)
                        {
                            port.Close();
                        }
                    }

                    if (!port.IsOpen)
                    {
                        port.Open();
                        if (isFrombak)
                        {
                            port.DtrEnable = true;
                            port.RtsEnable = true;
                            // Optional: short delay to let device recognize the signals
                            Thread.Sleep(10);
                        }
                    }

                    //this.port.ReadExisting();
                    PortAck = true;
                    PortData = "";                    
                    this.port.Write(data);

                    if (isFrombak)
                    {
                        bool received = _ackEvent.WaitOne(1000);

                        if (!received)
                        {
                            this.port.Write(data);
                        }

                        received = _ackEvent.WaitOne(1000);

                        if (!received)
                        {
                            this.port.Write(data);
                        }

                        if (!received)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        System.DateTime dateTime = DateTime.Now;
                        while (PortAck)
                        {

                            if ((DateTime.Now - dateTime).TotalMilliseconds > 500)
                            {
                                PortAck = false;
                            }
                        }
                    }
                    
                    byte[] result = new byte[1];
                    result[0] = Convert.ToByte(PortData[0]);

                    if (result[0] == '0' || result[0] == '2' || result[0] == '3' || result[0] == '4')
                    {
                        if (result[0] == '2')
                        {
                            DeviceCOM.IsSystemBusy = true;
                            busyStamp = System.DateTime.Now;
                        }
                        else if (result[0] == '3')
                        {
                            IsBalanceRequired = true;
                        }
                        else if (result[0] == '4')
                        {
                            IsBinRequired = true;
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
            catch (Exception ex)
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

                    //if (port.IsOpen)
                    //{
                    //    port.Close();
                    //}

                    //InitialPort(CommunicationType, PortName, BaudRate, IpAddress, SPort);

                    if (!port.IsOpen)
                    {
                        port.Open();
                    }
                    //this.port.ReadExisting();
                    PortAck = true;
                    PortData = "";
                    System.DateTime dateTime = DateTime.Now;
                    this.port.Write(data);

                    while (PortAck)
                    {

                        if ((DateTime.Now - dateTime).TotalMilliseconds > 200)
                        {
                            PortAck = false;
                        }
                    }

                    byte[] result = new byte[1];
                    result[0] = Convert.ToByte(PortData[0]);

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

        public bool GetSystemStatusInBytes(byte[] data)
        {
            try
            {
                if (CommunicationType == 0)
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }

                    //if (port.IsOpen)
                    //{
                    //    port.Close();
                    //}

                    //InitialPort(CommunicationType, PortName, BaudRate, IpAddress, SPort);

                    if (!port.IsOpen)
                    {
                        port.Open();
                    }
                    //this.port.ReadExisting();
                    PortAck = true;
                    PortData = "";
                    System.DateTime dateTime = DateTime.Now;

                    ushort crc = ComputeCRC(data, 2);

                    byte crcLow = (byte)(crc & 0xFF);
                    byte crcHigh = (byte)(crc >> 8);

                    data[data.Length - 2] = crcLow;
                    data[data.Length - 1] = crcHigh;

                    this.port.Write(data, 0, data.Length);

                    while (PortAck)
                    {

                        if ((DateTime.Now - dateTime).TotalMilliseconds > 200)
                        {
                            PortAck = false;
                        }
                    }

                    byte[] result = new byte[1];
                    result[0] = Convert.ToByte(PortData[1]);

                    if (result[0] == 21)
                    {
                        DeviceCOM.IsSystemBusy = true;
                        busyStamp = System.DateTime.Now;
                    }
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

        public GetSerialNumber GetSeialNumber()
        {
            SetSerialNumber setSerialNumber = new SetSerialNumber { FC = 31, S1 = 1234, S2 = 5678};
            string data = JsonConvert.SerializeObject(setSerialNumber);
            GetSerialNumber getSerialNumber = new GetSerialNumber(); 
            try
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
                Thread.Sleep(100);
                string sData = this.port.ReadExisting();

                if (port.IsOpen)
                {
                    port.Close();
                }

                port.DataReceived += serialPort_DataReceived;
                if (!port.IsOpen)
                {
                    port.Open();
                }
                
                if (!string.IsNullOrEmpty(sData))
                {
                    getSerialNumber = JsonConvert.DeserializeObject<GetSerialNumber>(sData);
                    getSerialNumber.S1 = setSerialNumber.S1;
                    getSerialNumber.S2 = setSerialNumber.S2;

                }
            }
            catch (Exception e)
            {
                
            }

            return getSerialNumber;
        }

        public static ushort ComputeCRC(byte[] data, int length)
        {
            ushort crc = 0xFFFF;

            for (int pos = 0; pos < length; pos++)
            {
                crc ^= data[pos];

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }

    }
    public class ChannelData
    {
        public int Id = 0;
        public bool IsSeleted = false;
        public int TxStrength = 100;
        public List<GraphData> graphDatas;
    }

    public class PartTypeData
    {
        public string Name { get; set; }
        public List<string> Values { get; set; }
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
    public class Ellips
    {
        public int Id = 0;
        public double height = DeviceCOM.DefaultHeight;
        public double width = DeviceCOM.DefaultWidth;
        public double ex = 0;
        public double ey = 0;
        public double angel = 0;
    }

    public class EllipsDTO
    {
        public int Id { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public double ex { get; set; }
        public double ey { get; set; }
        public double angel { get; set; }
        public string ColorName { get; set; }
        public string ChannelName { get; set; }
    }
    
    public class Response
    {
        public int FC;
        public int CN;
        public int OR;
        public bool IsBalacenced = false;
        public List<FreqResult> FD;
        public int ERR { get; set; }
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
        public int T;
        public List<Frequency> FD;
    }
    public class Frequency
    {
        public int FN;
        public int F;
        public int G;
        public int P;
        public int E;
        //public int T;
        
    }
    public class Frequ
    {
        public int FN;
        public List<Elliplse> ED;
    }
    public class ElliplseWrite
    {
        public int FC;
        public int CN;
        public List<Frequ> FD;
    }
    public class Elliplse
    {
        public int FN;
        public int EId;
        public double a;
        public double b;
        public double t;
        public int x;
        public int y;
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
        public OuterElliplse OE;
    }

    public class OuterElliplse
    {
        public double a;
        public double b;
        public double t;
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

    public class Operator
    {
        public int Id { get; set; }
        public string OperatorName { get; set; }
        public bool IsActive { get; set; }
    }

    public class PartMaster
    {
        public int Id { get; set; }
        public int PartFamilyId { get; set; }
        public string PartNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class PartFamily
    {
        public int Id { get; set; }
        public string FamilyName { get; set; }
        public bool IsActive { get; set; }
    }

    public class BatchDetail
    {
        public DateTime TimeStamp { get; set; }
        public string BatchName { get; set; }
        public bool Result { get; set; }
        public string FDData { get; set; }
        public string PartData { get; set; }
    }
    public class PartConfiguration
    {
        public string BatchName { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }
        public string CheckedBy { get; set; }
        public string CompanyName { get; set; }

        public string ProductionOrder { get; set; }
        public string MachineNumber { get; set; }
        public string PartNumber { get; set; }
        public string PartFamily { get; set; }
    }

    public class SetSerialNumber
    {
        public int FC;
        public int S1;
        public int S2;
    }


    public class GetSerialNumber
    {
        public int FC;
        public string S;
        public int S1;
        public int S2;
    }

    public class MyColor
    {
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

    public class LogData
    {
        public string BatchName { get; set; }
        public string LogStartDate { get; set; }
        public string LogEndDate { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int TotalCount { get; set; }

        // IsReNewConfig
        public DateTime LogDateRaw { get; set; }
        public string ProductionOrder { get; set; }   
        public string Date { get; set; }

    }

    public class LogData1
    {
        public string BatchName { get; set; }
        public string PartName { get; set; }
        public string SrNo { get; set; }
        public string TimeStamp { get; set; }
        public string ResultStatus { get; set; }
        public string Ch1Result { get; set; }
        public string Ch2Result { get; set; }
        public string Ch3Result { get; set; }
        public string Ch4Result { get; set; }

    }

    public class SerialPortManager
    {
        private SerialPort _serialPort;

        public SerialPortManager(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            // Subscribe to the DataReceived event
            _serialPort.DataReceived += OnDataReceived;
        }

        public void Open()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                Console.WriteLine($"Serial port {_serialPort.PortName} opened at {_serialPort.BaudRate} baud.");
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                Console.WriteLine("Serial port closed.");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Read all available data
                string data = _serialPort.ReadExisting();
                Console.WriteLine("Received: " + data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading data: " + ex.Message);
            }
        }
    }
}


