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

    [TestFixture]
    public class FlatmasterCommandTest {

        [Test]
        [TestCase("Status", "#")]
        [TestCase("FirmwareVersion", "V")]
        public void TestCommand(string commandName, string commandString) {
            var sut = (ICommand)Activator.CreateInstance("NINA",
                $"NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK.{commandName}Command").Unwrap();
            Assert.That(sut.CommandString, Is.EqualTo($"{commandString}\n"));
            Assert.That(sut.HasResponse, Is.True);
        }

        [Test]
        public void TestSetBrightnessCommand() {
            var sut = new SetBrightnessCommand { Brightness = 100.0 };
            Assert.That(sut.CommandString, Is.EqualTo($"L:100\n"));
            Assert.That(sut.HasResponse, Is.True);
        }

        [Test]
        public void TestOnOffCommandOn() {
            var sut = new OnOffCommand { On = true };
            Assert.That(sut.CommandString, Is.EqualTo($"E:1\n"));
            Assert.That(sut.HasResponse, Is.True);
        }

        [Test]
        public void TestOnOffCommandOff() {
            var sut = new OnOffCommand { On = false };
            Assert.That(sut.CommandString, Is.EqualTo($"E:0\n"));
            Assert.That(sut.HasResponse, Is.True);
        }
    }
}