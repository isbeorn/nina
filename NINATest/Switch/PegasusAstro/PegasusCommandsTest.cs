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

using NINA.Utility.SwitchSDKs.PegasusAstro;
using NUnit.Framework;

namespace NINATest.Switch.PegasusAstro {

    [TestFixture]
    public class PegasusCommandsTest {

        [Test]
        public void TestFirmwareVersionCommand() {
            var sut = new FirmwareVersionCommand();
            Assert.That(sut.CommandString, Is.EqualTo("PV\n"));
        }

        [Test]
        public void TestStatusCommand() {
            var sut = new StatusCommand();
            Assert.That(sut.CommandString, Is.EqualTo("PA\n"));
        }

        [Test]
        [TestCase(1, false, "P1:0\n")]
        [TestCase(1, true, "P1:1\n")]
        public void TestSetPowerCommand(short switchNr, bool on, string command) {
            var sut = new SetPowerCommand { SwitchNumber = switchNr, On = on };
            Assert.That(sut.CommandString, Is.EqualTo(command));
        }

        [Test]
        [TestCase(1, false, "U1:0\n")]
        [TestCase(1, true, "U1:1\n")]
        public void TestSetUsbPowerCommand(short switchNr, bool on, string command) {
            var sut = new SetUsbPowerCommand { SwitchNumber = switchNr, On = on };
            Assert.That(sut.CommandString, Is.EqualTo(command));
        }

        [Test]
        [TestCase(0d, "P8:3\n")]
        [TestCase(100d, "P8:12\n")]
        [TestCase(8d, "P8:8\n")]
        public void TestSetVariableVoltageCommand(double voltage, string command) {
            var sut = new SetVariableVoltageCommand { VariableVoltage = voltage };
            Assert.That(sut.CommandString, Is.EqualTo(command));
        }

        [Test]
        public void TestPowerStatusCommand() {
            var sut = new PowerStatusCommand();
            Assert.That(sut.CommandString, Is.EqualTo("PS\n"));
        }

        [Test]
        [TestCase(0, 100, "P5:255\n")]
        [TestCase(0, 0, "P5:000\n")]
        [TestCase(0, 50, "P5:128\n")]
        public void TestSetDewHeaterPowerCommand(short heater, double dutyCycle, string command) {
            var sut = new SetDewHeaterPowerCommand { DutyCycle = dutyCycle, SwitchNumber = heater };
            Assert.That(sut.CommandString, Is.EqualTo(command));
        }

        [Test]
        public void TestPowerConsumptionCommand() {
            var sut = new PowerConsumptionCommand();
            Assert.That(sut.CommandString, Is.EqualTo("PC\n"));
        }

        [Test]
        [TestCase(false, false, false, 0, false, "PD:0\n")]
        [TestCase(false, false, false, 1, false, "PD:0\n")]
        [TestCase(false, false, false, 2, false, "PD:0\n")]
        [TestCase(false, false, false, 0, true, "PD:2\n")]
        [TestCase(false, false, false, 1, true, "PD:3\n")]
        [TestCase(false, false, false, 2, true, "PD:4\n")]
        [TestCase(true, false, false, 0, false, "PD:0\n")]
        [TestCase(true, false, false, 1, false, "PD:2\n")]
        [TestCase(true, false, false, 2, false, "PD:2\n")]
        [TestCase(true, false, false, 0, true, "PD:2\n")]
        [TestCase(true, false, false, 1, true, "PD:5\n")]
        [TestCase(true, false, false, 2, true, "PD:6\n")]
        [TestCase(false, true, false, 0, false, "PD:3\n")]
        [TestCase(false, true, false, 1, false, "PD:0\n")]
        [TestCase(false, true, false, 2, false, "PD:3\n")]
        [TestCase(false, true, false, 0, true, "PD:5\n")]
        [TestCase(false, true, false, 1, true, "PD:3\n")]
        [TestCase(false, true, false, 2, true, "PD:7\n")]
        [TestCase(false, false, true, 0, false, "PD:4\n")]
        [TestCase(false, false, true, 1, false, "PD:4\n")]
        [TestCase(false, false, true, 2, false, "PD:0\n")]
        [TestCase(false, false, true, 0, true, "PD:6\n")]
        [TestCase(false, false, true, 1, true, "PD:7\n")]
        [TestCase(false, false, true, 2, true, "PD:4\n")]
        [TestCase(true, true, false, 0, false, "PD:3\n")]
        [TestCase(true, true, false, 1, false, "PD:2\n")]
        [TestCase(true, true, false, 2, false, "PD:5\n")]
        [TestCase(true, true, false, 0, true, "PD:5\n")]
        [TestCase(true, true, false, 1, true, "PD:5\n")]
        [TestCase(true, true, false, 2, true, "PD:1\n")]
        [TestCase(true, false, true, 0, false, "PD:4\n")]
        [TestCase(true, false, true, 1, false, "PD:6\n")]
        [TestCase(true, false, true, 2, false, "PD:2\n")]
        [TestCase(true, false, true, 0, true, "PD:6\n")]
        [TestCase(true, false, true, 1, true, "PD:1\n")]
        [TestCase(true, false, true, 2, true, "PD:6\n")]
        [TestCase(false, true, true, 0, false, "PD:7\n")]
        [TestCase(false, true, true, 1, false, "PD:4\n")]
        [TestCase(false, true, true, 2, false, "PD:3\n")]
        [TestCase(false, true, true, 0, true, "PD:1\n")]
        [TestCase(false, true, true, 1, true, "PD:7\n")]
        [TestCase(false, true, true, 2, true, "PD:7\n")]
        [TestCase(true, true, true, 0, false, "PD:7\n")]
        [TestCase(true, true, true, 1, false, "PD:6\n")]
        [TestCase(true, true, true, 2, false, "PD:5\n")]
        [TestCase(true, true, true, 0, true, "PD:1\n")]
        [TestCase(true, true, true, 1, true, "PD:1\n")]
        [TestCase(true, true, true, 2, true, "PD:1\n")]
        public void TestSetAutoDewCommand(bool ch1, bool ch2, bool ch3, short channel, bool on, string command) {
            var current = new[] { ch1, ch2, ch3 };
            var sut = new SetAutoDewCommand(current, channel, on);
            Assert.That(sut.CommandString, Is.EqualTo(command));
        }

        [Test]
        public void TestStepperMotorTemperatureCommand() {
            var sut = new StepperMotorTemperatureCommand();
            Assert.That(sut.CommandString, Is.EqualTo("ST\n"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestStepperMotorMoveToPositionCommand(int position) {
            var sut = new StepperMotorMoveToPositionCommand { Position = position };
            Assert.That(sut.CommandString, Is.EqualTo($"SM:{position}\n"));
        }

        [Test]
        public void TestStepperMotorHaltCommand() {
            var sut = new StepperMotorHaltCommand();
            Assert.That(sut.CommandString, Is.EqualTo("SH\n"));
        }

        [Test]
        [TestCase(true, 0)]
        [TestCase(false, 1)]
        public void TestStepperMotorDirectionCommandCommand(bool directionClockwise, int expected) {
            var sut = new StepperMotorDirectionCommand { DirectionClockwise = directionClockwise };
            Assert.That(sut.CommandString, Is.EqualTo($"SR:{expected}\n"));
        }

        [Test]
        public void TestStepperMotorGetCurrentPositionCommand() {
            var sut = new StepperMotorGetCurrentPositionCommand();
            Assert.That(sut.CommandString, Is.EqualTo("SP\n"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestStepperMotorSetCurrentPositionCommand(int position) {
            var sut = new StepperMotorSetCurrentPositionCommand { Position = position };
            Assert.That(sut.CommandString, Is.EqualTo($"SC:{position}\n"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestStepperMotorSetMaximumSpeedCommand(int speed) {
            var sut = new StepperMotorSetMaximumSpeedCommand { Speed = speed };
            Assert.That(sut.CommandString, Is.EqualTo($"SS:{speed}\n"));
        }

        [Test]
        public void TestStepperMotorIsMovingCommand() {
            var sut = new StepperMotorIsMovingCommand();
            Assert.That(sut.CommandString, Is.EqualTo("SI\n"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestStepperMotorSetBacklashStepsCommand(int steps) {
            var sut = new StepperMotorSetBacklashStepsCommand { Steps = steps };
            Assert.That(sut.CommandString, Is.EqualTo($"SB:{steps}\n"));
        }
    }
}