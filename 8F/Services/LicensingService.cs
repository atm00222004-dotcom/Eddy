using System;

namespace _8F.Services
{
    public class LicensingService : ILicensingService
    {
        public string ReverseString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public bool ValidateSerialNumber(object? serial1, object? serial2, string? serialFull, string configSerial)
        {
            if (string.IsNullOrEmpty(serialFull)) return false;
            string s1 = serial1?.ToString() ?? string.Empty;
            string s2 = serial2?.ToString() ?? string.Empty;
            string scrambled = ReverseString(s1 + configSerial + s2);
            return scrambled == serialFull;
        }
    }
}
