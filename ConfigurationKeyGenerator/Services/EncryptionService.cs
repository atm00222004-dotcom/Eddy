using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConfigurationKeyGenerator.Services
{
    public class EncryptionService
    {
        // 32 bytes = AES-256 Key
        private static readonly byte[] Key =Encoding.UTF8.GetBytes("12345678901234567890123456789012");

        // 16 bytes = AES IV
        private static readonly byte[] IV =Encoding.UTF8.GetBytes("1234567890123456");
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentException("Plain text cannot be empty.");

            using Aes aes = Aes.Create();

            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new MemoryStream();

            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
                throw new ArgumentException("Encrypted text cannot be empty.");

            byte[] buffer = Convert.FromBase64String(encryptedText);

            using Aes aes = Aes.Create();

            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new MemoryStream(buffer);
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}