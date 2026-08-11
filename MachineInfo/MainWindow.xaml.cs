using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace MachineInfo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtMachineId.Text = GetMachineId();
        }

        private string GetMachineId()
        {
            try
            {
                string cpuId =
                    GetWmiValue("Win32_Processor", "ProcessorId");

                string biosSerial =
                    GetWmiValue("Win32_BIOS", "SerialNumber");

                string boardSerial =
                    GetWmiValue("Win32_BaseBoard", "SerialNumber");

                string systemUuid =
                    GetWmiValue("Win32_ComputerSystemProduct", "UUID");

                string diskSerial =
                    GetWmiValue("Win32_DiskDrive", "SerialNumber");

                // Normalize all values
                cpuId = Normalize(cpuId);
                biosSerial = Normalize(biosSerial);
                boardSerial = Normalize(boardSerial);
                systemUuid = Normalize(systemUuid);
                diskSerial = Normalize(diskSerial);

                // Check whether we got at least some hardware information
                if (string.IsNullOrWhiteSpace(cpuId) &&
                    string.IsNullOrWhiteSpace(biosSerial) &&
                    string.IsNullOrWhiteSpace(boardSerial) &&
                    string.IsNullOrWhiteSpace(systemUuid) &&
                    string.IsNullOrWhiteSpace(diskSerial))
                {
                    return "Unable to generate Machine ID";
                }

                // IMPORTANT:
                // Use exactly this same format in your Eddy application.
                string combined =
                    $"CPU:{cpuId}|" +
                    $"BIOS:{biosSerial}|" +
                    $"BOARD:{boardSerial}|" +
                    $"UUID:{systemUuid}|" +
                    $"DISK:{diskSerial}";

                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash =
                        sha.ComputeHash(
                            Encoding.UTF8.GetBytes(combined));

                    StringBuilder sb = new StringBuilder();

                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("X2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToUpperInvariant();
        }

        private string GetWmiValue(string className,string propertyName)
        {
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        $"SELECT {propertyName} FROM {className}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString() ?? "";
                    }
                }
            }
            catch
            {
            }

            return "";
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtMachineId.Text);
            MessageBox.Show("Machine ID copied successfully.",
                            "Machine Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}