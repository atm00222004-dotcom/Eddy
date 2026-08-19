using System;

namespace _8F.Models
{
    /// <summary>
    /// Model representing a computed Auto Ellipse result record per frequency for audit trail.
    /// </summary>
    public class AutoEllipseResultRecord
    {
        public long Id { get; set; }
        public int ChannelId { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string SelectedTestIdsJson { get; set; } = "[]";
        public double ComputedCenterX { get; set; }
        public double ComputedCenterY { get; set; }
        public double ComputedWidth { get; set; }
        public double ComputedHeight { get; set; }
        public double ComputedRotationAngle { get; set; }
        public int SampleCount { get; set; }
    }
}
