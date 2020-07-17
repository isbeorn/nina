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
using NINA.MGEN.Commands.CompatibilityMode;
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
    public class EnterNormalModeCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new EnterNormalModeCommand();

            sut.CommandCode.Should().Be(0x42);
            sut.AcknowledgeCode.Should().Be(0x42);
            sut.RequiredBaudRate.Should().Be(9600);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        public void Successful_Scenario_Test() {
            SetupWrite(ftdiMock, new byte[] { 0x42 });

            var sut = new EnterNormalModeCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
        }
    }
}