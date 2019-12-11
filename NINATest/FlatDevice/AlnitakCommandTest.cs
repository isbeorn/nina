using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NUnit.Framework;
using System;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakCommandTest {

        [Test]
        [TestCase("Ping", "POOO")]
        [TestCase("Open", "OOOO")]
        [TestCase("Close", "COOO")]
        [TestCase("LightOn", "LOOO")]
        [TestCase("LightOff", "DOOO")]
        [TestCase("GetBrightness", "JOOO")]
        [TestCase("State", "SOOO")]
        [TestCase("FirmwareVersion", "VOOO")]
        public void TestCommand(string commandName, string commandString) {
            var sut = (Command)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.AlnitakSDK.{commandName}Command").Unwrap();
            Assert.That(sut.CommandString, Is.EqualTo($">{commandString}\r"));
        }

        [Test]
        public void TestSetBrightnessCommand() {
            var sut = new SetBrightnessCommand(100.0);
            Assert.That(sut.CommandString, Is.EqualTo($">B100\r"));
        }
    }
}