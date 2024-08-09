using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8F
{
    public class PortCOM
    {
        public SerialPort port;
        public static List<ChannelData> channelDatas;
        public static List<Response> responses;
        public static bool IsResponseRefreshRequired = false;
        public static int ResultCount = 0;
        public void InitialPort(string portName)
        {
            port = new SerialPort
            {
                BaudRate = 115200,
                DataBits = 8,
                Handshake = Handshake.None,
                Parity = Parity.None,
                PortName = portName,
                StopBits = StopBits.One,
                ReadTimeout = 500,
                WriteTimeout = 2000
            };
            responses = new List<Response>();
            //port.DataReceived +=serialPort_DataReceived;
        }
        public bool ReadFreqAndGain()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }

            this.port.ReadExisting();
            this.port.Write("0");
            int toread = 91;
            int offset = 0;
            char[] result = new char[toread];
            while (toread > 0)
            {
                int r = this.port.Read(result, offset, toread);
                offset += r;
                toread -= r;
            }

            var FreqAndGainData = new string(result).Split(',');

            return true;
        }
        public bool WriteFreqAndGain(string chId, string frenq, string gain)
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
            this.port.ReadExisting();
            this.port.Write("6");
            int toread = 3;
            int offset = 0;
            char[] result = new char[toread];
            while (toread > 0)
            {
                int r = this.port.Read(result, offset, toread);
                offset += r;
                toread -= r;
            }

            if (result[2] == '1')
            {
                toread = 12;
                offset = 0;
                char[] result1 = new char[toread];
                this.port.ReadExisting();
                string dataToWrite = chId + frenq + gain;
                this.port.Write(dataToWrite);
                while (toread > 0)
                {
                    int r = this.port.Read(result1, offset, toread);
                    offset += r;
                    toread -= r;
                }

                if (result1[11] == '1')
                {
                    return true;
                }
            }

            return false;
        }

        private void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadExisting();
                if (!string.IsNullOrEmpty(indata))
                {
                    var res = JsonConvert.DeserializeObject<Response>(indata);
                    //foreach (var item in res.FD)
                    //{
                    //    item.X = item.X / 10;
                    //    item.Y = item.X / 10;
                    //}
                    responses.Add(res);
                    ResultCount = ResultCount + 1;
                    IsResponseRefreshRequired = true;
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

                port.DataReceived += null;
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
        public bool ReadGraphData()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }

            this.port.ReadExisting();
            this.port.Write("4");
            int toread = 19;
            int offset = 0;
            char[] result = new char[toread];
            while (toread > 0)
            {
                int r = this.port.Read(result, offset, toread);
                offset += r;
                toread -= r;
            }

            var GraphData = new string(result).Split(',');

            return true;
        }
        public bool WriteBalance()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
            this.port.ReadExisting();
            this.port.Write("1");
            int toread = 3;
            int offset = 0;
            char[] result = new char[toread];
            while (toread > 0)
            {
                int r = this.port.Read(result, offset, toread);
                offset += r;
                toread -= r;
            }

            if (result[2] == '1')
            {
                return true;
            }

            return false;
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
        public int freq = 100;
        public int gain = 10;
        public int phase = 10;
        public double height = 2000;
        public double width = 1400;
        public double ex = -660;
        public double ey = -960;
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
