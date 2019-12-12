using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NINA.Locale;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NINATest.FlatDevice {

    [TestFixture]
    internal class FlatDeviceInfoTest {

        [Test]
        [TestCase(CoverState.NeitherOpenNorClosed)]
        [TestCase(CoverState.Closed)]
        [TestCase(CoverState.Open)]
        [TestCase(CoverState.Unknown)]
        public void TestLocalizedCoverState(CoverState expected) {
            var sut = new FlatDeviceInfo {
                CoverState = expected
            };
            Assert.That(sut.CoverState, Is.EqualTo(expected));
            Assert.That(sut.LocalizedCoverState, Is.EqualTo(Loc.Instance[$"LblFlatDevice{expected}"]));
        }
    }
}