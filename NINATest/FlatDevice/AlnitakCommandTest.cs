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
        }

        [Test]
        public void TestSetBrightnessCommand() {
            var sut = new SetBrightnessCommand() { Brightness = 100.0 };
            Assert.That(sut.CommandString, Is.EqualTo($">B100\r"));
        }
    }
}