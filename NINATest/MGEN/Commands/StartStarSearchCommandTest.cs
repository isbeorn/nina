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
    public class StartStarSearchCommandTest : CommandTestRunner {
        private Mock<IFTDI> ftdiMock = new Mock<IFTDI>();

        [Test]
        public void ConstructorTest() {
            var sut = new StartStarSearchCommand(2, 50);

            sut.CommandCode.Should().Be(0xca);
            sut.AcknowledgeCode.Should().Be(0xca);
            sut.SubCommandCode.Should().Be(0x30);
            sut.RequiredBaudRate.Should().Be(250000);
            sut.Timeout.Should().Be(15000);
        }

        [Test]
        public void Successful_Scenario_Test() {
            byte gain = 0x05;
            var exposureTime = new byte[] { 0xA0, 0x0F };
            byte stars = 215;
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x30 }, new byte[] { gain, exposureTime[0], exposureTime[1] });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { 0x00 }, new byte[] { stars });

            var sut = new StartStarSearchCommand(gain, 4000);
            var result = sut.Execute(ftdiMock.Object);

            result.Success.Should().BeTrue();
            result.NumberOfStars.Should().Be(stars);
        }

        [Test]
        [TestCase(0x99, typeof(UnexpectedReturnCodeException))]
        [TestCase(0xf0, typeof(UILockedException))]
        [TestCase(0xf1, typeof(AnotherCommandInProgressException))]
        [TestCase(0xf2, typeof(CameraIsOffException))]
        [TestCase(0xf3, typeof(AutoGuidingActiveException))]
        public void Exception_Test(byte errorCode, Type ex) {
            SetupWrite(ftdiMock, new byte[] { 0xca }, new byte[] { 0x30 });
            SetupRead(ftdiMock, new byte[] { 0xca }, new byte[] { errorCode });

            var sut = new StartStarSearchCommand(2, 50);
            Action act = () => sut.Execute(ftdiMock.Object);

            TestDelegate test = new TestDelegate(act);

            MethodInfo method = typeof(Assert).GetMethod("Throws", new[] { typeof(TestDelegate) });
            MethodInfo generic = method.MakeGenericMethod(ex);

            generic.Invoke(this, new object[] { test });
        }
    }
}