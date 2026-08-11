using System;
using System.Collections.Generic;
using System.IO;

namespace ConfigurationKeyGenerator.Services
{
    public class TextFileService
    {
        public Dictionary<string, string> ReadConfiguration(string filePath)
        {
            Dictionary<string, string> data = new();

            foreach (string line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    data[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return data;
        }
    }
}