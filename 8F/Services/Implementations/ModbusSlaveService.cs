using NModbus;
using NModbus.Data;
using NModbus.Device;
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
        private Timer? _heartbeatTimer;
        private ushort _heartbeatValue = 0;
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }
        public int Port { get; private set; } = 502;
        public int HeartbeatIntervalMs { get; private set; } = 1000;

        public event EventHandler<ushort>? RegisterValueChanged;
        public event EventHandler<ushort>? HeartbeatValueChanged;

        public ushort ReadRegister(ushort address = 0)
        {
            lock (_lock)
            {
                if (_slave?.DataStore?.HoldingRegisters != null)
                {
                    var points = _slave.DataStore.HoldingRegisters.ReadPoints(address, 1);
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
            WriteRegister(0, value);
        }

        public void WriteRegister(ushort address, ushort value)
        {
            lock (_lock)
            {
                if (_slave?.DataStore?.HoldingRegisters != null)
                {
                    _slave.DataStore.HoldingRegisters.WritePoints(address, new ushort[] { value });
                }
            }

            if (address == 0)
            {
                RegisterValueChanged?.Invoke(this, value);
            }
            else if (address == 1)
            {
                _heartbeatValue = value;
                HeartbeatValueChanged?.Invoke(this, value);
            }
        }

        public void Start(int port = 502, int heartbeatIntervalMs = 1000)
        {
            if (IsRunning) return;

            try
            {
                Port = port;
                HeartbeatIntervalMs = heartbeatIntervalMs > 0 ? heartbeatIntervalMs : 1000;
                _tcpListener = new TcpListener(IPAddress.Any, Port);
                _tcpListener.Start();

                var factory = new ModbusFactory();
                _slaveNetwork = factory.CreateSlaveNetwork(_tcpListener);

                byte unitId = 1;
                _slave = factory.CreateSlave(unitId);

                // Initialize holding registers: Address 0 = Profile ID (0), Address 1 = Heartbeat (0)
                _slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[] { 0, 0 });

                if (_slave.DataStore.HoldingRegisters is PointSource<ushort> ps)
                {
                    ps.AfterWrite += (s, e) =>
                    {
                        if (e.StartAddress == 0)
                        {
                            ushort val = ReadRegister(0);
                            RegisterValueChanged?.Invoke(this, val);
                        }
                        else if (e.StartAddress == 1)
                        {
                            ushort val = ReadRegister(1);
                            _heartbeatValue = val;
                            HeartbeatValueChanged?.Invoke(this, val);
                        }
                    };
                }

                _slaveNetwork.AddSlave(_slave);

                Task.Run(async () =>
                {
                    try
                    {
                        await _slaveNetwork.ListenAsync();
                    }
                    catch { }
                });

                // Start heartbeat timer
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = new Timer(OnHeartbeatTick, null, HeartbeatIntervalMs, HeartbeatIntervalMs);

                IsRunning = true;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                Stop();
                throw new InvalidOperationException($"Failed to start Modbus TCP Slave on port {port}: {ex.Message}", ex);
            }
        }

        private void OnHeartbeatTick(object? state)
        {
            if (!IsRunning || _slave?.DataStore?.HoldingRegisters == null) return;

            ushort newVal;
            lock (_lock)
            {
                _heartbeatValue = (ushort)((_heartbeatValue + 1) % 65536);
                newVal = _heartbeatValue;
                _slave.DataStore.HoldingRegisters.WritePoints(1, new ushort[] { newVal });
            }

            HeartbeatValueChanged?.Invoke(this, newVal);
        }

        public void Stop()
        {
            try
            {
                _heartbeatTimer?.Dispose();
                _slaveNetwork?.Dispose();
                _tcpListener?.Stop();
            }
            catch { }
            finally
            {
                _heartbeatTimer = null;
                _slaveNetwork = null;
                _slave = null;
                _tcpListener = null;
                IsRunning = false;
            }
        }
    }
}
