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

using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;

namespace NINATest.FlatDevice {

    public class PegasusResponseTest {

        [Test]
        [TestCase("Status", "OK_FM")]
        [TestCase("OnOff", "E:0")]
        [TestCase("OnOff", "E:1")]
        [TestCase("SetBrightness", "L:20")]
        public void TestValidResponse(string responseName, string response) {
            var sut = (Response)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK.{responseName}Response").Unwrap();
            sut.DeviceResponse = response;
        }

        [Test]
        [TestCase("Status", "ERR")]
        [TestCase("Status", null)]
        [TestCase("Status", "")]
        [TestCase("OnOff", "ERR")]
        [TestCase("OnOff", null)]
        [TestCase("OnOff", "")]
        [TestCase("SetBrightness", "ERR")]
        [TestCase("SetBrightness", null)]
        [TestCase("SetBrightness", "")]
        public void TestInvalidResponse(string responseName, string response) {
            var sut = (Response)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK.{responseName}Response").Unwrap();
            Assert.That(() => sut.DeviceResponse = response, Throws.TypeOf<InvalidDeviceResponseException>());
        }

        [TestCase("V:1.3", 1.3)]
        public void TestValidFirmwareVersionResponse(string response, double firmwareVersion) {
            var sut = new FirmwareVersionResponse { DeviceResponse = response };
            Assert.That(sut.FirmwareVersion, Is.EqualTo(firmwareVersion));
        }

        [TestCase("V:XXX")]
        [TestCase(null)]
        [TestCase("")]
        public void TestInvalidFirmwareVersionResponse(string response) {
            Assert.That(() => new FirmwareVersionResponse { DeviceResponse = response }, Throws.TypeOf<InvalidDeviceResponseException>());
        }
    }
}