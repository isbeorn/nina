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
    public class QueryCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new QueryCommand(QueryCommand.QueryCommandFlag.None);

            sut.CommandCode.Should().Be(0xca);
            sut.AcknowledgeCode.Should().Be(0xca);
            sut.SubCommandCode.Should().Be(0x10);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase(QueryCommand.QueryCommandFlag.None, null, null, false, 0, 0, 0, 0, 0)]
        [TestCase(QueryCommand.QueryCommandFlag.AutoguidingState, new byte[] { 0x00 }, null, false, 0, 0, 0, 0, 0)]
        [TestCase(QueryCommand.QueryCommandFlag.AutoguidingState, new byte[] { 0x01 }, null, true, 0, 0, 0, 0, 0)]
        [TestCase(QueryCommand.QueryCommandFlag.FrameInfo, null, new byte[] { 0x12, 0x34, 0xff, 0xff, 0x9a, 0xbc, 0xde, 0xf0, 0xff, 0x34, 0x56, 0x78 }, false, 18, -204, -2179942, -16, 22068)]
        [TestCase(QueryCommand.QueryCommandFlag.FrameInfo | QueryCommand.QueryCommandFlag.AutoguidingState, new byte[] { 0x01 }, new byte[] { 0x12, 0x34, 0xff, 0xff, 0x9a, 0xbc, 0xde, 0xf0, 0xff, 0x34, 0x56, 0x78 }, true, 18, -204, -2179942, -16, 22068)]
        [TestCase(QueryCommand.QueryCommandFlag.All | QueryCommand.QueryCommandFlag.None, new byte[] { 0x01 }, new byte[] { 0x12, 0x34, 0xff, 0xff, 0x9a, 0xbc, 0xde, 0xf0, 0xff, 0x34, 0x56, 0x78 }, true, 18, -204, -2179942, -16, 22068)]
        [TestCase(QueryCommand.QueryCommandFlag.All, new byte[] { 0x01 }, new byte[] { 0x12, 0x34, 0xff, 0xff, 0x9a, 0xbc, 0xde, 0xf0, 0xff, 0x34, 0x56, 0x78 }, true, 18, -204, -2179942, -16, 22068)]
        public void Successful_Scenario_Test(QueryCommand.QueryCommandFlag flag, byte[] guidingValues, byte[] frameInfoValues, bool expectedAutoGuiderActive, byte frameIndex, int positionX, int positionY, short distanceRA, short distanceDec) {
            var expectedFrameInfo = frameIndex != 0 ? new FrameInfo(frameIndex, positionX, positionY, distanceRA, distanceDec) : null;

            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x10 }, new byte[] { (byte)flag });
            if (guidingValues != null && frameInfoValues != null) {
                SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 }, guidingValues, frameInfoValues);
            } else if (guidingValues != null) {
                SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 }, guidingValues);
            } else if (frameInfoValues != null) {
                SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 }, frameInfoValues);
            } else {
                SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 });
            }

            var sut = new QueryCommand(flag);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();

            result.AutoGuiderActive.Should().Be(expectedAutoGuiderActive);
            result.FrameInfo.Should().BeEquivalentTo(expectedFrameInfo);
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf1, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf2, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf3, typeof(UnexpectedReturnCodeException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x10 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { errorCode });

            var sut = new QueryCommand(QueryCommand.QueryCommandFlag.None);
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}
