using System;
using System.Collections.Generic;

namespace ConfigurationKeyGenerator.Models
{
    public class ConfigurationGeneration
    {
        public ProductType Product { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string MachineId { get; set; } = string.Empty;

        // All keys from the uploaded text file
        public Dictionary<string, string> ConfigurationKeys { get; set; } = new();

        public DateTime GeneratedOn { get; set; } = DateTime.Now;
    }
}