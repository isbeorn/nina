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