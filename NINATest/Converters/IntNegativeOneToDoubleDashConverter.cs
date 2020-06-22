using FluentAssertions;
using NINA.Utility.Converters;
using NUnit.Framework;
using System.Globalization;

namespace NINATest.Converters {

    [TestFixture]
    internal class IntNegativeOneToDoubleDashConverterTest {
        private IntNegativeOneToDoubleDashConverter sut;

        [SetUp]
        public void Init() {
            sut = new IntNegativeOneToDoubleDashConverter();
        }

        [Test]
        public void TestConvert() {
            sut.Convert(-1, null, null, CultureInfo.CurrentCulture).Should().Be("--");
            sut.Convert(-2, null, null, CultureInfo.CurrentCulture).Should().Be(-2);
            sut.Convert("anything", null, null, CultureInfo.CurrentCulture).Should().Be("anything");
            sut.Convert(null, null, null, CultureInfo.CurrentCulture).Should().BeNull();
        }

        [Test]
        public void TestConvertBack() {
            sut.ConvertBack("--", null, null, CultureInfo.CurrentCulture).Should().Be(-1);
            sut.ConvertBack(-2, null, null, CultureInfo.CurrentCulture).Should().Be(-2);
            sut.ConvertBack("anything", null, null, CultureInfo.CurrentCulture).Should().Be("anything");
            sut.ConvertBack(null, null, null, CultureInfo.CurrentCulture).Should().BeNull();
        }
    }
}