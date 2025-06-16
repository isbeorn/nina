using FluentAssertions;
using Moq;
using NINA.Astrometry.Interfaces;
using NINA.Astrometry.RiseAndSet;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Sequencer.Utility.DateTimeProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Utility.DateTimeProvider {
    [TestFixture]
    internal class CivilDuskProviderTest {

        [Test]
        public void GetDateTime_NoCivilDusk_ExceptionIsThrown() {
            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var riseAndSetEvent = new CustomRiseAndSet(referenceDate, 0, 0, 0);
            var nighttimeData = new NighttimeData(referenceDate, referenceDate, AstroUtil.MoonPhase.Unknown, null, null, null, null, null, riseAndSetEvent);

            var nightTimeCalculatorMock = new Mock<INighttimeCalculator>();
            nightTimeCalculatorMock.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(nighttimeData);

            var sut = new CivilDuskProvider(nightTimeCalculatorMock.Object);
            sut.DateTime = customDateTimeMock.Object;

            Action act = () => sut.GetDateTime(null);
            act.Should().Throw<Exception>();
            nightTimeCalculatorMock.Verify(x => x.Calculate(It.IsAny<DateTime?>()), Times.Once);
        }

        [Test]
        public void GetDateTime_HasSetEvent_SetEventReturned() {
            var referenceDate = new DateTime(2020, 1, 1, 0, 0, 0);
            var customCivilDusk = new DateTime(2020, 2, 2, 2, 2, 2);

            var customDateTimeMock = new Mock<ICustomDateTime>();
            customDateTimeMock.SetupGet(x => x.Now).Returns(referenceDate);

            var riseAndSetEvent = new CustomRiseAndSet(null, customCivilDusk);
            var nighttimeData = new NighttimeData(referenceDate, referenceDate, AstroUtil.MoonPhase.Unknown, null, null, null, null, null, riseAndSetEvent);

            var nightTimeCalculatorMock = new Mock<INighttimeCalculator>();
            nightTimeCalculatorMock.Setup(x => x.Calculate(It.IsAny<DateTime?>())).Returns(nighttimeData);

            var sut = new CivilDuskProvider(nightTimeCalculatorMock.Object);
            sut.DateTime = customDateTimeMock.Object;

            sut.GetDateTime(null).Should().Be(customCivilDusk);
            nightTimeCalculatorMock.Verify(x => x.Calculate(It.IsAny<DateTime?>()), Times.Once);
        }
    }
}
