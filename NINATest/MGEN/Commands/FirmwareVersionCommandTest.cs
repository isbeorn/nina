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
    public class FirmwareVersionCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new FirmwareVersionCommand();

            sut.CommandCode.Should().Be(0x03);
            sut.AcknowledgeCode.Should().Be(0x03);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase(0x12, 0x02, "2.12")]
        [TestCase(0xff, 0x01, "1.ff")]
        [TestCase(0x01, 0xff, "ff.1")]
        public void Successful_Scenario_Test(byte fwLow, byte fwHigh, string expectedVersion) {
            SetupWrite(ftdiMock, new byte[] { 0x03 });
            SetupRead(ftdiMock, new byte[] { 0x03, fwLow, fwHigh });

            var sut = new FirmwareVersionCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.Version.Should().Be(expectedVersion);
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf0, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf1, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf2, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf3, typeof(UnexpectedReturnCodeException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0x03 });
            SetupRead(ftdiMock, new byte[] { errorCode, 0x00, 0x00 });

            var sut = new FirmwareVersionCommand();
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}