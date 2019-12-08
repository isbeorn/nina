#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
    public class SetImagingParameterCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new SetImagingParameterCommand(2, 50, 1);

            sut.CommandCode.Should().Be(0xca);
            sut.AcknowledgeCode.Should().Be(0xca);
            sut.SubCommandCode.Should().Be(0x91);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(1000);
        }

        [Test]
        public void Successful_Scenario_Test() {
            byte gain = 0x05;
            var exposure = new byte[] { 0x23, 0x04 };

            var exposureTime = BitConverter.ToUInt16(exposure, 0);
            if (!BitConverter.IsLittleEndian) { exposureTime = BitConverter.ToUInt16(exposure.Reverse().ToArray(), 0); }

            byte threshold = 0x5;
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x91 }, new byte[] { gain, exposure[0], exposure[1], threshold });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 });

            var sut = new SetImagingParameterCommand(gain, exposureTime, threshold);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf0, typeof(UILockedException))]
        [TestCase(0xf1, typeof(AnotherCommandInProgressException))]
        [TestCase(0xf2, typeof(CameraIsOffException))]
        [TestCase(0xf3, typeof(UnexpectedReturnCodeException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x91 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { errorCode });

            var sut = new SetImagingParameterCommand(2, 50, 1);
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}