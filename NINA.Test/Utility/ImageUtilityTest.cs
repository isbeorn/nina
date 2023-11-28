using FluentAssertions;
using NINA.Image.ImageAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Utility {
    [TestFixture]
    public class ImageUtilityTest {
        [Test]
        [TestCase(0, 0)]
        [TestCase(8192, 0.125)]
        [TestCase(16384, 0.25)]
        [TestCase(32767, 0.5)]
        [TestCase(49151, 0.75)]
        [TestCase(57343, 0.875)]
        [TestCase(ushort.MaxValue, 1)]
        public void TestNormalization(int input, double expected) {
            var normalization = ImageUtility.NormalizeUShort(input, 16);

            normalization.Should().BeApproximately(expected, 0.00001);
        }
        [Test]
        [TestCase(0, 0)]
        [TestCase(0.125, 8192)]
        [TestCase(0.25, 16384)]
        [TestCase(0.5, 32767)]
        [TestCase(0.75, 49151)]
        [TestCase(0.875, 57343)]
        [TestCase(1, ushort.MaxValue)]
        public void TestDeNormalization(double input, int expected) {
            int normalization = ImageUtility.DenormalizeUShort(input);

            normalization.Should().Be(expected);
        }
    }
}
