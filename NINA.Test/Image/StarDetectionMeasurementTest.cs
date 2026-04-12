using FluentAssertions;
using NINA.Image.ImageAnalysis;
using NUnit.Framework;
using System.Collections.Generic;
using System.Drawing;

namespace NINA.Test.Image {
    [TestFixture]
    public class StarDetectionMeasurementTest {
        [Test]
        public void Calculate_CircularGaussianStar_ComputesDefensibleHfrAndFwhm() {
            const double sigma = 2.0;
            const double background = 100.0;
            var actualCenterX = 15.35;
            var actualCenterY = 14.7;

            var star = new StarDetection.Star {
                Position = new Accord.Point(15f, 15f),
                Radius = 6.0,
                Rectangle = new Rectangle(9, 9, 12, 12),
                SurroundingMean = background,
                MaxPixelValue = background + 1000
            };

            var pixels = BuildGaussianPixelData(31, 31, actualCenterX, actualCenterY, sigma, amplitude: 1000, background: background);

            star.Calculate(pixels);

            var expectedHfr = sigma * System.Math.Sqrt(2.0 * System.Math.Log(2.0));
            var expectedFwhm = 2.0 * expectedHfr;

            star.Position.X.Should().BeApproximately((float)actualCenterX, 0.1f);
            star.Position.Y.Should().BeApproximately((float)actualCenterY, 0.1f);
            star.HFR.Should().BeApproximately(expectedHfr, 0.2);
            star.FWHM.Should().BeApproximately(expectedFwhm, 0.35);
        }

        [Test]
        public void ToDetectedStar_ExposesComputedFwhm() {
            var star = new StarDetection.Star {
                Position = new Accord.Point(10f, 10f),
                Radius = 5.0,
                Rectangle = new Rectangle(5, 5, 10, 10),
                SurroundingMean = 50,
                MaxPixelValue = 350
            };

            var pixels = BuildGaussianPixelData(21, 21, 10.2, 10.4, sigma: 1.8, amplitude: 300, background: 50);
            star.Calculate(pixels);

            var detected = star.ToDetectedStar();

            detected.HFR.Should().Be(star.HFR);
            detected.FWHM.Should().Be(star.FWHM);
            detected.Eccentricity.Should().Be(star.Eccentricity);
        }

        [Test]
        public void Calculate_EllipticalGaussianStar_ComputesSecondMomentEccentricity() {
            const double sigmaX = 3.0;
            const double sigmaY = 2.0;
            const double background = 80.0;
            var actualCenterX = 20.1;
            var actualCenterY = 18.9;

            var star = new StarDetection.Star {
                Position = new Accord.Point(20f, 19f),
                Radius = 8.0,
                Rectangle = new Rectangle(12, 11, 16, 16),
                SurroundingMean = background,
                MaxPixelValue = background + 900
            };

            var pixels = BuildEllipticalGaussianPixelData(41, 41, actualCenterX, actualCenterY, sigmaX, sigmaY, amplitude: 900, background: background);

            star.Calculate(pixels);

            var expectedEccentricity = System.Math.Sqrt(1.0 - (sigmaY * sigmaY) / (sigmaX * sigmaX));

            star.Position.X.Should().BeApproximately((float)actualCenterX, 0.1f);
            star.Position.Y.Should().BeApproximately((float)actualCenterY, 0.1f);
            star.Eccentricity.Should().BeApproximately(expectedEccentricity, 0.05);
        }

        private static List<StarDetection.PixelData> BuildGaussianPixelData(int width, int height, double centerX, double centerY, double sigma, double amplitude, double background) {
            var pixels = new List<StarDetection.PixelData>(width * height);
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    var radiusSquared = System.Math.Pow(x - centerX, 2) + System.Math.Pow(y - centerY, 2);
                    var value = background + amplitude * System.Math.Exp(-radiusSquared / (2.0 * sigma * sigma));
                    pixels.Add(new StarDetection.PixelData(x, y, value));
                }
            }

            return pixels;
        }

        private static List<StarDetection.PixelData> BuildEllipticalGaussianPixelData(int width, int height, double centerX, double centerY, double sigmaX, double sigmaY, double amplitude, double background) {
            var pixels = new List<StarDetection.PixelData>(width * height);
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    var exponent = (dx * dx) / (2.0 * sigmaX * sigmaX) + (dy * dy) / (2.0 * sigmaY * sigmaY);
                    var value = background + amplitude * System.Math.Exp(-exponent);
                    pixels.Add(new StarDetection.PixelData(x, y, value));
                }
            }

            return pixels;
        }
    }
}
