using System.Runtime.InteropServices;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;

namespace NINATest.SerialCommunication {

    internal class TestResponse : Response {

        public bool CheckDeviceResponse(string response) {
            return DeviceResponse.Equals(response);
        }

        public bool GetBoolFromZeroOne(char response, out bool result) {
            return ParseBoolFromZeroOne(response, "test", out result);
        }

        public bool GetDoubleFromResponse(string response, out double result) {
            return ParseDouble(response, "test", out result);
        }

        public bool GetShortFromResponse(string response, out short result) {
            return ParseShort(response, "test", out result);
        }

        public bool GetIntegerFromResponse(string response, out int result) {
            return ParseInteger(response, "test", out result);
        }

        public bool GetLongFromResponse(string response, out long result) {
            return ParseLong(response, "test", out result);
        }
    }

    [TestFixture]
    internal class ResponseTests {
        private TestResponse _sut;

        [SetUp]
        public void Init() {
            _sut = new TestResponse();
        }

        [Test]
        [TestCase('0', true, false)]
        [TestCase('1', true, true)]
        [TestCase('X', false, false)]
        [TestCase(null, false, false)]
        public void TestBoolParsing(char response, bool valid, bool expectedResult) {
            Assert.That(_sut.GetBoolFromZeroOne(response, out var result), Is.EqualTo(valid));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("1.0", true, 1d)]
        [TestCase("", false, 0d)]
        [TestCase(null, false, 0d)]
        public void TestDoubleParsing(string response, bool valid, double expectedResult) {
            Assert.That(_sut.GetDoubleFromResponse(response, out var result), Is.EqualTo(valid));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("1", true, 1)]
        [TestCase("", false, 0)]
        [TestCase(null, false, 0)]
        public void TestShortParsing(string response, bool valid, short expectedResult) {
            Assert.That(_sut.GetShortFromResponse(response, out var result), Is.EqualTo(valid));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("1", true, 1)]
        [TestCase("", false, 0)]
        [TestCase(null, false, 0)]
        public void TestIntegerParsing(string response, bool valid, int expectedResult) {
            Assert.That(_sut.GetIntegerFromResponse(response, out var result), Is.EqualTo(valid));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("1", true, 1)]
        [TestCase("", false, 0)]
        [TestCase(null, false, 0)]
        public void TestLongParsing(string response, bool valid, long expectedResult) {
            Assert.That(_sut.GetLongFromResponse(response, out var result), Is.EqualTo(valid));
            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }

    [TestFixture]
    [SetUICulture("da-DK")]
    [SetCulture("da-DK")]
    internal class DaDKResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("de-DE")]
    [SetCulture("de-DE")]
    internal class DeDeResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("en-GB")]
    [SetCulture("en-GB")]
    internal class EnGbResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("en-US")]
    [SetCulture("en-US")]
    internal class EnUsResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("es-ES")]
    [SetCulture("es-ES")]
    internal class EsEsResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("fr-FR")]
    [SetCulture("fr-FR")]
    internal class FrFrResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("it-IT")]
    [SetCulture("it-IT")]
    internal class ItItResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("ja-JP")]
    [SetCulture("ja-JP")]
    internal class JaJpResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("nl-NL")]
    [SetCulture("nl-NL")]
    internal class NlNlResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("pl-PL")]
    [SetCulture("pl-PL")]
    internal class PlPlResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("ru-RU")]
    [SetCulture("ru-RU")]
    internal class RuRuResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("zh-CN")]
    [SetCulture("zh-CN")]
    internal class ZhCnResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("zh-HK")]
    [SetCulture("zh-HK")]
    internal class ZhHkResponseTests : ResponseTests {
    }

    [TestFixture]
    [SetUICulture("zh-TW")]
    [SetCulture("zh-TW")]
    internal class ZhTwResponseTests : ResponseTests {
    }
}