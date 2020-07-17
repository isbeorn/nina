#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class PegasusResponsesTest {

        [Test]
        [TestCase("1.3", 1.3)]
        public void TestValidFirmwareVersionResponse(string deviceResponse, double version) {
            var sut = new FirmwareVersionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.FirmwareVersion, Is.EqualTo(version));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("ERR")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidFirmwareVersionResponse(string deviceResponse) {
            Assert.That(() => new FirmwareVersionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0",
            "UPB", 12.2, 0d, 0, 23.2, 59, 14.7)]
        [TestCase("UPB:14.2:3.5:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0",
            "UPB", 14.2, 3.5, 50, -23.2, -2, -0.3)]
        public void TestValidStatusResponseDirectProperties(string deviceResponse, string deviceName, double inputVoltage,
            double inputCurrent, int power, double temperature, double humidity, double dewPoint) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
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
        [TestCase("UPB:XXX:3.5:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:XXX:50:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:3.5:50.0:-23.2:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:3.5:50:XXX:-2:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:3.5:50:23.2:XXX:-0.3:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:3.5:50:23.2:59:XXX:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:XXX:111111:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:XXX:0:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:X:0:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:X:0:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:X:0:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:X:0:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:X:0:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:X:0:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:X:0:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:X:0:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:X:0:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:X:0000000:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:XXX:0")]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:X")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStatusResponse(string deviceResponse) {
            Assert.That(() => new StatusResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:0111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1011:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, false, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1101:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1110:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, false)]
        public void TestValidStatusResponsePowerPortOn(string deviceResponse, bool port0, bool port1, bool port2, bool port3) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.PowerPortOn[0], Is.EqualTo(port0));
            Assert.That(sut.PowerPortOn[1], Is.EqualTo(port1));
            Assert.That(sut.PowerPortOn[2], Is.EqualTo(port2));
            Assert.That(sut.PowerPortOn[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:011111:0:0:0:0:0:0:0:0:0:0:0000000:0", false, true, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:101111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, false, true, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:110111:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, false, true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111011:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, false, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111101:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111110:0:0:0:0:0:0:0:0:0:0:0000000:0", true, true, true, true, true, false)]
        public void TestValidStatusResponseUsbPortOn(string deviceResponse, bool port0, bool port1, bool port2, bool port3, bool port4, bool port5) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.UsbPortOn[0], Is.EqualTo(port0));
            Assert.That(sut.UsbPortOn[1], Is.EqualTo(port1));
            Assert.That(sut.UsbPortOn[2], Is.EqualTo(port2));
            Assert.That(sut.UsbPortOn[3], Is.EqualTo(port3));
            Assert.That(sut.UsbPortOn[4], Is.EqualTo(port4));
            Assert.That(sut.UsbPortOn[5], Is.EqualTo(port5));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:128:255:0:0:0:0:0:0:0:0000000:0", 0, 128, 255)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:255:0:128:0:0:0:0:0:0:0:0000000:0", 255, 0, 128)]
        public void TestValidStatusResponseDewHeaterCycle(string deviceResponse, short cycle0, short cycle1, short cycle2) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.DewHeaterDutyCycle[0], Is.EqualTo(cycle0));
            Assert.That(sut.DewHeaterDutyCycle[1], Is.EqualTo(cycle1));
            Assert.That(sut.DewHeaterDutyCycle[2], Is.EqualTo(cycle2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:300:0:0:0:0:0:0:0000000:0", 1, 0, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:600:0:0:0:0:0:0000000:0", 0, 2, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:900:0:0:0:0:0000000:0", 0, 0, 3, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:1200:0:0:0:0000000:0", 0, 0, 0, 4)]
        public void TestValidStatusResponsePortPowerFlow(string deviceResponse, double port0, double port1, double port2, double port3) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.PortPowerFlow[0], Is.EqualTo(port0));
            Assert.That(sut.PortPowerFlow[1], Is.EqualTo(port1));
            Assert.That(sut.PortPowerFlow[2], Is.EqualTo(port2));
            Assert.That(sut.PortPowerFlow[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:300:0:0:0000000:0", 1, 0, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:600:0:0000000:0", 0, 2, 0)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:1200:0000000:0", 0, 0, 2)]
        public void TestValidStatusResponseDewHeaterPowerFlow(string deviceResponse, double port0, double port1, double port2) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.DewHeaterPowerFlow[0], Is.EqualTo(port0));
            Assert.That(sut.DewHeaterPowerFlow[1], Is.EqualTo(port1));
            Assert.That(sut.DewHeaterPowerFlow[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:1000000:0", true, false, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0100000:0", false, true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0010000:0", false, false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0001000:0", false, false, false, true)]
        public void TestValidStatusResponsePortOverCurrent(string deviceResponse, bool port0, bool port1, bool port2, bool port3) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.PortOverCurrent[0], Is.EqualTo(port0));
            Assert.That(sut.PortOverCurrent[1], Is.EqualTo(port1));
            Assert.That(sut.PortOverCurrent[2], Is.EqualTo(port2));
            Assert.That(sut.PortOverCurrent[3], Is.EqualTo(port3));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000100:0", true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000010:0", false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000001:0", false, false, true)]
        public void TestValidStatusResponseDewHeaterOverCurrent(string deviceResponse, bool port0, bool port1, bool port2) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.DewHeaterOverCurrent[0], Is.EqualTo(port0));
            Assert.That(sut.DewHeaterOverCurrent[1], Is.EqualTo(port1));
            Assert.That(sut.DewHeaterOverCurrent[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0", false, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:1", true, true, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:2", true, false, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:3", false, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:4", false, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:5", true, true, false)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:6", true, false, true)]
        [TestCase("UPB:12.2:0.0:0:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:7", false, true, true)]
        public void TestValidStatusResponseAutoDewStatus(string deviceResponse, bool port0, bool port1, bool port2) {
            var sut = new StatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.AutoDewStatus[0], Is.EqualTo(port0));
            Assert.That(sut.AutoDewStatus[1], Is.EqualTo(port1));
            Assert.That(sut.AutoDewStatus[2], Is.EqualTo(port2));
        }

        [Test]
        [TestCase("P1:0", 0, false)]
        [TestCase("P1:1", 0, true)]
        [TestCase("P2:0", 1, false)]
        [TestCase("P2:1", 1, true)]
        [TestCase("P3:0", 2, false)]
        [TestCase("P3:1", 2, true)]
        [TestCase("P4:0", 3, false)]
        [TestCase("P4:1", 3, true)]
        public void TestValidSetPowerResponse(string deviceResponse, short switchNr, bool on) {
            var sut = new SetPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.SwitchNumber, Is.EqualTo(switchNr));
            Assert.That(sut.On, Is.EqualTo(on));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("U2:0")]
        [TestCase("PX:1")]
        [TestCase("P111")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidSetPowerResponse(string deviceResponse) {
            Assert.That(() => new SetPowerResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("U1:0", 0, false)]
        [TestCase("U1:1", 0, true)]
        [TestCase("U2:0", 1, false)]
        [TestCase("U2:1", 1, true)]
        [TestCase("U3:0", 2, false)]
        [TestCase("U3:1", 2, true)]
        [TestCase("U4:0", 3, false)]
        [TestCase("U4:1", 3, true)]
        [TestCase("U5:0", 4, false)]
        [TestCase("U5:1", 4, true)]
        [TestCase("U6:0", 5, false)]
        [TestCase("U6:1", 5, true)]
        public void TestValidSetUsbPowerResponse(string deviceResponse, short switchNr, bool on) {
            var sut = new SetUsbPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.SwitchNumber, Is.EqualTo(switchNr));
            Assert.That(sut.On, Is.EqualTo(on));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("P2:0")]
        [TestCase("UX:1")]
        [TestCase("U111")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidSetUsbPowerResponse(string deviceResponse) {
            Assert.That(() => new SetUsbPowerResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("PS:1111:8", true, true, true, true, 8)]
        [TestCase("PS:1111:12000", true, true, true, true, 0)]
        [TestCase("PS:0111:8", false, true, true, true, 8)]
        [TestCase("PS:1011:8", true, false, true, true, 8)]
        [TestCase("PS:1101:8", true, true, false, true, 8)]
        [TestCase("PS:1110:8", true, true, true, false, 8)]
        public void TestValidPowerStatusResponse(string deviceResponse, bool port0, bool port1, bool port2, bool port3, double voltage) {
            var sut = new PowerStatusResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.PowerStatusOnBoot[0], Is.EqualTo(port0));
            Assert.That(sut.PowerStatusOnBoot[1], Is.EqualTo(port1));
            Assert.That(sut.PowerStatusOnBoot[2], Is.EqualTo(port2));
            Assert.That(sut.PowerStatusOnBoot[3], Is.EqualTo(port3));
            Assert.That(sut.VariableVoltage, Is.EqualTo(voltage));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("XXX1110:8")]
        [TestCase("PS:X:8")]
        [TestCase("PS:1111:X")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidPowerStatusResponse(string deviceResponse) {
            Assert.That(() => new PowerStatusResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("P8:8", 8)]
        [TestCase("P8:1200", 0)]
        public void TestValidSetVariableVoltageResponse(string deviceResponse, double voltage) {
            var sut = new SetVariableVoltageResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.VariableVoltage, Is.EqualTo(voltage));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("XXX8")]
        [TestCase("P8:X")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidSetVariableVoltageResponse(string deviceResponse) {
            Assert.That(() => new SetVariableVoltageResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("P5:000", 0, 0)]
        [TestCase("P5:255", 0, 100)]
        [TestCase("P6:000", 1, 0)]
        [TestCase("P6:255", 1, 100)]
        [TestCase("P7:000", 2, 0)]
        [TestCase("P7:255", 2, 100)]
        public void TestValidSetDewHeaterPowerResponse(string deviceResponse, short heater, double dutyCycle) {
            var sut = new SetDewHeaterPowerResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.DewHeaterNumber, Is.EqualTo(heater));
            Assert.That(sut.DutyCycle, Is.EqualTo(dutyCycle));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("PX:000")]
        [TestCase("P5:X")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidSetDewHeaterPowerResponse(string deviceResponse) {
            Assert.That(() => new SetDewHeaterPowerResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("0.23:12.4:640.5:86400", 0.23, 12.4, 640.5, 86400)]
        public void TestValidPowerConsumptionResponse(string deviceResponse, double averagePower, double ampereHours, double wattHours, long milliseconds) {
            var sut = new PowerConsumptionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.AveragePower, Is.EqualTo(averagePower));
            Assert.That(sut.AmpereHours, Is.EqualTo(ampereHours));
            Assert.That(sut.WattHours, Is.EqualTo(wattHours));
            Assert.That(sut.UpTime, Is.EqualTo(TimeSpan.FromMilliseconds(milliseconds)));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("X:12.4:640.5:86400")]
        [TestCase("0.23:X:640.5:86400")]
        [TestCase("0.23:12.4:X:86400")]
        [TestCase("0.23:12.4:640.5:X")]
        [TestCase("0.23:12.4")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidPowerConsumptionResponse(string deviceResponse) {
            Assert.That(() => new PowerConsumptionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("PD:0", false, false, false)]
        [TestCase("PD:1", true, true, true)]
        [TestCase("PD:2", true, false, false)]
        [TestCase("PD:3", false, true, false)]
        [TestCase("PD:4", false, false, true)]
        [TestCase("PD:5", true, true, false)]
        [TestCase("PD:6", true, false, true)]
        [TestCase("PD:7", false, true, true)]
        [TestCase("PD:8", false, false, false)]
        public void TestValidSetAutoDewResponse(string deviceResponse, bool heater0, bool heater1, bool heater2) {
            var sut = new SetAutoDewResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.AutoDewStatus[0], Is.EqualTo(heater0));
            Assert.That(sut.AutoDewStatus[1], Is.EqualTo(heater1));
            Assert.That(sut.AutoDewStatus[2], Is.EqualTo(heater2));
            Assert.That(sut.ToString().Contains(deviceResponse), Is.True);
        }

        [Test]
        [TestCase("XX:7")]
        [TestCase("PD:X")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidSetAutoDewResponse(string deviceResponse) {
            Assert.That(() => new SetAutoDewResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("0", 0d)]
        [TestCase("45.3", 45.3)]
        [TestCase("-43.78", -43.78)]
        public void TestValidStepperTemperatureResponse(string deviceResponse, double temperature) {
            var sut = new StepperMotorTemperatureResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.Temperature, Is.EqualTo(temperature));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperTemperatureResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorTemperatureResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("0", 0)]
        [TestCase("4500000", 4500000)]
        [TestCase("-43000000", -43000000)]
        public void TestValidStepperGetCurrentPositionResponse(string deviceResponse, int position) {
            var sut = new StepperMotorGetCurrentPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperGetCurrentPositionResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorGetCurrentPositionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("SM:0", 0)]
        [TestCase("SM:4500000", 4500000)]
        [TestCase("SM:-43000000", -43000000)]
        public void TestValidStepperMoveToPositionResponse(string deviceResponse, int position) {
            var sut = new StepperMotorMoveToPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMoveToPositionResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorMoveToPositionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("0", false)]
        [TestCase("1", true)]
        public void TestValidStepperMotorIsMovingResponse(string deviceResponse, bool isMoving) {
            var sut = new StepperMotorIsMovingResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.IsMoving, Is.EqualTo(isMoving));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMotorIsMovingResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorIsMovingResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("SH")]
        public void TestValidStepperMotorHaltResponse(string deviceResponse) {
            _ = new StepperMotorHaltResponse { DeviceResponse = deviceResponse };
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMotorHaltResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorHaltResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("SR:1", false)]
        [TestCase("SR:0", true)]
        public void TestValidStepperMotorDirectionResponse(string deviceResponse, bool isClockwise) {
            var sut = new StepperMotorDirectionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.DirectionClockwise, Is.EqualTo(isClockwise));
        }

        [Test]
        [TestCase("SR:X")]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMotorDirectionResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorDirectionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("SC:0", 0)]
        [TestCase("SC:4500000", 4500000)]
        [TestCase("SC:-43000000", -43000000)]
        public void TestValidStepperMotorSetCurrentPositionResponse(string deviceResponse, int position) {
            var sut = new StepperMotorSetCurrentPositionResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.Position, Is.EqualTo(position));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMotorSetCurrentPositionResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorSetCurrentPositionResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [Test]
        [TestCase("SB:0", 0)]
        [TestCase("SB:4500000", 4500000)]
        [TestCase("SB:-43000000", -43000000)]
        public void TestValidStepperMotorSetBacklashStepsResponse(string deviceResponse, int steps) {
            var sut = new StepperMotorSetBacklashStepsResponse { DeviceResponse = deviceResponse };
            Assert.That(sut.Steps, Is.EqualTo(steps));
        }

        [Test]
        [TestCase("XXX")]
        [TestCase("")]
        [TestCase(null)]
        public void TestInvalidStepperMotorSetBacklashStepsResponse(string deviceResponse) {
            Assert.That(() => new StepperMotorSetBacklashStepsResponse { DeviceResponse = deviceResponse }, Throws.TypeOf<InvalidDeviceResponseException>());
        }
    }
}