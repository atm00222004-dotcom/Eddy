using System;
using System.Collections.Generic;
using Xunit;
using _8F.Services;

namespace _8F.Tests
{
    public class EllipseFitterTests
    {
        [Fact]
        public void FitEllipse_WithReferenceDataset_ComputesCorrectParameters()
        {
            // Arrange: reference dataset from specification
            var points = new List<(double X, double Y)>
            {
                (1, 1),
                (2, 2),
                (3, 2),
                (4, 4),
                (5, 8),
                (6, 6),
                (7, 7),
                (8, 8),
                (9, 9),
                (10, 6)
            };

            // Act
            AutoEllipseResult result = EllipseFitter.FitEllipse("D1", 1, points);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(10, result.SampleCount);
            Assert.Equal("D1", result.FrequencyName);
            Assert.Equal(1, result.FrequencyId);

            // Center X should be distance-weighted center (~4.50 to 6.00)
            Assert.InRange(result.CenterX, 4.50, 6.00);

            // Center Y should be distance-weighted center (~4.50 to 6.00)
            Assert.InRange(result.CenterY, 4.50, 6.00);

            // Rotation angle should be ~38° (slope ~0.78 - 0.79)
            Assert.InRange(result.RotationAngle, 35.0, 42.0);

            // REGRESSION TEST (Step 4): Width and Height must NOT be unconditionally floored to MIN_DIMENSION (100.0)
            Assert.NotEqual(EllipseFitter.MIN_DIMENSION, result.Width);
            Assert.NotEqual(EllipseFitter.MIN_DIMENSION, result.Height);

            // Expected raw dimensions for reference dataset (Version 3: Width = 21.28, Height = 12.66 due to tightening loop ensuring point (5,8) containment)
            Assert.InRange(result.Width, 8.0, 40.0);
            Assert.InRange(result.Height, 2.0, 15.0);
        }

        [Fact]
        public void FitEllipse_WithEmptyPoints_ReturnsInvalid()
        {
            var points = new List<(double X, double Y)>();
            var result = EllipseFitter.FitEllipse("F1", 1, points);

            Assert.False(result.IsValid);
            Assert.Equal(0, result.SampleCount);
        }

        [Fact]
        public void FitEllipse_WithSinglePoint_ReturnsFallbackFloor()
        {
            var points = new List<(double X, double Y)> { (5.0, 10.0) };
            var result = EllipseFitter.FitEllipse("F1", 1, points);

            Assert.True(result.IsValid);
            Assert.Equal(1, result.SampleCount);
            Assert.Equal(5.0, result.CenterX);
            Assert.Equal(10.0, result.CenterY);
            // Degenerate single point should still trigger MIN_DIMENSION floor
            Assert.Equal(EllipseFitter.MIN_DIMENSION, result.Width);
            Assert.Equal(EllipseFitter.MIN_DIMENSION, result.Height);
        }

        [Fact]
        public void IsInsideEllipse_EvaluatesPointsCorrectly()
        {
            double Xc = 0.0, Yc = 0.0;
            double a = 10.0, b = 5.0;
            double thetaRad = 0.0;

            // Point at center is inside
            Assert.True(EllipseFitter.IsInsideEllipse(0.0, 0.0, a, b, Xc, Yc, thetaRad));

            // Point clearly inside (1, 1)
            Assert.True(EllipseFitter.IsInsideEllipse(1.0, 1.0, a, b, Xc, Yc, thetaRad));

            // Point clearly outside (20, 20)
            Assert.False(EllipseFitter.IsInsideEllipse(20.0, 20.0, a, b, Xc, Yc, thetaRad));

            // Boundary point (10, 0): dist = (10/10)^2 + (0/5)^2 = 1.0 -> boundary is outside (dist < 1 is false)
            Assert.False(EllipseFitter.IsInsideEllipse(10.0, 0.0, a, b, Xc, Yc, thetaRad));
        }

        [Fact]
        public void FitEllipse_Version3_IterativeTighteningLoopTerminates()
        {
            var syntheticPoints = new List<(double X, double Y)>();
            for (int i = 0; i < 50; i++)
            {
                syntheticPoints.Add((i * 0.5, Math.Sin(i) * 3.0 + i * 0.2));
            }

            var result = EllipseFitter.FitEllipse("D1", 1, syntheticPoints);

            Assert.True(result.IsValid);
            Assert.Equal(50, result.SampleCount);
            Assert.True(result.Width > 0);
            Assert.True(result.Height > 0);
        }

        [Fact]
        public void FitEllipse_WithIdenticalPoints_DoesNotDivideByZeroOrReturnNaN()
        {
            var points = new List<(double X, double Y)>
            {
                (5.0, 5.0),
                (5.0, 5.0),
                (5.0, 5.0)
            };

            var result = EllipseFitter.FitEllipse("F1", 1, points);

            Assert.True(result.IsValid);
            Assert.Equal(3, result.SampleCount);
            Assert.False(double.IsNaN(result.CenterX));
            Assert.False(double.IsNaN(result.CenterY));
            Assert.False(double.IsNaN(result.Width));
            Assert.False(double.IsNaN(result.Height));
            Assert.False(double.IsNaN(result.RotationAngle));
        }

        [Fact]
        public void FitEllipse_WithVerticalPointCloud_BehavesSensiblyWithoutNaN()
        {
            var points = new List<(double X, double Y)>
            {
                (5.0, 1.0),
                (5.0, 2.0),
                (5.0, 3.0),
                (5.0, 4.0),
                (5.0, 5.0)
            };

            var result = EllipseFitter.FitEllipse("F1", 1, points);

            Assert.True(result.IsValid);
            Assert.Equal(5, result.SampleCount);
            Assert.False(double.IsNaN(result.CenterX));
            Assert.False(double.IsNaN(result.CenterY));
            Assert.False(double.IsNaN(result.Width));
            Assert.False(double.IsNaN(result.Height));
            Assert.False(double.IsNaN(result.RotationAngle));
        }
    }
}
