#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Moq;
using NINA.Locale;
using NINA.Model.MySwitch.PegasusAstro;
using NINA.Utility.SerialCommunication;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class DataProviderSwitchTest {
        private DataProviderSwitch _sut;
        private Mock<IPegasusDevice> _mockSdk;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _mockSdk = new Mock<IPegasusDevice>();
        }

        [SetUp]
        public void Init() {
            _mockSdk.Reset();
            _sut = new DataProviderSwitch { Sdk = _mockSdk.Object };
        }

        [Test]
        public async Task TestPoll() {
            var statusResponse = new StatusResponse { DeviceResponse = "UPB:12.2:0.2:2:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0" };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(Task.FromResult(statusResponse));
            var powerConsumptionResponse = new PowerConsumptionResponse { DeviceResponse = "0.23:123.4:734.6:86400000" };
            _mockSdk.Setup(m => m.SendCommand<PowerConsumptionResponse>(It.IsAny<PowerConsumptionCommand>()))
                .Returns(Task.FromResult(powerConsumptionResponse));
            var result = await _sut.Poll();
            Assert.That(result, Is.True);
            Assert.That(_sut.Voltage, Is.EqualTo(12.2));
            Assert.That(_sut.Current, Is.EqualTo(0.2));
            Assert.That(_sut.Power, Is.EqualTo(2));
            Assert.That(_sut.Temperature, Is.EqualTo(23.2));
            Assert.That(_sut.Humidity, Is.EqualTo(59));
            Assert.That(_sut.DewPoint, Is.EqualTo(14.7));
            Assert.That(_sut.AveragePower, Is.EqualTo(0.23));
            Assert.That(_sut.AmpereHours, Is.EqualTo(123.4));
            Assert.That(_sut.WattHours, Is.EqualTo(734.6));
            Assert.That(_sut.AmpereHistory.Count, Is.EqualTo(1));
            Assert.That(_sut.VoltageHistory.Count, Is.EqualTo(1));
            Assert.That(_sut.UpTime, Is.EqualTo(
                $"{powerConsumptionResponse.UpTime.Days} {Loc.Instance["LblDays"]}, " +
                $"{powerConsumptionResponse.UpTime.Hours} {Loc.Instance["LblHours"]}, " +
                $"{powerConsumptionResponse.UpTime.Minutes} {Loc.Instance["LblMinutes"]}"));
        }

        [Test]
        public async Task TestPollInvalidDeviceResponse() {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new InvalidDeviceResponseException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task TestPollSerialPortClosed() {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Throws(new SerialPortClosedException());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestSetValue() {
            Assert.That(async () => await _sut.SetValue(), Throws.TypeOf<NotImplementedException>());
        }
    }
}