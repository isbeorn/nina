#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Drawing;
using OpenCvSharp;
using System.Linq;

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Hough Line Transformation filter
    /// </summary>
    public class HoughLineTransformation {
        private HoughLine[] detectedLines;
        private int minLineIntensity = 10;
        private int maxIntensity = 0;

        public HoughLineTransformation() {
        }

        /// <summary>
        /// Minimum line intensity to accept
        /// </summary>
        public int MinLineIntensity {
            get => minLineIntensity;
            set => minLineIntensity = value;
        }

        /// <summary>
        /// Process an image to detect lines using Hough Transform
        /// </summary>
        public void ProcessImage(Bitmap image) {
            Mat mat = image;
            
            // Ensure image is grayscale
            Mat gray = new Mat();
            if (mat.Channels() == 3) {
                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
            } else {
                gray = mat.Clone();
            }

            // Apply Hough Line Transform
            // Parameters: image, rho, theta, threshold
            LineSegmentPoint[] lines = Cv2.HoughLinesP(
                gray, 
                1,                          // rho: distance resolution in pixels
                System.Math.PI / 180,       // theta: angle resolution in radians
                minLineIntensity,           // threshold: minimum votes
                50,                         // minLineLength
                10                          // maxLineGap
            );

            // Convert OpenCV lines to HoughLine format
            var houghLines = new System.Collections.Generic.List<HoughLine>();
            maxIntensity = 0;

            foreach (var line in lines) {
                // Convert line segment to polar coordinates (radius, theta)
                int dx = line.P2.X - line.P1.X;
                int dy = line.P2.Y - line.P1.Y;
                
                // Calculate angle (theta) in degrees
                double theta = System.Math.Atan2(dy, dx) * 180.0 / System.Math.PI;
                if (theta < 0) theta += 180;

                // Calculate perpendicular distance from origin (radius)
                // Using formula: r = x*cos(theta) + y*sin(theta)
                double thetaRad = theta * System.Math.PI / 180.0;
                int x = (line.P1.X + line.P2.X) / 2;
                int y = (line.P1.Y + line.P2.Y) / 2;
                int radius = (int)(x * System.Math.Cos(thetaRad) + y * System.Math.Sin(thetaRad));

                // Intensity is approximated by line length
                short intensity = (short)System.Math.Sqrt(dx * dx + dy * dy);
                if (intensity > maxIntensity) {
                    maxIntensity = intensity;
                }

                houghLines.Add(new HoughLine(radius, theta, intensity, 0));
            }

            // Calculate relative intensities
            if (maxIntensity > 0) {
                for (int i = 0; i < houghLines.Count; i++) {
                    var line = houghLines[i];
                    line.RelativeIntensity = (double)line.Intensity / maxIntensity;
                }
            }

            // Sort by intensity (descending)
            detectedLines = houghLines.OrderByDescending(l => l.Intensity).ToArray();

            gray.Dispose();
        }

        /// <summary>
        /// Get the most intensive lines detected
        /// </summary>
        public HoughLine[] GetMostIntensiveLines(int count) {
            if (detectedLines == null || detectedLines.Length == 0) {
                return new HoughLine[0];
            }

            // Return up to 'count' lines (already sorted by intensity)
            int returnCount = System.Math.Min(count, detectedLines.Length);
            HoughLine[] result = new HoughLine[returnCount];
            System.Array.Copy(detectedLines, result, returnCount);
            return result;
        }
    }
}
