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

            // Expected raw dimensions for reference dataset (Version 2 major-axis extension expands Width to ~33.90)
            Assert.InRange(result.Width, 8.0, 40.0);
            Assert.InRange(result.Height, 2.0, 8.0);
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
    }
}
