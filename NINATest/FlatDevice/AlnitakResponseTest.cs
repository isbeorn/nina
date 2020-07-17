#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFlatDevice;
using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NINA.Utility.SerialCommunication;
using NUnit.Framework;
using System;
using FluentAssertions;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakResponseTest {

        [Test]
        [TestCase("Ping", "*P99OOO")]
        [TestCase("Open", "*O99OOO")]
        [TestCase("Close", "*C99OOO")]
        [TestCase("LightOn", "*L99OOO")]
        [TestCase("LightOff", "*D99OOO")]
        public void TestIsValidResponse(string responseName, string response) {
            var sut = (AlnitakResponse)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.AlnitakSDK.{responseName}Response").Unwrap();
            Action act = () => sut.DeviceResponse = response;
            act.Should().NotThrow();
        }

        [Test]
        [TestCase("Ping", "*P99000")]
        [TestCase("Ping", "P99OOO")]
        [TestCase("Ping", "*P33OOO")]
        [TestCase("Ping", "*PXXOOO")]
        [TestCase("Ping", null)]
        [TestCase("Ping", "")]
        [TestCase("Open", "*O99000")]
        [TestCase("Open", null)]
        [TestCase("Open", "")]
        [TestCase("Close", "*C99000")]
        [TestCase("Close", null)]
        [TestCase("Close", "")]
        [TestCase("LightOn", "*L99000")]
        [TestCase("LightOn", null)]
        [TestCase("LightOn", "")]
        [TestCase("LightOff", "*D99000")]
        [TestCase("LightOff", null)]
        [TestCase("LightOff", "")]
        public void TestIsInvalidResponse(string responseName, string response) {
            var sut = (AlnitakResponse)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.AlnitakSDK.{responseName}Response").Unwrap();
            Action act = () => sut.DeviceResponse = response;
            act.Should().Throw<InvalidDeviceResponseException>();
        }

        [Test]
        [TestCase("*B98100", 100)]
        public void TestValidSetBrightnessResponse(string response, int brightness) {
            var sut = new SetBrightnessResponse { DeviceResponse = response };

            sut.Brightness.Should().Be(brightness);
        }

        [Test]
        [TestCase("*B33100")]
        [TestCase("*B99-10")]
        [TestCase("*B99999")]
        [TestCase("*B99XXX")]
        [TestCase(null)]
        [TestCase("")]
        public void TestInvalidSetBrightnessResponse(string response) {
            Action act = () => _ = new SetBrightnessResponse { DeviceResponse = response };
            act.Should().Throw<InvalidDeviceResponseException>();
        }

        [Test]
        [TestCase("*J98100", 100)]
        public void TestValidGetBrightnessResponse(string response, int brightness) {
            var sut = new GetBrightnessResponse { DeviceResponse = response };

            sut.Brightness.Should().Be(brightness);
        }

        [Test]
        [TestCase("*J33100")]
        [TestCase("*J99-10")]
        [TestCase("*J99999")]
        [TestCase("*J99XXX")]
        [TestCase("*B99100")]
        [TestCase(null)]
        [TestCase("")]
        public void TestInvalidGetBrightnessResponse(string response) {
            Action act = () => _ = new GetBrightnessResponse { DeviceResponse = response };
            act.Should().Throw<InvalidDeviceResponseException>();
        }

        [Test]
        [TestCase("*S99000", false, CoverState.NeitherOpenNorClosed, false)]
        [TestCase("*S99111", true, CoverState.Closed, true)]
        [TestCase("*S99002", false, CoverState.Open, false)]
        [TestCase("*S99003", false, CoverState.Unknown, false)]
        public void TestValidStateResponse(string response, bool motorRunning, CoverState covertState, bool lightOn) {
            var sut = new StateResponse { DeviceResponse = response };

            sut.Ttl.Should().Be(0);
            sut.MotorRunning.Should().Be(motorRunning);
            sut.CoverState.Should().Be(covertState);
            sut.LightOn.Should().Be(lightOn);
        }

        [Test]
        [TestCase("*S99004")]
        [TestCase("*S99020")]
        [TestCase("*S99200")]
        [TestCase(null)]
        [TestCase("")]
        public void TestInvalidStateResponse(string response) {
            Action act = () => _ = new StateResponse { DeviceResponse = response };
            act.Should().Throw<InvalidDeviceResponseException>();
        }
    }
}