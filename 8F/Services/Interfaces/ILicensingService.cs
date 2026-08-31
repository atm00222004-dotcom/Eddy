namespace _8F.Services
{
    public interface ILicensingService
    {
        bool ValidateSerialNumber(object? serial1, object? serial2, string? serialFull, string configSerial);
        string ReverseString(string input);
    }
}
