using System;
using NUnit.Framework;
using AlnitakAstrosystemsSDK;
using static AlnitakAstrosystemsSDK.LIBAlnitak;

namespace NINATest {

    [TestFixture]
    public class LIBAlnitakTest {

        [Test]
        public void TestScanForDevices() {
            LIBAlnitak.ScanForDevices();
        }

        [Test]
        public void TestCoverStatus() {
            LIBAlnitak.GetCoverState("COM3");
        }

        [Test]
        public void TestGetLightOn() {
            LIBAlnitak.GetLightOn("COM3");
        }

        [Test]
        public void TestMotor() {
            LIBAlnitak.GetMotorOn("COM3");
        }

        [Test]
        public void TestFWrev() {
            LIBAlnitak.GetFWrev("COM3");
        }
    }
}