#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using FTD2XX_NET;
using Moq;
using NINA.MGEN;
using NINA.MGEN.Commands.AppMode;
using NINA.MGEN.Exceptions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.MGEN.Commands {

    [TestFixture]
    public class GetLEDStatesCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new GetLEDStatesCommand();

            sut.CommandCode.Should().Be(0x5d);
            sut.AcknowledgeCode.Should().Be(0x5d);
            sut.SubCommandCode.Should().Be(0x0a);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase(LEDS.BLUE | LEDS.DOWN_RED)]
        [TestCase(LEDS.UP_RED | LEDS.GREEN | LEDS.RIGHT_RED)]
        [TestCase(LEDS.UP_RED)]
        public void Successful_Scenario_Test(byte expectedLEDs) {
            SetupWrite(ftdiMock, new byte[] { 0x5d }, new byte[] { 0x0a }, new byte[] { 1 });
            SetupRead(ftdiMock, new byte[] { 0x5d }, new byte[] { expectedLEDs });

            var sut = new GetLEDStatesCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.LEDs.Should().Be(expectedLEDs);
        }
    }
}