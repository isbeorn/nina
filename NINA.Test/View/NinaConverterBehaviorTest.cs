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
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.View.Equipment;
using NINA.View.Equipment.Switch;
using NINA.View.Options;
using NINA.View.SimpleSequencer;
using NINA.View.Thumbnail;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Expression = NINA.Sequencer.Logic.Expression;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class NinaConverterBehaviorTest {

        /// <summary>
        /// Verifies simple-sequencer gain/offset display keeps invalid driver defaults visible and round-trips numeric edits through the target type parser.
        /// </summary>
        [Test]
        public void CameraGainOffsetConverter_FormatsExpressionDefaultsAndParsesEdits() {
            var converter = new CameraGainOffsetConverter();
            var invalidDefault = new Expression {
                Default = -1,
                Definition = string.Empty,
                IsValid = false
            };
            var configuredDefault = new Expression {
                Default = 139,
                Definition = string.Empty,
                IsValid = true
            };
            var explicitValue = new Expression {
                Definition = "42"
            };

            converter.Convert(new object[] { DependencyProperty.UnsetValue }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("(??)");
            converter.Convert(new object[] { invalidDefault }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be($"({Loc.Instance["LblCamera"]})");
            converter.Convert(new object[] { configuredDefault }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("(139)");
            converter.Convert(new object[] { explicitValue }, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("42");
            converter.ConvertBack("17", new[] { typeof(int) }, null, CultureInfo.InvariantCulture).Should().Equal(17);
            converter.ConvertBack("not an integer", new[] { typeof(int) }, null, CultureInfo.InvariantCulture).Should().Equal(-1);
        }

        /// <summary>
        /// Verifies sequence-mode visibility converters expose rotate-only controls while hiding mutually exclusive standard-mode controls.
        /// </summary>
        [Test]
        public void SequenceModeVisibilityConverters_MapRotateModeSymmetrically() {
            var rotate = new SequenceModeIsRotateToVisibilityConverter();
            var inverse = new InverseSequenceModeIsRotateToVisibilityConverter();

            rotate.Convert(SequenceMode.ROTATE, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
            rotate.Convert(SequenceMode.STANDARD, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
            inverse.Convert(SequenceMode.ROTATE, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
            inverse.Convert(SequenceMode.STANDARD, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
        }

        /// <summary>
        /// Verifies plate-solver option visibility hides duplicate blind-solver settings and leaves distinct solver choices visible.
        /// </summary>
        [Test]
        public void BlindSolverSettingsVisibilityConverter_HidesOnlyDuplicateSolverConfiguration() {
            var converter = new BlindSolverSettingsVisibilityConverter();

            converter.Convert(new object[] { PlateSolverEnum.ASTAP, BlindSolverEnum.ASTAP }, typeof(Visibility), null, CultureInfo.InvariantCulture)
                .Should().Be(Visibility.Collapsed);
            converter.Convert(new object[] { PlateSolverEnum.PLATESOLVE2, BlindSolverEnum.ASTAP }, typeof(Visibility), null, CultureInfo.InvariantCulture)
                .Should().Be(Visibility.Visible);
            converter.Convert(new object[] { PlateSolverEnum.ASTAP }, typeof(Visibility), null, CultureInfo.InvariantCulture)
                .Should().Be(Visibility.Visible);
        }

        /// <summary>
        /// Verifies world-map coordinate converters map longitude/latitude to deterministic canvas positions and reject incomplete bindings.
        /// </summary>
        [Test]
        public void WorldMapConverters_MapGeographicCoordinatesToCanvasPixels() {
            var longitude = new LongitudeWorldMapConverter();
            var latitude = new LatitudeWorldMapConverter();

            longitude.Convert(new object[] { 0d, 360d, 10d }, typeof(double), null, CultureInfo.InvariantCulture).Should().Be(175d);
            longitude.Convert(new object[] { -180d, 360d, 10d }, typeof(double), null, CultureInfo.InvariantCulture).Should().Be(-5d);
            latitude.Convert(new object[] { 45d, 180d, 20d }, typeof(double), null, CultureInfo.InvariantCulture).Should().Be(35d);
            latitude.Convert(new object[] { -90d, 180d, 20d }, typeof(double), null, CultureInfo.InvariantCulture).Should().Be(170d);
            longitude.Convert(new object[] { DependencyProperty.UnsetValue, 360d, 10d }, typeof(double), null, CultureInfo.InvariantCulture).Should().BeNull();
            latitude.Convert(new object[] { 45d, 180d }, typeof(double), null, CultureInfo.InvariantCulture).Should().BeNull();
        }

        /// <summary>
        /// Verifies switch converters treat positive power values as on and convert boolean edits back into the numeric command payload used by switch drivers.
        /// </summary>
        [Test]
        public void SwitchPowerConverters_TranslateBetweenNumericAndBooleanState() {
            var multi = new PowerValueConverter();
            var single = new DoubleToOnOffConverter();

            multi.Convert(new object[] { "ignored", 0.1d }, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
            multi.Convert(new object[] { "ignored", 0d }, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
            multi.Convert(new object[] { "ignored", "not numeric" }, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
            multi.ConvertBack(true, new[] { typeof(double) }, null, CultureInfo.InvariantCulture).Should().Equal(1);
            multi.ConvertBack(false, new[] { typeof(double) }, null, CultureInfo.InvariantCulture).Should().Equal(0);
            single.Convert(1d, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
            single.Convert(0d, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
            single.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        }

        /// <summary>
        /// Verifies filter-wheel position conversion resolves configured profile filters and safely falls back to the null-filter sentinel.
        /// </summary>
        [Test]
        public void FilterPositionToFilterConverter_UsesProfileFiltersAndNullSentinel() {
            var filters = new ObserveAllCollection<FilterInfo> {
                new FilterInfo("L", 0, 0),
                new FilterInfo("Ha", 12, 3)
            };
            var filterSettings = new Mock<IFilterWheelSettings>();
            filterSettings.SetupGet(x => x.FilterWheelFilters).Returns(filters);
            var profile = new Mock<IProfile>();
            profile.SetupGet(x => x.FilterWheelSettings).Returns(filterSettings.Object);
            var profileService = new Mock<IProfileService>();
            profileService.SetupGet(x => x.ActiveProfile).Returns(profile.Object);
            var converter = new FilterPositionToFilterConverter();

            converter.Convert(3, typeof(FilterInfo), profileService.Object, CultureInfo.InvariantCulture).Should().BeSameAs(filters[1]);
            converter.Convert(9, typeof(FilterInfo), profileService.Object, CultureInfo.InvariantCulture).Should().BeSameAs(NullFilter.Instance);
            converter.Convert(-1, typeof(FilterInfo), profileService.Object, CultureInfo.InvariantCulture).Should().BeSameAs(NullFilter.Instance);
            converter.ConvertBack(filters[1], typeof(short), null, CultureInfo.InvariantCulture).Should().Be((short)3);
            converter.ConvertBack(NullFilter.Instance, typeof(short), null, CultureInfo.InvariantCulture).Should().Be(-1);
            converter.ConvertBack(new object(), typeof(short), null, CultureInfo.InvariantCulture).Should().Be(-1);
        }

        /// <summary>
        /// Verifies thumbnail grade converters return the expected resource glyphs for displayed and button states without mutating application resources.
        /// </summary>
        [Test]
        public void ThumbnailGradeConverters_SelectExpectedResourceGeometry() {
            EnsureApplication();
            var dislike = new GeometryGroup();
            var like = new GeometryGroup();
            Application.Current.Resources["DislikeSVG"] = dislike;
            Application.Current.Resources["LikeSVG"] = like;
            var imageConverter = new ThumbnailGradeToImageConverter();
            var buttonConverter = new ThumbnailGradeToButtonImageConverter();

            imageConverter.Convert("BAD", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeSameAs(dislike);
            imageConverter.Convert("", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeNull();
            imageConverter.Convert("UNKNOWN", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeNull();
            buttonConverter.Convert("BAD", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeSameAs(dislike);
            buttonConverter.Convert("", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeSameAs(like);
            buttonConverter.Convert("UNKNOWN", typeof(GeometryGroup), null, CultureInfo.InvariantCulture).Should().BeNull();
        }

        private static void EnsureApplication() {
            if (Application.Current == null) {
                _ = new Application {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }
        }
    }
}
