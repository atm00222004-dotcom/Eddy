using System.Threading.Tasks;
using _8F.Models;

namespace _8F.Services.Interfaces
{
    public interface IDeviceCommunication
    {
        void InitialPort(int communicationType, string portName, int baudRate, string ipAddress, int sport);
        void StartTcpReceiveLoop();
        void StopTcpReceiveLoop();
        Task<bool> WriteDataAsync(string data, bool isFrombak = false);
        Task<bool> WriteDataInBytesAsync(byte[] data, bool isFrombak = false);
        Task<bool> GetSystemStatusAsync(string data);
        Task<bool> GetSystemStatusInBytesAsync(byte[] data);
        bool WriteData(string data, bool isFrombak = false);
        bool GetSystemStatus(string data);
        GetSerialNumber GetSeialNumber();
        bool WriteDataInBytes(byte[] data, bool isFrombak = false);
        bool GetSystemStatusInBytes(byte[] data);
    }
}
