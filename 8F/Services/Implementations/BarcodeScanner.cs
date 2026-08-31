using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _8F.Services
{
    public class BarcodeScanner
    {
        private readonly StringBuilder _buffer = new();
        private readonly DispatcherTimer _timer;

        public event EventHandler<string>? BarcodeScanned;

        public BarcodeScanner()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (s, e) =>
            {
                if (_buffer.Length > 0)
                {
                    string code = _buffer.ToString();
                    _buffer.Clear();
                    BarcodeScanned?.Invoke(this, code);
                }
                _timer.Stop();
            };
        }

        public void HandleKey(KeyEventArgs e)
        {
            char c = GetCharFromKey(e.Key);
            if (c == '\0')
                return;

            if (e.Key == Key.Enter)
            {
                string code = _buffer.ToString();
                _buffer.Clear();
                _timer.Stop();
                BarcodeScanned?.Invoke(this, code);
            }
            else
            {
                _buffer.Append(c);
                _timer.Stop();
                _timer.Start();
            }
        }

        private static char GetCharFromKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
                return (char)('A' + (key - Key.A));
            if (key >= Key.D0 && key <= Key.D9)
                return (char)('0' + (key - Key.D0));
            if (key == Key.OemMinus)
                return '-';
            if (key == Key.Space)
                return ' ';
            if (key == Key.Enter)
                return '\r';

            return '\0';
        }
    }

}
