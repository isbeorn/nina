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
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MySwitch.PegasusAstro;
using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class DataProviderSwitchTest {
        private Mock<IPegasusDevice> _mockSdk;
        private DataProviderSwitch _sut;

        [SetUp]
        public void Init() {
            _mockSdk = new Mock<IPegasusDevice>();
            _sut = new DataProviderSwitch { Sdk = _mockSdk.Object };
        }

        [Test]
        public async Task TestPoll() {
            var statusResponse = new StatusResponse { DeviceResponse = "UPB:12.2:0.2:2:23.2:59:14.7:1111:111111:0:0:0:0:0:0:0:0:0:0:0000000:0" };
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>()))
                .Returns(statusResponse);
            var powerConsumptionResponse = new PowerConsumptionResponse { DeviceResponse = "0.23:123.4:734.6:86400000" };
            _mockSdk.Setup(m => m.SendCommand<PowerConsumptionResponse>(It.IsAny<PowerConsumptionCommand>()))
                .Returns(powerConsumptionResponse);
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
            Assert.That(_sut.UpTime, Is.EqualTo($"{powerConsumptionResponse.UpTime.Days} {Loc.Instance["LblDays"]}, " +
                                                $"{powerConsumptionResponse.UpTime.Hours} {Loc.Instance["LblHours"]}, " +
                                                $"{powerConsumptionResponse.UpTime.Minutes} {Loc.Instance["LblMinutes"]}"));
        }

        [Test]
        public async Task TestPollException() {
            _mockSdk.Setup(m => m.SendCommand<StatusResponse>(It.IsAny<StatusCommand>())).Throws(new Exception());
            var result = await _sut.Poll();
            Assert.That(result, Is.False);
        }

        [Test]
        public void TestSetValue() {
            async Task TestDelegate() => await _sut.SetValue();
            Assert.That((Func<Task>)TestDelegate, Throws.TypeOf<NotImplementedException>());
        }
    }
}