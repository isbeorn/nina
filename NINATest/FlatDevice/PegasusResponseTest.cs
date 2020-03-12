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

using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;
using NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK;

namespace NINATest.FlatDevice {

    public class PegasusResponseTest {

        [Test]
        [TestCase("Status", "OK_FM", true)]
        [TestCase("Status", "ERR", false)]
        [TestCase("Status", null, false)]
        [TestCase("Status", "", false)]
        [TestCase("OnOff", "E:0", true)]
        [TestCase("OnOff", "E:1", true)]
        [TestCase("OnOff", "ERR", false)]
        [TestCase("OnOff", null, false)]
        [TestCase("OnOff", "", false)]
        [TestCase("SetBrightness", "L:20", true)]
        [TestCase("SetBrightness", "ERR", false)]
        [TestCase("SetBrightness", null, false)]
        [TestCase("SetBrightness", "", false)]
        public void TestIsValidResponse(string responseName, string response, bool valid) {
            var sut = (Response)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK.{responseName}Response").Unwrap();
            sut.DeviceResponse = response;
            Assert.That(sut.IsValid, Is.EqualTo(valid));
        }

        [TestCase("V:1.3", true, 1.3)]
        [TestCase("V:XXX", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void TestFirmwareVersionResponse(string response, bool valid, double firmwareVersion = 0) {
            var sut = new FirmwareVersionResponse { DeviceResponse = response };
            Assert.That(sut.IsValid, Is.EqualTo(valid));
            if (!sut.IsValid) return;
            Assert.That(sut.FirmwareVersion, Is.EqualTo(firmwareVersion));
        }
    }
}