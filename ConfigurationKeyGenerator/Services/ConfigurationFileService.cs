using ConfigurationKeyGenerator.Models;
using System;
using System.IO;
using System.Text.Json;

namespace ConfigurationKeyGenerator.Services
{
    public class ConfigurationFileService
    {
        private readonly EncryptionService _encryptionService;
        private readonly JsonService _jsonService;

        public ConfigurationFileService()
        {
            _encryptionService = new EncryptionService();
            _jsonService = new JsonService();
        }

        public void GenerateLicenseFile(ConfigurationGeneration configuration, string filePath)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Invalid file path.", nameof(filePath));

            // Convert object to JSON
            string json = _jsonService.Serialize(configuration);

            // Encrypt JSON
            string encryptedData = _encryptionService.Encrypt(json);

            // Save encrypted data
            File.WriteAllText(filePath, encryptedData);
        }

        public ConfigurationGeneration ReadLicenseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("License file not found.", filePath);

            // Read encrypted file
            string encryptedData = File.ReadAllText(filePath);

            // Decrypt
            string json = _encryptionService.Decrypt(encryptedData);

            // Convert JSON to object
            ConfigurationGeneration? configuration =
                _jsonService.Deserialize<ConfigurationGeneration>(json);

            if (configuration == null)
                throw new Exception("Invalid license file.");

            return configuration;
        }

        public byte[] GenerateEncryptedBytes(ConfigurationGeneration configuration)
        {
            string json =
                JsonSerializer.Serialize(configuration);


            string encryptedText =
                _encryptionService.Encrypt(json);


            return System.Text.Encoding.UTF8
                .GetBytes(encryptedText);
        }
    }
}