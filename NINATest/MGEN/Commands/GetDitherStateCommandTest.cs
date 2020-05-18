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
using NINA.MGEN.Commands.AppMode;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.MGEN.Commands {

    [TestFixture]
    public class GetDitherStateCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new GetDitherStateCommand();

            sut.CommandCode.Should().Be(0xa8);
            sut.AcknowledgeCode.Should().Be(0xa8);
            sut.SubCommandCode.Should().Be(0x00);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        public void RDActive_Scenario_Test() {
            SetupWrite(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0x00 });
            SetupRead(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0b0001_0000 });

            var sut = new GetDitherStateCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.Dithering.Should().BeTrue();
        }

        [Test]
        public void RDNextExposure_Scenario_Test() {
            SetupWrite(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0x00 });
            SetupRead(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0b0100_0000 });

            var sut = new GetDitherStateCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.Dithering.Should().BeFalse();
        }

        [Test]
        public void GuidingNotActive_Scenario_Test() {
            SetupWrite(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0x00 });
            SetupRead(ftdiMock, new byte[] { 0xa8 }, new byte[] { 0b0000_0000 });

            var sut = new GetDitherStateCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.Dithering.Should().BeFalse();
        }
    }
}
