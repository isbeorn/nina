#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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