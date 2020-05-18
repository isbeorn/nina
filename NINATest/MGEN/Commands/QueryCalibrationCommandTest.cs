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
using FluentAssertions.Specialized;
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
    public class QueryCalibrationCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new QueryCalibrationCommand();

            sut.CommandCode.Should().Be(0xca);
            sut.AcknowledgeCode.Should().Be(0xca);
            sut.SubCommandCode.Should().Be(0x29);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        [TestCase(0x00, 0x00, CalibrationStatus.NotStarted, "")]
        [TestCase(0x01, 0x00, CalibrationStatus.MeasuringStartPosition, "")]
        [TestCase(0x02, 0x00, CalibrationStatus.MovingDecEliminatingBacklash, "")]
        [TestCase(0x03, 0x00, CalibrationStatus.MeasuringDec, "")]
        [TestCase(0x04, 0x00, CalibrationStatus.MeasuringRA, "")]
        [TestCase(0x05, 0x00, CalibrationStatus.AlmostDone, "")]
        [TestCase(0xff, 0x00, CalibrationStatus.Done, "")]
        [TestCase(0xff, 0x01, CalibrationStatus.Error, "The user has canceled the calibration")]
        [TestCase(0xff, 0x02, CalibrationStatus.Error, "Star has been lost (or wasn't present)")]
        [TestCase(0xff, 0x04, CalibrationStatus.Error, "Fatal position error detected")]
        [TestCase(0xff, 0x05, CalibrationStatus.Error, "Orientation error detected")]
        public void Successful_Scenario_Test(byte calibrationState, byte calibrationError, CalibrationStatus status, string errorMessage) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x29 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00, calibrationState, calibrationError });

            var sut = new QueryCalibrationCommand();
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.CalibrationStatus.Should().Be(status);
            result.Error.Should().Be(errorMessage);
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf0, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf1, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf2, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf3, typeof(UnexpectedReturnCodeException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x29 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { errorCode });

            var sut = new QueryCalibrationCommand();
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}
