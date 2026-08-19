using System;
using System.Collections.Generic;
using System.Linq;

namespace _8F.Services
{
    public class AutoEllipseResult
    {
        public string FrequencyName { get; set; } = string.Empty;
        public int FrequencyId { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double RotationAngle { get; set; } // Represented as 'angel' in 8F model
        public int SampleCount { get; set; }
        public bool IsValid { get; set; } = true;
    }

    /// <summary>
    /// Service for computing threshold ellipse parameters (mean centroid and spread) per frequency.
    /// </summary>
    public static class EllipseFitter
    {
        private const double MIN_DIMENSION = 100.0; // Minimum width/height floor for 8F ECT instrument

        public static AutoEllipseResult FitEllipse(string frequencyName, int frequencyId, IEnumerable<(double X, double Y)> points)
        {
            var pointList = points?.ToList() ?? new List<(double X, double Y)>();
            if (pointList.Count == 0)
            {
                return new AutoEllipseResult
                {
                    FrequencyName = frequencyName,
                    FrequencyId = frequencyId,
                    IsValid = false,
                    SampleCount = 0
                };
            }

            // 1. CenterX and CenterY = Mean of X and Mean of Y values
            double meanX = pointList.Average(p => p.X);
            double meanY = pointList.Average(p => p.Y);

            // 2. Width and Height = Spread (Max - Min) of X and Y values
            double minX = pointList.Min(p => p.X);
            double maxX = pointList.Max(p => p.X);
            double minY = pointList.Min(p => p.Y);
            double maxY = pointList.Max(p => p.Y);

            double spreadX = maxX - minX;
            double spreadY = maxY - minY;

            // Enforce minimum dimension floor (100 units)
            double width = Math.Max(MIN_DIMENSION, spreadX);
            double height = Math.Max(MIN_DIMENSION, spreadY);

            return new AutoEllipseResult
            {
                FrequencyName = frequencyName,
                FrequencyId = frequencyId,
                CenterX = Math.Round(meanX, 2),
                CenterY = Math.Round(meanY, 2),
                Width = Math.Round(width, 2),
                Height = Math.Round(height, 2),
                RotationAngle = 0.0,
                SampleCount = pointList.Count,
                IsValid = true
            };
        }
    }
}
