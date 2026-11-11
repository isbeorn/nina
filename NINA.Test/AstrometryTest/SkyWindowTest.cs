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
using NINA.Core.Model;
using NUnit.Framework;
using static NINA.Test.HorizonTestData;

namespace NINA.Test.AstrometryTest {

    [TestFixture]
    public class SkyWindowTest {

        [Test]
        public void NoProfiles_BoundsAreTheWholeSky() {
            var sut = new SkyWindow(null, null);

            sut.GetLower(123).Should().Be(SkyWindow.DefaultLowerAltitude);
            sut.GetUpper(123).Should().Be(SkyWindow.DefaultUpperAltitude);
            sut.HasLower.Should().BeFalse();
            sut.HasUpper.Should().BeFalse();
        }

        [Test]
        public void OnlyLower_BehavesLikeBefore() {
            var sut = SkyWindow.From(Flat(20));

            sut.HasLower.Should().BeTrue();
            sut.HasUpper.Should().BeFalse();
            sut.GetLower(10).Should().Be(20);
            sut.GetUpper(10).Should().Be(90);
        }

        [Test]
        public void OnlyUpper_LowerDefaultsToZero() {
            var sut = new SkyWindow(null, Flat(70));

            sut.HasLower.Should().BeFalse();
            sut.HasUpper.Should().BeTrue();
            sut.GetLower(10).Should().Be(0);
            sut.GetUpper(10).Should().Be(70);
        }

        [Test]
        public void UpperBelowLower_IsKeptAsDefined() {
            var sut = new SkyWindow(Flat(50), Flat(30));

            sut.GetLower(10).Should().Be(50);
            sut.GetUpper(10).Should().Be(30);
        }

        [Test]
        public void ImplicitConversionFromHorizonWithEmbeddedUpper_KeepsTheUpperBound() {
            SkyWindow sut = Dual(10, 70);

            sut.HasLower.Should().BeTrue();
            sut.HasUpper.Should().BeTrue();
            sut.GetLower(45).Should().Be(10);
            sut.GetUpper(45).Should().Be(70);
        }

        [Test]
        public void From_HorizonWithOnlyUpperValues_HasNoLowerBound() {
            var sut = SkyWindow.From(Dual(null, 70));

            sut.HasLower.Should().BeFalse();
            sut.GetLower(45).Should().Be(SkyWindow.DefaultLowerAltitude);
            sut.GetUpper(45).Should().Be(70);
        }

        [Test]
        public void ImplicitConversionFromNull_HasNoBounds() {
            SkyWindow sut = (CustomHorizon)null!;

            sut.HasLower.Should().BeFalse();
            sut.HasUpper.Should().BeFalse();
        }

        [Test]
        public void AzimuthDependentUpper_IsInterpolated() {
            // upper bound 90 at north, 50 at south
            var upper = FromPoints("0 90\n180 50\n360 90");
            var sut = new SkyWindow(null, upper);

            sut.GetUpper(0).Should().Be(90);
            sut.GetUpper(180).Should().Be(50);
            sut.GetUpper(90).Should().BeApproximately(70, 0.0001);
        }

    }
}
