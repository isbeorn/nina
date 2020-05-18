#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.FlatDeviceSDKs.AlnitakSDK;
using NUnit.Framework;
using System;
using NINA.Utility.SerialCommunication;

namespace NINATest.FlatDevice {

    [TestFixture]
    public class AlnitakCommandTest {

        [Test]
        [TestCase("Ping", "POOO")]
        [TestCase("Open", "OOOO")]
        [TestCase("Close", "COOO")]
        [TestCase("LightOn", "LOOO")]
        [TestCase("LightOff", "DOOO")]
        [TestCase("GetBrightness", "JOOO")]
        [TestCase("State", "SOOO")]
        [TestCase("FirmwareVersion", "VOOO")]
        public void TestCommand(string commandName, string commandString) {
            var sut = (ICommand)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.AlnitakSDK.{commandName}Command").Unwrap();
            Assert.That(sut.CommandString, Is.EqualTo($">{commandString}\r"));
            Assert.That(sut.HasResponse, Is.True);
        }

        [Test]
        public void TestSetBrightnessCommand() {
            var sut = new SetBrightnessCommand() { Brightness = 100.0 };
            Assert.That(sut.CommandString, Is.EqualTo($">B100\r"));
            Assert.That(sut.HasResponse, Is.True);
        }
    }
}
