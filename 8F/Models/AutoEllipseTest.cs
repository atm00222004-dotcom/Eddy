using System;

namespace _8F.Models
{
    /// <summary>
    /// Model representing a raw multi-frequency Auto Ellipse test run.
    /// </summary>
    public class AutoEllipseTest
    {
        public long Id { get; set; }
        public int ChannelId { get; set; }
        public int TestNumber { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string OperatorName { get; set; } = string.Empty;
        public string FrequencyValuesJson { get; set; } = "{}";
        public bool IsDeleted { get; set; } = false;
    }
}
