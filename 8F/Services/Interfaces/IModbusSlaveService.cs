using System;

namespace _8F.Services.Interfaces
{
    public interface IModbusSlaveService
    {
        bool IsRunning { get; }
        int Port { get; }
        ushort ReadRegister();
        void WriteRegister(ushort value);
        void Start(int port = 502);
        void Stop();
        event EventHandler<ushort>? RegisterValueChanged;
    }
}
