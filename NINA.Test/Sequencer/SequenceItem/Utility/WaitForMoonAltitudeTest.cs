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
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class WaitForSunAltitudeTest {
        private Mock<IProfileService> profileServiceMock;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(0);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(0);
        }

        [Test]
        public void WaitForMoonAltitude_Clone_GoodClone() {
            var sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Data.Offset = 12;
            sut.Data.Comparator = ComparisonOperatorEnum.LESS_THAN;
            var item2 = (WaitForSunAltitude)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Data.TargetAltitude.Should().Be(sut.Data.TargetAltitude);
            item2.Data.Comparator.Should().Be(sut.Data.Comparator);
            item2.Data.Offset.Should().Be(12);
        }

        /// <summary>
        /// Verifies that WaitForSunAltitude can complete immediately when the evaluated threshold means no wait is required.
        /// </summary>
        [Test]
        public async Task WaitForSunAltitude_Execute_EndsImmediatelyWhenThresholdCannotRequireWaiting() {
            WaitForSunAltitude sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Offset = 90;
            sut.Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;

            await sut.Execute(default, CancellationToken.None);

            sut.Data.ExpectedDateTime.Should().BeCloseTo(System.DateTime.Now, System.TimeSpan.FromSeconds(2));
            sut.Data.ExpectedTime.Should().NotBe("--");
        }

        /// <summary>
        /// Verifies that WaitForSunAltitude rejects offset expressions outside the valid altitude range.
        /// </summary>
        [Test]
        public void WaitForSunAltitude_Validate_ReportsInvalidOffsetExpression() {
            WaitForSunAltitude sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.OffsetExpression.Definition = "91";

            sut.Validate().Should().BeFalse();

            sut.Issues.Should().NotBeEmpty();
        }

        /// <summary>
        /// Verifies that WaitForSunAltitude restores the expression-backed offset from serialized data when older data is deserialized.
        /// </summary>
        [Test]
        public void WaitForSunAltitude_OnDeserialized_BackfillsOffsetExpressionFromData() {
            WaitForSunAltitude sut = new WaitForSunAltitude(profileServiceMock.Object);
            sut.Data.Offset = 12;
            sut.OffsetExpression = new Expression(string.Empty, sut);

            sut.OnDeserialized(default);

            sut.OffsetExpression.Definition.Should().Be("12");
        }

        [Test]
        [TestCase(9.99, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10.0, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(10.01, ComparisonOperatorEnum.LESS_THAN, false)]
        [TestCase(9.99, ComparisonOperatorEnum.GREATER_THAN, false)]
        [TestCase(10.0, ComparisonOperatorEnum.GREATER_THAN, false)]
        [TestCase(10.01, ComparisonOperatorEnum.GREATER_THAN, true)]
        [TestCase(double.NaN, ComparisonOperatorEnum.LESS_THAN, true)]
        [TestCase(double.NaN, ComparisonOperatorEnum.GREATER_THAN, true)]
        public void WaitForSunAltitude_Execute_UsesThresholdAndWaitsForInvalidAltitude(
            double currentAltitude,
            ComparisonOperatorEnum comparator,
            bool shouldWait) {
            var sut = new TestableWaitForSunAltitude(profileServiceMock.Object, currentAltitude);
            sut.Data.Offset = 10.0;
            sut.Data.Comparator = comparator;
            var progress = new Mock<IProgress<ApplicationStatus>>();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            if (shouldWait) {
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await sut.Execute(progress.Object, cancellationTokenSource.Token));
            } else {
                Assert.DoesNotThrowAsync(async () =>
                    await sut.Execute(progress.Object, cancellationTokenSource.Token));
            }
        }

        private sealed class TestableWaitForSunAltitude : WaitForSunAltitude {
            private readonly double currentAltitude;

            public TestableWaitForSunAltitude(IProfileService profileService, double currentAltitude) : base(profileService) {
                this.currentAltitude = currentAltitude;
            }

            public override void CalculateExpectedTime() {
                Data.CurrentAltitude = currentAltitude;
            }
        }
    }
}
