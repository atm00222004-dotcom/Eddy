using System;

namespace _8F.Services.Interfaces
{
    public interface IModbusSlaveService
    {
        bool IsRunning { get; }
        int Port { get; }
        ushort ReadRegister();
        void WriteRegister(ushort value);
        void Start(int port = 5020);
        void Stop();
        event EventHandler<ushort>? RegisterValueChanged;
    }
}
