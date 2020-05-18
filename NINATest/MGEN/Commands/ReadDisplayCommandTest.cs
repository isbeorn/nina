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
    public class ReadDisplayCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new ReadDisplayCommand(0, 0);

            sut.CommandCode.Should().Be(0x5d);
            sut.AcknowledgeCode.Should().Be(0x5d);
            sut.SubCommandCode.Should().Be(0x0d);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase((ushort)1, (byte)1)]
        [TestCase((ushort)1234, (byte)123)]
        [TestCase((ushort)65535, (byte)255)]
        public void Successful_Scenario_Test(ushort address, byte chunkSize) {
            var addressBytes = GetBytes(address);

            var chunk = new byte[chunkSize];
            var rand = new Random();
            for (var i = 0; i < chunk.Length; i++) {
                chunk[i] = (byte)rand.Next(byte.MaxValue);
            }

            SetupWrite(ftdiMock, new byte[] { 0x5d }, new byte[] { 0x0d }, new byte[] { addressBytes[0], addressBytes[1], chunkSize });
            SetupRead(ftdiMock, new byte[] { 0x5d }, chunk);

            var sut = new ReadDisplayCommand(address, chunkSize);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(chunk);
        }
    }
}
