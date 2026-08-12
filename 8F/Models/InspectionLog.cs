using System;

namespace _8F.Models
{
    /// <summary>
    /// Model representing an inspection record for industrial Eddy Current NDT testing.
    /// </summary>
    public class InspectionLog
    {
        public long Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public DateTime InspectionTimestamp { get; set; } = DateTime.UtcNow;
        public int ChannelNumber { get; set; }
        public double FrequencyHz { get; set; }
        public double XValue { get; set; }
        public double YValue { get; set; }
        public bool ResultPass { get; set; }
        public string? DefectType { get; set; }
        public string MachineId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
