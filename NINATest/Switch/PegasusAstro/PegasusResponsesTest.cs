#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class PegasusResponsesTest {

        [Test]
        [TestCase("1.3", true, 1.3)]
        [TestCase("ERR", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestFirmwareVersionResponse(string deviceResponse, bool valid, double version = 0) {
            var sut = new FirmwareVersionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.FirmwareVersion, Is.EqualTo(version));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0",
            true, "UPB", 12.2, 0d, 0, 23.2, 59, 14.7)]
        [TestCase("UPB:14.2:3.5:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0",
            true, "UPB", 14.2, 3.5, 50, -23.2, -2, -0.3)]
        [TestCase("UPB:XXX:3.5:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:XXX:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:3.5:50.0:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:3.5:50:XXX:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:3.5:50:23.2:XXX:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:3.5:50:23.2:59:XXX:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStatusResponseDirectProperties(string deviceResponse, bool valid, string deviceName = null, double inputVoltage = 0d,
            double inputCurrent = 0d, int power = 0, double temperature = 0d, double humidity = 0d, double dewPoint = 0d) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DeviceName, Is.EqualTo(deviceName));
            Assert.That(sut.DeviceInputVoltage, Is.EqualTo(inputVoltage));
            Assert.That(sut.DeviceCurrentAmpere, Is.EqualTo(inputCurrent));
            Assert.That(sut.DevicePower, Is.EqualTo(power));
            Assert.That(sut.Temperature, Is.EqualTo(temperature));
            Assert.That(sut.Humidity, Is.EqualTo(humidity));
            Assert.That(sut.DewPoint, Is.EqualTo(dewPoint));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:0111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, false, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1011:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, false, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1101:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1110:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:XXX:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        public void TestStatusResponsePowerPortOn(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false,
            bool port3 = false) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.PowerPortOn[0], Is.EqualTo(port0));
            Assert.That(sut.PowerPortOn[1], Is.EqualTo(port1));
            Assert.That(sut.PowerPortOn[2], Is.EqualTo(port2));
            Assert.That(sut.PowerPortOn[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:011111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, false, true, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:101111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, false, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:110111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, false, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111011:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, false, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111101:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111110:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:XXX:0:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        public void TestStatusResponseUsbPortOn(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false,
            bool port3 = false, bool port4 = false, bool port5 = false) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.UsbPortOn[0], Is.EqualTo(port0));
            Assert.That(sut.UsbPortOn[1], Is.EqualTo(port1));
            Assert.That(sut.UsbPortOn[2], Is.EqualTo(port2));
            Assert.That(sut.UsbPortOn[3], Is.EqualTo(port3));
            Assert.That(sut.UsbPortOn[4], Is.EqualTo(port4));
            Assert.That(sut.UsbPortOn[5], Is.EqualTo(port5));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:128:255:0:0:0:0:0:0:0:0000000:0", true, 0, 128, 255)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:255:0:128:0:0:0:0:0:0:0:0000000:0", true, 255, 0, 128)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:X:0:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:X:0:0:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:X:0:0:0:0:0:0:0:0000000:0", false)]
        public void TestStatusResponseDewHeaterCycle(string deviceResponse, bool valid, short cycle0 = 0, short cycle1 = 0, short cycle2 = 0) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DewHeaterDutyCycle[0], Is.EqualTo(cycle0));
            Assert.That(sut.DewHeaterDutyCycle[1], Is.EqualTo(cycle1));
            Assert.That(sut.DewHeaterDutyCycle[2], Is.EqualTo(cycle2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:300:0:0:0:0:0:0:0000000:0", true, 1, 0, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:600:0:0:0:0:0:0000000:0", true, 0, 2, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:900:0:0:0:0:0000000:0", true, 0, 0, 3, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:1200:0:0:0:0000000:0", true, 0, 0, 0, 4)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:X:0:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:X:0:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:X:0:0:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:X:0:0:0:0000000:0", false)]
        public void TestStatusResponsePortPowerFlow(string deviceResponse, bool valid, double port0 = 0d, double port1 = 0d, double port2 = 0d, double port3 = 0d) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.PortPowerFlow[0], Is.EqualTo(port0));
            Assert.That(sut.PortPowerFlow[1], Is.EqualTo(port1));
            Assert.That(sut.PortPowerFlow[2], Is.EqualTo(port2));
            Assert.That(sut.PortPowerFlow[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:300:0:0:0000000:0", true, 1, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:600:0:0000000:0", true, 0, 2, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:1200:0000000:0", true, 0, 0, 2)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:X:0:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:X:0:0000000:0", false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:X:0000000:0", false)]
        public void TestStatusResponseDewHeaterPowerFlow(string deviceResponse, bool valid, double port0 = 0d, double port1 = 0d, double port2 = 0d) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DewHeaterPowerFlow[0], Is.EqualTo(port0));
            Assert.That(sut.DewHeaterPowerFlow[1], Is.EqualTo(port1));
            Assert.That(sut.DewHeaterPowerFlow[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:1000000:0", true, true, false, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0100000:0", true, false, true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0010000:0", true, false, false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0001000:0", true, false, false, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:XXX:0", false)]
        public void TestStatusResponsePortOverCurrent(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false, bool port3 = false) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.PortOverCurrent[0], Is.EqualTo(port0));
            Assert.That(sut.PortOverCurrent[1], Is.EqualTo(port1));
            Assert.That(sut.PortOverCurrent[2], Is.EqualTo(port2));
            Assert.That(sut.PortOverCurrent[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000100:0", true, true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000010:0", true, false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000001:0", true, false, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:XXX:0", false)]
        public void TestStatusResponseDewHeaterOverCurrent(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DewHeaterOverCurrent[0], Is.EqualTo(port0));
            Assert.That(sut.DewHeaterOverCurrent[1], Is.EqualTo(port1));
            Assert.That(sut.DewHeaterOverCurrent[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, false, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:1", true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:2", true, true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:3", true, false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:4", true, false, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:5", true, true, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:6", true, true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:7", true, false, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:X", false)]
        public void TestStatusResponseAutoDewStatus(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.AutoDewStatus[0], Is.EqualTo(port0));
            Assert.That(sut.AutoDewStatus[1], Is.EqualTo(port1));
            Assert.That(sut.AutoDewStatus[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("P1:0", true, 0, false)]
        [TestCase("P1:1", true, 0, true)]
        [TestCase("P2:0", true, 1, false)]
        [TestCase("P2:1", true, 1, true)]
        [TestCase("P3:0", true, 2, false)]
        [TestCase("P3:1", true, 2, true)]
        [TestCase("P4:0", true, 3, false)]
        [TestCase("P4:1", true, 3, true)]
        [TestCase("U2:0", false)]
        [TestCase("PX:1", false)]
        [TestCase("P111", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestSetPowerResponse(string deviceResponse, bool valid, short switchNr = 0, bool on = false) {
            var sut = new SetPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.SwitchNumber, Is.EqualTo(switchNr));
            Assert.That(sut.On, Is.EqualTo(on));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("U1:0", true, 0, false)]
        [TestCase("U1:1", true, 0, true)]
        [TestCase("U2:0", true, 1, false)]
        [TestCase("U2:1", true, 1, true)]
        [TestCase("U3:0", true, 2, false)]
        [TestCase("U3:1", true, 2, true)]
        [TestCase("U4:0", true, 3, false)]
        [TestCase("U4:1", true, 3, true)]
        [TestCase("U5:0", true, 4, false)]
        [TestCase("U5:1", true, 4, true)]
        [TestCase("U6:0", true, 5, false)]
        [TestCase("U6:1", true, 5, true)]
        [TestCase("P2:0", false)]
        [TestCase("UX:1", false)]
        [TestCase("U111", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestSetUsbPowerResponse(string deviceResponse, bool valid, short switchNr = 0, bool on = false) {
            var sut = new SetUsbPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.SwitchNumber, Is.EqualTo(switchNr));
            Assert.That(sut.On, Is.EqualTo(on));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("PS:1111:8", true, true, true, true, true, 8)]
        [TestCase("PS:1111:12000", true, true, true, true, true, 0)]
        [TestCase("PS:0111:8", true, false, true, true, true, 8)]
        [TestCase("PS:1011:8", true, true, false, true, true, 8)]
        [TestCase("PS:1101:8", true, true, true, false, true, 8)]
        [TestCase("PS:1110:8", true, true, true, true, false, 8)]
        [TestCase("XXX1110:8", false)]
        [TestCase("PS:X:8", false)]
        [TestCase("PS:1111:X", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestPowerStatusResponse(string deviceResponse, bool valid, bool port0 = false, bool port1 = false, bool port2 = false, bool port3 = false, double voltage = 0) {
            var sut = new PowerStatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.PowerStatusOnBoot[0], Is.EqualTo(port0));
            Assert.That(sut.PowerStatusOnBoot[1], Is.EqualTo(port1));
            Assert.That(sut.PowerStatusOnBoot[2], Is.EqualTo(port2));
            Assert.That(sut.PowerStatusOnBoot[3], Is.EqualTo(port3));
            Assert.That(sut.VariableVoltage, Is.EqualTo(voltage));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("P8:8", true, 8)]
        [TestCase("P8:1200", true, 0)]
        [TestCase("XXX8", false)]
        [TestCase("P8:X", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestSetVariableVoltageResponse(string deviceResponse, bool valid, double voltage = 0d) {
            var sut = new SetVariableVoltageResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.VariableVoltage, Is.EqualTo(voltage));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("P5:000", true, 0, 0)]
        [TestCase("P5:255", true, 0, 100)]
        [TestCase("P6:000", true, 1, 0)]
        [TestCase("P6:255", true, 1, 100)]
        [TestCase("P7:000", true, 2, 0)]
        [TestCase("P7:255", true, 2, 100)]
        [TestCase("PX:000", false)]
        [TestCase("P5:X", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestSetDewHeaterPowerResponse(string deviceResponse, bool valid, short heater = 0,
            double dutyCycle = 0) {
            var sut = new SetDewHeaterPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DewHeaterNumber, Is.EqualTo(heater));
            Assert.That(sut.DutyCycle, Is.EqualTo(dutyCycle));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("0.23:12.4:640.5:86400", true, 0.23, 12.4, 640.5, 86400)]
        [TestCase("X:12.4:640.5:86400", false)]
        [TestCase("0.23:X:640.5:86400", false)]
        [TestCase("0.23:12.4:X:86400", false)]
        [TestCase("0.23:12.4:640.5:X", false)]
        [TestCase("0.23:12.4", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestPowerConsumptionResponse(string deviceResponse, bool valid, double averagePower = 0d,
            double ampereHours = 0d, double wattHours = 0d, long milliseconds = 0) {
            var sut = new PowerConsumptionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.AveragePower, Is.EqualTo(averagePower));
            Assert.That(sut.AmpereHours, Is.EqualTo(ampereHours));
            Assert.That(sut.WattHours, Is.EqualTo(wattHours));
            Assert.That(sut.UpTime, Is.EqualTo(TimeSpan.FromMilliseconds(milliseconds)));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("PD:0", true, false, false, false)]
        [TestCase("PD:1", true, true, true, true)]
        [TestCase("PD:2", true, true, false, false)]
        [TestCase("PD:3", true, false, true, false)]
        [TestCase("PD:4", true, false, false, true)]
        [TestCase("PD:5", true, true, true, false)]
        [TestCase("PD:6", true, true, false, true)]
        [TestCase("PD:7", true, false, true, true)]
        [TestCase("PD:8", true, false, false, false)]
        [TestCase("XX:7", false)]
        [TestCase("PD:X", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestSetAutoDewResponse(string deviceResponse, bool valid, bool heater0 = false,
            bool heater1 = false, bool heater2 = false) {
            var sut = new SetAutoDewResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.AutoDewStatus[0], Is.EqualTo(heater0));
            Assert.That(sut.AutoDewStatus[1], Is.EqualTo(heater1));
            Assert.That(sut.AutoDewStatus[2], Is.EqualTo(heater2));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("0", true, 0d)]
        [TestCase("45.3", true, 45.3)]
        [TestCase("-43.78", true, -43.78)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperTemperatureResponse(string deviceResponse, bool valid, double temperature = 0d) {
            var sut = new StepperMotorTemperatureResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.Temperature, Is.EqualTo(temperature));
        }

        [Test]
        [TestCase("0", true, 0)]
        [TestCase("4500000", true, 4500000)]
        [TestCase("-43000000", true, -43000000)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperGetCurrentPositionResponse(string deviceResponse, bool valid, int position = 0) {
            var sut = new StepperMotorGetCurrentPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("SM:0", true, 0)]
        [TestCase("SM:4500000", true, 4500000)]
        [TestCase("SM:-43000000", true, -43000000)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMoveToPositionResponse(string deviceResponse, bool valid, int position = 0) {
            var sut = new StepperMotorMoveToPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("0", true, false)]
        [TestCase("1", true, true)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMotorIsMovingResponse(string deviceResponse, bool valid, bool isMoving = false) {
            var sut = new StepperMotorIsMovingResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.IsMoving, Is.EqualTo(isMoving));
        }

        [Test]
        [TestCase("SH", true)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMotorHaltResponse(string deviceResponse, bool valid) {
            var sut = new StepperMotorHaltResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
        }

        [Test]
        [TestCase("SR:1", true, false)]
        [TestCase("SR:0", true, true)]
        [TestCase("SR:X", false)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMotorDirectionResponse(string deviceResponse, bool valid, bool isClockwise = false) {
            var sut = new StepperMotorDirectionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.DirectionClockwise, Is.EqualTo(isClockwise));
        }

        [Test]
        [TestCase("SC:0", true, 0)]
        [TestCase("SC:4500000", true, 4500000)]
        [TestCase("SC:-43000000", true, -43000000)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMotorSetCurrentPositionResponse(string deviceResponse, bool valid, int position = 0) {
            var sut = new StepperMotorSetCurrentPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("SB:0", true, 0)]
        [TestCase("SB:4500000", true, 4500000)]
        [TestCase("SB:-43000000", true, -43000000)]
        [TestCase("XXX", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void TestStepperMotorSetBacklashStepsResponse(string deviceResponse, bool valid, int steps = 0) {
            var sut = new StepperMotorSetBacklashStepsResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.Steps, Is.EqualTo(steps));
        }
    }
}