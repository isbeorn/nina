#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForMoonAltitudeTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void WaitForMoonAltitude_Clone_GoodClone() {
            var sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Data.Offset = 12;
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            var item2 = (WaitForMoonAltitude)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.TargetAltitude.Should().Be(sut.Data.TargetAltitude);
            item2.Data.Comparator.Should().Be(sut.Data.Comparator);
            item2.Data.Offset.Should().Be(12);
        }

        /// <summary>
        /// Verifies that WaitForMoonAltitude can complete immediately when the evaluated threshold means no wait is required.
        /// </summary>
        [Test]
        public async Task WaitForMoonAltitude_Execute_EndsImmediatelyWhenThresholdCannotRequireWaiting() {
            WaitForMoonAltitude sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Offset = 90;
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;

            await sut.Execute(default, CancellationToken.None);

            sut.Data.ExpectedDateTime.Should().BeCloseTo(System.DateTime.Now, System.TimeSpan.FromSeconds(2));
            sut.Data.ExpectedTime.Should().NotBe("--");
        }

        /// <summary>
        /// Verifies that WaitForMoonAltitude rejects offset expressions outside the valid altitude range.
        /// </summary>
        [Test]
        public void WaitForMoonAltitude_Validate_ReportsInvalidOffsetExpression() {
            WaitForMoonAltitude sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.OffsetExpression.Definition = "91";

            sut.Validate().Should().BeFalse();

            sut.Issues.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies that WaitForMoonAltitude restores the expression-backed offset from serialized data when older data is deserialized.
        /// </summary>
        [Test]
        public void WaitForMoonAltitude_OnDeserialized_BackfillsOffsetExpressionFromData() {
            WaitForMoonAltitude sut = new WaitForMoonAltitude(profileServiceMock.Object);
            sut.Data.Offset = 12;
            sut.OffsetExpression = new Expression(string.Empty, sut);

            sut.OnDeserialized(default);

            sut.OffsetExpression.Definition.Should().Be("12");
        }
    }
}
