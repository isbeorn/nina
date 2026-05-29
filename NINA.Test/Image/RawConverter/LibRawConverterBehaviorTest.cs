using FluentAssertions;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NUnit.Framework;
using System;
using System.Reflection;

namespace NINA.Test.Image.RawConverter {

    [TestFixture]
    public class LibRawConverterBehaviorTest {

        /// <summary>
        /// Verifies LibRaw visible-area color codes are translated into the Bayer pattern used by N.I.N.A.'s debayer pipeline.
        /// </summary>
        [TestCase("RGGB", SensorType.RGGB)]
        [TestCase("GBRG", SensorType.GBRG)]
        [TestCase("GRBG", SensorType.GRBG)]
        [TestCase("BGGR", SensorType.BGGR)]
        [TestCase("RGBG", SensorType.RGBG)]
        [TestCase("GRGB", SensorType.GRGB)]
        [TestCase("GBGR", SensorType.GBGR)]
        [TestCase("BGRG", SensorType.BGRG)]
        public void TryGetBayerPattern_MapsVisibleLibRawPatternToSensorType(string visiblePattern, SensorType expectedSensorType) {
            (bool mapped, SensorType sensorType) = TryGetBayerPattern(visiblePattern);

            mapped.Should().BeTrue();
            sensorType.Should().Be(expectedSensorType);
        }

        /// <summary>
        /// Verifies unsupported CFA color descriptions are not misreported as Bayer patterns.
        /// </summary>
        [Test]
        public void TryGetBayerPattern_RejectsUnsupportedPattern() {
            (bool mapped, SensorType sensorType) = TryGetBayerPattern("GCMY");

            mapped.Should().BeFalse();
            sensorType.Should().Be(SensorType.Monochrome);
        }

        private static (bool Mapped, SensorType SensorType) TryGetBayerPattern(string visiblePattern) {
            Type converterType = typeof(ImageDataFactory).Assembly.GetType(
                "NINA.Image.RawConverter.LibRawConverter",
                throwOnError: true)!;
            MethodInfo method = converterType.GetMethod(
                "TryGetBayerPattern",
                BindingFlags.Static | BindingFlags.NonPublic)!;

            object[] parameters = { visiblePattern, SensorType.Monochrome };
            var mapped = (bool)method.Invoke(null, parameters)!;
            return (mapped, (SensorType)parameters[1]);
        }
    }
}
