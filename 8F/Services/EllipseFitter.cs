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
        public double RotationAngle { get; set; }
        public int SampleCount { get; set; }
        public bool IsValid { get; set; } = true;
    }

    /// <summary>
    /// Service for computing threshold ellipse parameters using a literal C# port of the C++ get_parameters() function.
    /// </summary>
    public static class EllipseFitter
    {
        public const double MIN_DIMENSION = 100.0; // Minimum width/height floor for 8F ECT instrument

        /// <summary>
        /// Computes rotated-ellipse threshold parameters from 2D point cloud data using the literal C++ get_parameters() formulas:
        /// 1. theta = atan2(slope, 1) — rotation angle from fitted line slope.
        /// 2. Orthogonal Projection: Projects all sample points onto fitted line (y = slope*x + y_intercept).
        /// 3. Extreme endpoints: Finds index_end[0] (farthest from index 0) and index_end[1] (farthest from index_end[0]).
        /// 4. end_dist: Perpendicular distance from extreme projected points back to original sample points.
        /// 5. Weighted Center: Xc = (end_dist[0]*x_end[0] + end_dist[1]*x_end[1]) / (end_dist[0] + end_dist[1]), Yc = slope*Xc + y_intercept.
        /// 6. Major axis semi-diameter 'a': Distance from (Xc, Yc) to end point with smaller end_dist.
        /// 7. Furthest point (x_fur, y_fur): Point with maximum perpendicular deviation from projected line.
        /// 8. Minor axis endpoints: Solves two-line intersection formulas for (x_b, y_b) using slope_b from major axis endpoints to (x_fur, y_fur).
        /// 9. Minor axis semi-diameter 'b': Larger distance from (Xc, Yc) to (x_b[0], y_b[0]) and (x_b[1], y_b[1]).
        /// </summary>
        public static AutoEllipseResult FitEllipse(string frequencyName, int frequencyId, IEnumerable<(double X, double Y)> points, double a_stretch = 1.0, double b_stretch = 1.0)
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

            if (pointList.Count < 2)
            {
                return new AutoEllipseResult
                {
                    FrequencyName = frequencyName,
                    FrequencyId = frequencyId,
                    CenterX = Math.Round(pointList[0].X, 2),
                    CenterY = Math.Round(pointList[0].Y, 2),
                    Width = MIN_DIMENSION,
                    Height = MIN_DIMENSION,
                    RotationAngle = 0.0,
                    SampleCount = pointList.Count,
                    IsValid = true
                };
            }

            int m = pointList.Count;
            List<double> x = pointList.Select(p => p.X).ToList();
            List<double> y = pointList.Select(p => p.Y).ToList();

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            for (int i = 0; i < m; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumX2 += x[i] * x[i];
            }
            double meanX = sumX / m;
            double meanY = sumY / m;

            double denom = sumX2 - sumX * meanX;
            double slope = 0.0;
            double y_intercept = meanY;

            if (Math.Abs(denom) > 1e-9)
            {
                slope = (sumXY - sumX * meanY) / denom;
                y_intercept = meanY - slope * meanX;
            }

            double thetaRad = Math.Atan2(slope, 1.0);
            double thetaDeg = thetaRad * (180.0 / Math.PI);

            double[] x_proj = new double[m];
            double[] y_proj = new double[m];
            double slopeSqPlus1 = (slope * slope) + 1.0;

            for (int i = 0; i < m; i++)
            {
                x_proj[i] = (slope * y[i] + x[i] - slope * y_intercept) / slopeSqPlus1;
                y_proj[i] = slope * x_proj[i] + y_intercept;
            }

            int index_end0 = 0;
            double max_dist = 0.0;
            for (int i = 1; i < m; i++)
            {
                double dist = Math.Sqrt(sq(x_proj[i] - x_proj[0]) + sq(y_proj[i] - y_proj[0]));
                if (dist > max_dist)
                {
                    index_end0 = i;
                    max_dist = dist;
                }
            }

            int index_end1 = 0;
            max_dist = 0.0;
            for (int i = 0; i < m; i++)
            {
                double dist = Math.Sqrt(sq(x_proj[i] - x_proj[index_end0]) + sq(y_proj[i] - y_proj[index_end0]));
                if (dist > max_dist)
                {
                    index_end1 = i;
                    max_dist = dist;
                }
            }

            double[] x_end = new double[2];
            double[] y_end = new double[2];

            x_end[0] = x_proj[index_end0];
            y_end[0] = y_proj[index_end0];
            x_end[1] = x_proj[index_end1];
            y_end[1] = y_proj[index_end1];

            double[] end_dist = new double[2];
            end_dist[0] = Math.Sqrt(sq(x_end[0] - x[index_end0]) + sq(y_end[0] - y[index_end0]));
            end_dist[1] = Math.Sqrt(sq(x_end[1] - x[index_end1]) + sq(y_end[1] - y[index_end1]));

            double Xc = 0.0;
            double Yc = 0.0;    
            double end_dist_sum = end_dist[0] + end_dist[1];
            if (end_dist_sum > 1e-9)
            {
                Xc = ((end_dist[0] * x_end[0]) + (end_dist[1] * x_end[1])) / end_dist_sum;
            }
            else
            {
                Xc = (x_end[0] + x_end[1]) / 2.0;
            }
            Yc = slope * Xc + y_intercept;

            double a = 0.0;
            if (end_dist[0] < end_dist[1])
            {
                a = Math.Sqrt(sq(Xc - x_end[0]) + sq(Yc - y_end[0]));
            }
            else
            {
                a = Math.Sqrt(sq(Xc - x_end[1]) + sq(Yc - y_end[1]));
            }
            double hypo = 0.0;

            for (int i = 0; i < m; i++)
            {
                double distBase = Math.Sqrt(sq(x_proj[i] - x_end[1]) + sq(y_proj[i] - y_end[1]));
                double distHeight = Math.Sqrt(sq(x_proj[i] - x[i]) + sq(y_proj[i] - y[i]));
                double tempHypo = distHeight - distBase; 
                if (tempHypo > hypo) hypo = tempHypo;
            }

            for (int i = 0; i < m; i++)
            {
                double distBase = Math.Sqrt(sq(x_proj[i] - x_end[0]) + sq(y_proj[i] - y_end[0]));
                double distHeight = Math.Sqrt(sq(x_proj[i] - x[i]) + sq(y_proj[i] - y[i]));
                double tempHypo = distHeight - distBase;
                if (tempHypo > hypo) hypo = tempHypo;
            }

            a = a + hypo;

            if (a < 5e-1)
            {
                a = a + 5;
            }

            a = a * a_stretch;

            double x_fur = x[0];
            double y_fur = y[0];
            double max_dev = 0.0;
            for (int i = 0; i < m; i++)
            {
                double dist = Math.Sqrt(sq(x[i] - x_proj[i]) + sq(y[i] - y_proj[i]));
                if (dist > max_dev)
                {
                    x_fur = x[i];
                    y_fur = y[i];
                    max_dev = dist;
                }
            }

            double sqrtSlopeSqPlus1 = Math.Sqrt(slopeSqPlus1);
            x_end[0] = Xc + (a / sqrtSlopeSqPlus1);
            y_end[0] = slope * x_end[0] + y_intercept;

            x_end[1] = Xc - (a / sqrtSlopeSqPlus1);
            y_end[1] = slope * x_end[1] + y_intercept;

            double[] slope_b = new double[2];
            double denom_b0 = (x_end[0] - x_fur);
            double denom_b1 = (x_end[1] - x_fur);

            slope_b[0] = Math.Abs(denom_b0) > 1e-9 ? (y_end[0] - y_fur) / denom_b0 : 0.0;
            slope_b[1] = Math.Abs(denom_b1) > 1e-9 ? (y_end[1] - y_fur) / denom_b1 : 0.0;

            double[] x_b = new double[2];
            double[] y_b = new double[2];

            if (Math.Abs(slope) > 1e-9)
            {
                x_b[0] = (slope * Yc + Xc - slope * y_fur + slope * slope_b[0] * x_fur) / (slope * slope_b[0] + 1.0);
                y_b[0] = (slope * Yc - x_b[0] + Xc) / slope;

                x_b[1] = (slope * Yc + Xc - slope * y_fur + slope * slope_b[1] * x_fur) / (slope * slope_b[1] + 1.0);
                y_b[1] = (slope * Yc - x_b[1] + Xc) / slope;
            }
            else
            {
                x_b[0] = Xc; y_b[0] = y_fur;
                x_b[1] = Xc; y_b[1] = y_fur;
            }

            double b1 = Math.Sqrt(sq(Xc - x_b[0]) + sq(Yc - y_b[0]));
            double b2 = Math.Sqrt(sq(Xc - x_b[1]) + sq(Yc - y_b[1]));

            double b = (b1 > b2) ? b1 : b2;

            if (b <= 1e-9 || double.IsNaN(b))
            {
                b = max_dev;
            }

            double gsf = 0.5;

            while (gsf >= Math.Pow(2, -6))
            {
                bool allInside = true;

                foreach (var (px, py) in pointList)
                {
                    if (!IsInsideEllipse(px, py, a, b, Xc, Yc, thetaRad))
                    {
                        b = b / (1 - gsf);
                        gsf *= 0.5;
                        allInside = false;
                        break;
                    }
                }

                if (allInside)
                {
                    b = b * (1 - gsf);
                }
            }

            if (b < 5e-1)
            {
                b = b + 5;
            }

            b = b * b_stretch;

            double rawWidth = Math.Round(a * 2.0, 2);
            double rawHeight = Math.Round(b * 2.0, 2);

            double width = (double.IsNaN(rawWidth) || rawWidth <= 1e-6) ? MIN_DIMENSION : rawWidth;
            double height = (double.IsNaN(rawHeight) || rawHeight <= 1e-6) ? MIN_DIMENSION : rawHeight;

            if (double.IsNaN(Xc)) Xc = meanX;
            if (double.IsNaN(Yc)) Yc = meanY;
            if (double.IsNaN(thetaDeg)) thetaDeg = 0.0;

            return new AutoEllipseResult
            {
                FrequencyName = frequencyName,
                FrequencyId = frequencyId,
                CenterX = Math.Round(Xc, 2),
                CenterY = Math.Round(Yc, 2),
                Width = width,
                Height = height,
                RotationAngle = Math.Round(thetaDeg, 2),
                SampleCount = m,
                IsValid = true
            };
        }

        /// <summary>
        /// Checks whether 2D point (x, y) is inside rotated ellipse centered at (Xc, Yc) with semi-axes (a, b) and rotation thetaRad (radians).
        /// Corrects the bug in reference C++ auto_ellipse_formation_version3.cpp where tempX/tempY were unassigned zero variables during rotation.
        /// </summary>
        public static bool IsInsideEllipse(double x, double y, double a, double b, double Xc, double Yc, double thetaRad)
        {
            if (a <= 1e-9 || b <= 1e-9) return false;

            double shiftedX = x - Xc;
            double shiftedY = y - Yc;

            double rotatedX = (shiftedX * Math.Cos(thetaRad)) + (shiftedY * Math.Sin(thetaRad));
            double rotatedY = (-shiftedX * Math.Sin(thetaRad)) + (shiftedY * Math.Cos(thetaRad));

            double dist = sq(rotatedX / a) + sq(rotatedY / b);

            return dist < 1.0;
        }

        private static double sq(double val) => val * val;
    }
}
