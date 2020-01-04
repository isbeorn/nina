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

using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NUnit.Framework;

namespace NINATest.FlatDevice {
    [TestFixture]
    public class AlnitakFlipFlatSimulatorTest {
        private AlnitakFlipFlatSimulator _sut;
        private Mock<IProfileService> _mockProfileService;

        [SetUp]
        public void Init() {
            _mockProfileService = new Mock<IProfileService>();
            _sut = new AlnitakFlipFlatSimulator(_mockProfileService.Object);
        }

        [TearDown]
        public void Dispose() {
            _sut.Disconnect();
            Assert.That(_sut.Connected, Is.False);
        }

        [Test]
        public async Task TestConnect() {
            Assert.That(await _sut.Connect(new CancellationToken()), Is.True);
        }

        [Test]
        public async Task TestOpen() {
            Assert.That(await _sut.Open(new CancellationToken()), Is.True);
        }

        [Test]
        public async Task TestClose() {
            Assert.That(await _sut.Close(new CancellationToken()), Is.True);
        }

        [Test]
        public void TestLightOnNotConnected() {
            _sut.LightOn = true;
            Assert.That(_sut.LightOn, Is.False);
        }

        [Test]
        public async Task TestLightOnFreshInstance() {
            await _sut.Connect(new CancellationToken());
            _sut.LightOn = true;
            Assert.That(_sut.LightOn, Is.False);
        }

        [Test]
        public async Task TestLightOnCoverOpen() {
            await _sut.Connect(new CancellationToken());
            await _sut.Open(new CancellationToken());
            _sut.LightOn = true;
            Assert.That(_sut.LightOn, Is.False);
        }

        [Test]
        public async Task TestLightOnCoverClosed() {
            await _sut.Connect(new CancellationToken());
            await _sut.Close(new CancellationToken());
            _sut.LightOn = true;
            Assert.That(_sut.LightOn, Is.True);
        }

        [Test]
        [TestCase(-3.0, 0)]
        [TestCase(0.0, 0)]
        [TestCase(0.5, 0.5)]
        [TestCase(1.0, 1.0)]
        [TestCase(1000, 1.0)]
        public async Task TestBrightness(double setValue, double expectedValue) {
            await _sut.Connect(new CancellationToken());
            _sut.Brightness = setValue;
            Assert.That(_sut.Brightness, Is.EqualTo(expectedValue));
        }
    }
}