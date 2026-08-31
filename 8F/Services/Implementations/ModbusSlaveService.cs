using NModbus;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using _8F.Services.Interfaces;

namespace _8F.Services.Implementations
{
    public class ModbusSlaveService : IModbusSlaveService
    {
        private TcpListener? _tcpListener;
        private IModbusSlaveNetwork? _slaveNetwork;
        private IModbusSlave? _slave;
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }
        public int Port { get; private set; } = 502;

        public event EventHandler<ushort>? RegisterValueChanged;

        public ushort ReadRegister()
        {
            lock (_lock)
            {
                if (_slave?.DataStore?.HoldingRegisters != null)
                {
                    var points = _slave.DataStore.HoldingRegisters.ReadPoints(0, 1);
                    if (points != null && points.Length > 0)
                    {
                        return points[0];
                    }
                }
                return 0;
            }
        }

        public void WriteRegister(ushort value)
        {
            lock (_lock)
            {
                if (_slave?.DataStore?.HoldingRegisters != null)
                {
                    _slave.DataStore.HoldingRegisters.WritePoints(0, new[] { value });
                }
            }

            RegisterValueChanged?.Invoke(this, value);
        }

        public void Start(int port = 502)
        {
            if (IsRunning) return;

            try
            {
                Port = port;
                _tcpListener = new TcpListener(IPAddress.Any, Port);
                _tcpListener.Start();

                var factory = new ModbusFactory();
                _slaveNetwork = factory.CreateSlaveNetwork(_tcpListener);

                byte unitId = 1;
                _slave = factory.CreateSlave(unitId);

                // Initialize holding register at starting address 0
                _slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[] { 0 });

                _slaveNetwork.AddSlave(_slave);

                Task.Run(async () =>
                {
                    try
                    {
                        await _slaveNetwork.ListenAsync();
                    }
                    catch { }
                });

                IsRunning = true;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                Stop();
                throw new InvalidOperationException($"Failed to start Modbus TCP Slave on port {port}: {ex.Message}", ex);
            }
        }

        public void Stop()
        {
            try
            {
                _slaveNetwork?.Dispose();
                _tcpListener?.Stop();
            }
            catch { }
            finally
            {
                _slaveNetwork = null;
                _slave = null;
                _tcpListener = null;
                IsRunning = false;
            }
        }
    }
}
