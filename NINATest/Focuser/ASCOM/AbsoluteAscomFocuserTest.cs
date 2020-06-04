using ASCOM.DeviceInterface;
using Moq;
using NINA.Model.MyFocuser;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest.Focuser.ASCOM {
    [TestFixture]
    class AbsoluteAscomFocuserTest {
        private AbsoluteAscomFocuser _sut;
        private Mock<IFocuserV3> _mockFocuser;

        [SetUp]
        public async Task Init() {
            _mockFocuser = new Mock<IFocuserV3>();
            _mockFocuser.SetupProperty(m => m.Connected, false);
            _sut = new AbsoluteAscomFocuser(_mockFocuser.Object);
        }

        [Test]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public async Task TestMove(bool connected, bool tempComp, bool expected) {
            _mockFocuser.Setup(m => m.Connected).Returns(connected);
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            _mockFocuser.Setup(m => m.TempComp).Returns(tempComp);
            _mockFocuser.SetupSequence(m => m.Position).Returns(0).Returns(10);
            _mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            var ct = new CancellationToken();
            await _sut.MoveAsync(10, ct);
            if (expected) {
                _mockFocuser.Verify(m => m.Move(10), Times.Once);
            } else {
                _mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
            }
        }
    }
}
