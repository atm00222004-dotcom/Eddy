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
using _8F.Models;

namespace _8F
{
    public class DeviceCOM : IDeviceCommunication, IInspectionLogger
    {
        public static readonly IInspectionLogger Logger = new InspectionLogger();
        public SerialPort port = default!;
        public static List<ChannelData> channelDatas = default!;
        public static readonly object QueueLock = new object();
        public static List<Response> responses = default!;
        public static List<CordinateQueue> cordinateQueue = default!;
        public static bool IsResponseRefreshRequired = false;
        public static bool IsBalanceAll = false;
        public static bool IsBalanceBusyEnable = false;
        public static bool IsResponseClearRequired = false;
        public static bool IsTraceResetRequired = false;
        public static bool hasCurrentTraceBeenEvaluated = false;
        public static bool hasAlreadyClearedForThisDuplicate = false;
        public static bool isCurrentPartEvaluated = false;
        public static bool isWaitingForNextPart = false;
        public static int CommunicationType;
        public static string PortName = default!;
        public static int BaudRate;
        public static string IpAddress = default!;
        public static int SPort;
        public static int ChannelNo = 4;
        public static int DefaultHeight = 0;
        public static int DefaultWidth = 0;
        public static int DefaultHeight_O = 0;
        public static int DefaultWidth_O = 0;
        public static int DefaultAngel_O = 0;
        public static string ConnectionString = default!;
        public static bool IsLogEnable = false;
        public static bool IsSystemBusy = false;
        public static DateTime busyStamp = System.DateTime.Now; 
        public static Part part = default!;
        public static List<Counter> counter = default!;
        public static bool IsLogDisable = false;
        public static bool IsBalanceRequired = false;
        public static bool IsBinRequired = false;
        public static int ERRCode = 0;
        public static string Code = default!;
        public static bool IsJSON = false;

        public static byte[] receiveBytes = default!;

        TcpClient client = default!;
        NetworkStream stream = default!;
        public static bool PortAck = false ;
        public static string PortData = "";
        private ManualResetEvent _ackEvent = new ManualResetEvent(false);
        internal static int Mode;

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
                client = new TcpClient { NoDelay = true };
                StartTcpReceiveLoop();
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

        private System.Threading.CancellationTokenSource? _tcpCts;

        public void StartTcpReceiveLoop()
        {
            _tcpCts?.Cancel();
            _tcpCts = new System.Threading.CancellationTokenSource();
            Task.Run(() => TcpReceiveLoopAsync(_tcpCts.Token));
        }

        public void StopTcpReceiveLoop()
        {
            _tcpCts?.Cancel();
        }

        private async Task TcpReceiveLoopAsync(System.Threading.CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[8192];
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (client == null)
                    {
                        client = new TcpClient { NoDelay = true };
                    }
                    client.NoDelay = true;

                    if (!client.Connected)
                    {
                        try
                        {
                            IPAddress iPAddress = IPAddress.Parse(IpAddress);
                            await client.ConnectAsync(iPAddress, SPort, cancellationToken);
                            client.NoDelay = true;
                        }
                        catch
                        {
                            await Task.Delay(1000, cancellationToken);
                            continue;
                        }
                    }

                    if (client.Connected)
                    {
                        stream = client.GetStream();
                        int received = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (received > 0)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, received);
                            _ = Task.Run(() => ProcessPortData(message));
                        }
                        else
                        {
                            await Task.Delay(50, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
        }


        private void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
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

                    Task.Run(() =>
                    {
                        if (IsJSON)
                        {
                            ProcessPortData(indata);
                        }
                        else
                        {
                            ProcessPortDataBytpe(data);
                        }
                    });
                }

            }
            catch (Exception)
            {

            }
        }
        private void ProcessPortDataBytpe(short[] indata)
        {
            try
            {
                if (indata.Length > 1)
                {


                    if (indata[1] == 19)
                    {
                        isCurrentPartEvaluated = false;
                        IsResponseClearRequired = true;
                    }
                    else if (indata[1] == 20)
                    {
                        //var res = JsonConvert.DeserializeObject<Response>(indata);
                        Response res = new Response();
                        res.CN = indata[3];
                        res.OR = indata[4];
                        res.FC = 20;
                        int length = indata[2] / 6;
                        res.FD = new List<FreqResult>();
                        for (int i = 0; i < length; i++)
                        {
                            FreqResult fd = new FreqResult();
                            fd.FN = indata[5 + (i * 6)];
                            fd.R = indata[6 + (i * 6)];

                            int offset = 7 + (i * 6);
                            short x = (short)((ushort)indata[offset] | (ushort)(indata[offset + 1] << 8));
                            short y = (short)((ushort)indata[offset + 2] | (ushort)(indata[offset + 3] << 8));
                            fd.X = x;
                            fd.Y = y;


                            //fd.X = (short)(indata[7 + (i * 6)] | (indata[8 + (i * 6)] << 8));
                            //fd.Y = (short)(indata[9 + (i * 6)] | (indata[10 + (i * 6)] << 8));
                            res.FD.Add(fd);
                        }

                        if (ChannelNo >= res?.CN)
                        {
                            lock (QueueLock)
                            {
                                responses.Add(res);
                                if (responses.Count > 5000)
                                {
                                    responses.RemoveRange(0, responses.Count - 5000);
                                }

                            }

                            var cnt = counter.FirstOrDefault(c => c.Id == res.CN);
                            if (cnt != null)
                            {
                                if (res.OR == 1)
                                {
                                    cnt.ResultOkCount = cnt.ResultOkCount + 1;
                                }
                                else
                                {
                                    cnt.ResultOkNotCount = cnt.ResultOkNotCount + 1;
                                }
                                cnt.ResultCount = cnt.ResultOkCount + cnt.ResultOkNotCount;
                            }
                            isWaitingForNextPart = true;
                            IsResponseRefreshRequired = true;
                            // IsTraceResetRequired = true;

                            //if (!string.IsNullOrEmpty(Code))
                            //{
                            //    Task.Run(() =>
                            //    {
                            //        WriteLogCSV(Convert.ToBoolean(res.OR), DateTime.Now, res);
                            //    });
                            //}

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
            catch (Exception)
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

                    if (res != null)
                    {
                        if (res.FC == 19)
                        {
                            isCurrentPartEvaluated = false;
                            IsResponseClearRequired = true;
                        }
                        else if (res.FC == 20)
                    {
                        if (ChannelNo >= res.CN)
                        {
                            lock (QueueLock)
                            {
                                responses.Add(res);
                                if (responses.Count > 5000)
                                {
                                    responses.RemoveRange(0, responses.Count - 5000);
                                }

                                if (hasCurrentTraceBeenEvaluated)
                                {
                                    if (!hasAlreadyClearedForThisDuplicate)
                                    {
                                        cordinateQueue.Clear();
                                        IsTraceResetRequired = true;
                                        hasAlreadyClearedForThisDuplicate = true;
                                        _8F.Services.DiagnosticLogger.Log("DECOUPLE_TRACE", $"Stale trace cleared on duplicate FC20 evaluation (CN={res.CN})");
                                    }
                                }
                                else
                                {
                                    hasCurrentTraceBeenEvaluated = true;
                                    hasAlreadyClearedForThisDuplicate = false;
                                }
                            }

                            var cnt = counter.FirstOrDefault(c => c.Id == res.CN);
                            if (cnt != null)
                            {
                                if (res.OR == 1)
                                {
                                    cnt.ResultOkCount = cnt.ResultOkCount + 1;
                                }
                                else
                                {  
                                    cnt.ResultOkNotCount = cnt.ResultOkNotCount + 1;
                                }
                                cnt.ResultCount = cnt.ResultOkCount + cnt.ResultOkNotCount;
                            }
                            isWaitingForNextPart = true;
                            IsResponseRefreshRequired = true;

                            string fdSummary = res.FD != null ? string.Join("; ", res.FD.Select(f => $"FN={f.FN}:X={f.X},Y={f.Y},R={f.R}")) : "null";
                            _8F.Services.DiagnosticLogger.Log("FC20_EVAL", $"CN={res.CN}, OR={res.OR}, TotalCount={cnt?.ResultCount}, isWaitingForNextPart=true, FD=[{fdSummary}]");
                            // IsTraceResetRequired = true;

                            //if (!string.IsNullOrEmpty(Code))
                            //{
                            //    Task.Run(() =>
                            //    {
                            //        WriteLogCSV(Convert.ToBoolean(res.OR), DateTime.Now, res);
                            //    });
                            //}

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
                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
        }

        public void WriteLog(int ChId, bool Result, DateTime TimeStamp, Response res)
        {
            Logger.WriteLog(ChId, Result, TimeStamp, res);
        }

        public static string GetCSVDirectoryPath()
        {
            return Logger.GetCSVDirectoryPath();
        }

        string IInspectionLogger.GetCSVDirectoryPath()
        {
            return Logger.GetCSVDirectoryPath();
        }

        public void WriteLogCSV(bool Result, DateTime TimeStamp, Response res)
        {
            Logger.WriteLogCSV(Result, TimeStamp, res);
        }

        public Task<bool> WriteDataAsync(string data, bool isFrombak = false)
        {
            return Task.Run(() => WriteData(data, isFrombak));
        }

        public Task<bool> WriteDataInBytesAsync(byte[] data, bool isFrombak = false)
        {
            return Task.Run(() => WriteDataInBytes(data, isFrombak));
        }

        public Task<bool> GetSystemStatusAsync(string data)
        {
            return Task.Run(() => GetSystemStatus(data));
        }

        public Task<bool> GetSystemStatusInBytesAsync(byte[] data)
        {
            return Task.Run(() => GetSystemStatusInBytes(data));
        }

        public bool WriteData(string data, bool isFrombak = false)
        {
            bool success = false;
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
                    
                    if (!string.IsNullOrEmpty(PortData) && PortData.Length > 0)
                    {
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
                            success = true;
                            return true;
                        }
                    }

                    return false;
                }
                else if (CommunicationType == 1)
                {
                    if (!client.Connected)
                    {
                        client = new TcpClient { NoDelay = true };
                        IPAddress iPAddress = IPAddress.Parse(IpAddress);
                        var ipEndPoint = new IPEndPoint(iPAddress, SPort);
                        client.Connect(ipEndPoint);
                        client.NoDelay = true;
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
                                success = true;
                                return true;
                            }
                        }
                        success = true;
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                PortAck = false;
                PortData = "";
                if (!success)
                {
                    IsSystemBusy = false;
                    if (CommunicationType == 0 && port != null && port.IsOpen)
                    {
                        try
                        {
                            port.DiscardInBuffer();
                        }
                        catch { }
                    }
                }
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

                    if (!string.IsNullOrEmpty(PortData) && PortData.Length > 0)
                    {
                        byte[] result = new byte[1];
                        result[0] = Convert.ToByte(PortData[0]);

                        if (result[0] == 21)
                        {
                            DeviceCOM.IsSystemBusy = true;
                            busyStamp = System.DateTime.Now;
                        }
                    }
                }
                else if (CommunicationType == 1)
                {
                    if (!client.Connected)
                    {
                        client = new TcpClient { NoDelay = true };
                        IPAddress iPAddress = IPAddress.Parse(IpAddress);
                        var ipEndPoint = new IPEndPoint(iPAddress, SPort);
                        client.Connect(ipEndPoint);
                        client.NoDelay = true;
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
                        if (buffer[0] == 21)
                        {
                            DeviceCOM.IsSystemBusy = true;
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
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

                if (!port.IsOpen)
                {
                    port.Open();
                }
                
                if (!string.IsNullOrEmpty(sData))
                {
                    var parsed = JsonConvert.DeserializeObject<GetSerialNumber>(sData);
                    if (parsed != null)
                    {
                        getSerialNumber = parsed;
                        getSerialNumber.S1 = setSerialNumber.S1;
                        getSerialNumber.S2 = setSerialNumber.S2;
                    }
                }
            }
            catch (Exception)
            {
                
            }

            return getSerialNumber;
        }

        public bool WriteDataInBytes(byte[] data, bool isFrombak = false)
        {
            bool success = false;
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

                    this.port.Write(data, 0, data.Length);

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

                    if (!string.IsNullOrEmpty(PortData) && PortData.Length > 0)
                    {
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
                            success = true;
                            return true;
                        }
                    }

                    return false;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                PortAck = false;
                PortData = "";
                if (!success)
                {
                    IsSystemBusy = false;
                    if (CommunicationType == 0 && port != null && port.IsOpen)
                    {
                        try
                        {
                            port.DiscardInBuffer();
                        }
                        catch { }
                    }
                }
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

                    if (!string.IsNullOrEmpty(PortData) && PortData.Length > 1)
                    {
                        byte[] result = new byte[1];
                        result[0] = Convert.ToByte(PortData[1]);

                        if (result[0] == 21)
                        {
                            DeviceCOM.IsSystemBusy = true;
                            busyStamp = System.DateTime.Now;
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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


