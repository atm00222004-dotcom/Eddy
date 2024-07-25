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
}
