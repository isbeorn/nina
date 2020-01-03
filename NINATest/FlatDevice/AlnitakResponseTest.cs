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

using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NUnit.Framework;
using System;
using NINA.Model.MyFlatDevice;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakResponseTest {

        [Test]
        [TestCase("Ping", "*P99OOO", true)]
        [TestCase("Ping", "*P99000", false)]
        [TestCase("Ping", "P99OOO", false)]
        [TestCase("Ping", "*P33OOO", false)]
        [TestCase("Ping", "*PXXOOO", false)]
        [TestCase("Open", "*O99OOO", true)]
        [TestCase("Open", "*O99000", false)]
        [TestCase("Close", "*C99OOO", true)]
        [TestCase("Close", "*C99000", false)]
        [TestCase("LightOn", "*L99OOO", true)]
        [TestCase("LightOn", "*L99000", false)]
        [TestCase("LightOff", "*D99OOO", true)]
        [TestCase("LightOff", "*D99000", false)]
        public void TestIsValidResponse(string responseName, string response, bool valid) {
            var sut = (Response)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.AlnitakSDK.{responseName}Response").Unwrap();
            sut.DeviceResponse = response;
            Assert.That(sut.IsValid, Is.EqualTo(valid));
        }

        [Test]
        [TestCase("*B98100", true, 100)]
        [TestCase("*B33100", false, 0)]
        [TestCase("*B99-10", false, 0)]
        [TestCase("*B99999", false, 0)]
        [TestCase("*B99XXX", false, 0)]
        public void TestSetBrightnessResponse(string response, bool valid, int brightness) {
            var sut = new SetBrightnessResponse {
                DeviceResponse = response
            };

            Assert.That(sut.IsValid, Is.EqualTo(valid));
            Assert.That(sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase("*J98100", true, 100)]
        [TestCase("*J33100", false, 0)]
        [TestCase("*J99-10", false, 0)]
        [TestCase("*J99999", false, 0)]
        [TestCase("*J99XXX", false, 0)]
        [TestCase("*B99100", false, 100)]
        public void TestGetBrightnessResponse(string response, bool valid, int brightness) {
            var sut = new GetBrightnessResponse {
                DeviceResponse = response
            };

            Assert.That(sut.IsValid, Is.EqualTo(valid));
            Assert.That(sut.Brightness, Is.EqualTo(brightness));
        }

        [Test]
        [TestCase("*S99000", true, false, CoverState.NeitherOpenNorClosed, false)]
        [TestCase("*S99111", true, true, CoverState.Closed, true)]
        [TestCase("*S99002", true, false, CoverState.Open, false)]
        [TestCase("*S99003", true, false, CoverState.Unknown, false)]
        [TestCase("*S99004", false, false, CoverState.Unknown, false)]
        [TestCase("*S99020", false, false, CoverState.Unknown, false)]
        [TestCase("*S99200", false, false, CoverState.Unknown, false)]
        public void TestStateResponse(string response, bool valid, bool motorRunning, CoverState covertState,
            bool lightOn) {
            var sut = new StateResponse {
                DeviceResponse = response
            };

            Assert.That(sut.IsValid, Is.EqualTo(valid));
            Assert.That(sut.MotorRunning, Is.EqualTo(motorRunning));
            Assert.That(sut.CoverState, Is.EqualTo(covertState));
            Assert.That(sut.LightOn, Is.EqualTo(lightOn));
        }
    }
}