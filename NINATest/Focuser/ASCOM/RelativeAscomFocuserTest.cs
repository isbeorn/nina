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
    class RelativeAscomFocuserTest {
        private RelativeAscomFocuser _sut;
        private Mock<IFocuserV3> _mockFocuser;
        private static readonly int INITIAL_POSITION = 5000;

        [SetUp]
        public async Task Init() {
            _mockFocuser = new Mock<IFocuserV3>();
            _mockFocuser.SetupProperty(m => m.Connected, false);
            _sut = new RelativeAscomFocuser(_mockFocuser.Object);
        }

        [Test]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public async Task TestPositiveMove(bool connected, bool tempComp, bool disableEnableTempComp) {
            _mockFocuser.Setup(m => m.Connected).Returns(connected);
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            _mockFocuser.Setup(m => m.TempComp).Returns(tempComp);
            _mockFocuser.Setup(m => m.MaxStep).Returns(5);
            _mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            var ct = new CancellationToken();
            var moveAmount = 12;
            await _sut.MoveAsync(INITIAL_POSITION + moveAmount, ct);
            if (connected) {
                _mockFocuser.Verify(m => m.Move(5), Times.Exactly(2));
                _mockFocuser.Verify(m => m.Move(2), Times.Once);
                _mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
                Assert.AreEqual(INITIAL_POSITION + moveAmount, _sut.Position);
            } else {
                _mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
                Assert.AreEqual(INITIAL_POSITION, _sut.Position);
            }
            if (disableEnableTempComp) {
                _mockFocuser.VerifySet(m => m.TempComp = true, Times.Once);
                _mockFocuser.VerifySet(m => m.TempComp = false, Times.Once);
            } else {
                _mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
                _mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
            }
        }

        [Test]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public async Task TestNegativeMove(bool connected, bool tempComp, bool disableEnableTempComp) {
            _mockFocuser.Setup(m => m.Connected).Returns(connected);
            _mockFocuser.Setup(m => m.TempCompAvailable).Returns(true);
            _mockFocuser.Setup(m => m.TempComp).Returns(tempComp);
            _mockFocuser.Setup(m => m.MaxStep).Returns(5);
            _mockFocuser.SetupSequence(m => m.IsMoving).Returns(true).Returns(false);

            var ct = new CancellationToken();
            var moveAmount = -12;
            await _sut.MoveAsync(INITIAL_POSITION + moveAmount, ct);
            if (connected) {
                _mockFocuser.Verify(m => m.Move(-5), Times.Exactly(2));
                _mockFocuser.Verify(m => m.Move(-2), Times.Once);
                _mockFocuser.Verify(m => m.IsMoving, Times.Exactly(4));
                Assert.AreEqual(INITIAL_POSITION + moveAmount, _sut.Position);
            } else {
                _mockFocuser.Verify(m => m.Move(It.IsAny<int>()), Times.Never);
                Assert.AreEqual(INITIAL_POSITION, _sut.Position);
            }
            if (disableEnableTempComp) {
                _mockFocuser.VerifySet(m => m.TempComp = true, Times.Once);
                _mockFocuser.VerifySet(m => m.TempComp = false, Times.Once);
            } else {
                _mockFocuser.VerifySet(m => m.TempComp = true, Times.Never);
                _mockFocuser.VerifySet(m => m.TempComp = false, Times.Never);
            }
        }
    }
}
