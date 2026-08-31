using System;
using System.Windows;
using System.Windows.Threading;
using _8F.Services.Interfaces;

namespace _8F.Views
{
    public partial class ModbusTestPanel : Window
    {
        private readonly IModbusSlaveService? _modbusService;
        private readonly DispatcherTimer _timer;

        public ModbusTestPanel(IModbusSlaveService modbusService)
        {
            InitializeComponent();
            _modbusService = modbusService;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += (s, e) => RefreshRegisterDisplay();
            _timer.Start();

            if (_modbusService != null)
            {
                _modbusService.RegisterValueChanged += ModbusService_RegisterValueChanged;
            }

            UpdateServerStatus();
            RefreshRegisterDisplay();
        }

        private void ModbusService_RegisterValueChanged(object? sender, ushort e)
        {
            Dispatcher.Invoke(RefreshRegisterDisplay);
        }

        private void UpdateServerStatus()
        {
            if (_modbusService != null && _modbusService.IsRunning)
            {
                lblStatus.Text = $"Active (Listening on TCP Port {_modbusService.Port})";
                lblStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                lblStatus.Text = "Disabled / Stopped";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void RefreshRegisterDisplay()
        {
            if (_modbusService != null)
            {
                ushort val = _modbusService.ReadRegister();
                lblRegisterValue.Text = val.ToString();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshRegisterDisplay();
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (_modbusService == null || !_modbusService.IsRunning)
            {
                MessageBox.Show("Modbus TCP Slave server is not running. Please enable 'IsModbusServerEnable' in App.config.", "Modbus Server Off", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ushort.TryParse(txtWriteValue.Text.Trim(), out ushort val))
            {
                _modbusService.WriteRegister(val);
                RefreshRegisterDisplay();
                MessageBox.Show($"Holding register 0 updated to {val}!", "Modbus Write", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid ushort integer value between 0 and 65535.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            if (_modbusService != null)
            {
                _modbusService.RegisterValueChanged -= ModbusService_RegisterValueChanged;
            }
            base.OnClosed(e);
        }
    }
}
