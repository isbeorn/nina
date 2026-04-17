#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;

namespace NINA.Test.Model {

    [TestFixture]
    public class CoreModelBehaviorTest {

        /// <summary>
        /// Verifies BinningMode string formatting, value equality, and hash-code behavior used by lists and dictionaries.
        /// </summary>
        [Test]
        public void BinningMode_ValueObjectMembers_UseXAndYOnly() {
            BinningMode oneByTwo = new BinningMode(1, 2);
            BinningMode same = new BinningMode(1, 2);
            BinningMode different = new BinningMode(2, 2);

            Assert.That(oneByTwo.Name, Is.EqualTo("1x2"));
            Assert.That(oneByTwo.ToString(), Is.EqualTo("1x2"));
            Assert.That(oneByTwo, Is.EqualTo(same));
            Assert.That(oneByTwo.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(oneByTwo, Is.Not.EqualTo(different));
            Assert.That(oneByTwo.Equals(null), Is.False);
        }

        /// <summary>
        /// Verifies BinningMode parsing accepts the canonical persisted form and rejects malformed or ambiguous values.
        /// </summary>
        [Test]
        [TestCase("1x1", true, 1, 1)]
        [TestCase("2x3", true, 2, 3)]
        [TestCase("", false, 0, 0)]
        [TestCase("2", false, 0, 0)]
        [TestCase("2x", false, 0, 0)]
        [TestCase("2x3x4", false, 0, 0)]
        [TestCase("2X3", false, 0, 0)]
        public void TryParse_RepresentativeInputs_ReturnsExpectedMode(string value, bool expectedSuccess, short expectedX, short expectedY) {
            bool success = BinningMode.TryParse(value, out BinningMode mode);

            Assert.That(success, Is.EqualTo(expectedSuccess));
            if (expectedSuccess) {
                Assert.That(mode.X, Is.EqualTo(expectedX));
                Assert.That(mode.Y, Is.EqualTo(expectedY));
            } else {
                Assert.That(mode, Is.Null);
            }
        }

        /// <summary>
        /// Verifies BinningMode parsing rejects null input and leaves the out parameter empty.
        /// </summary>
        [Test]
        public void TryParse_NullInput_ReturnsFalseAndNullMode() {
            bool success = BinningMode.TryParse(null, out BinningMode mode);

            Assert.That(success, Is.False);
            Assert.That(mode, Is.Null);
        }

        /// <summary>
        /// Verifies ApplicationStatus default progress values and per-property notifications used by progress UI bindings.
        /// </summary>
        [Test]
        public void ApplicationStatus_SetProperties_RaisesExpectedNotifications() {
            ApplicationStatus status = new ApplicationStatus();
            List<string> changedProperties = new List<string>();
            status.PropertyChanged += (object sender, PropertyChangedEventArgs args) => changedProperties.Add(args.PropertyName);

            status.Source = "Capture";
            status.Status = "Exposing";
            status.Progress = 0.5;
            status.MaxProgress = 2;
            status.ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
            status.Status2 = "Dither";
            status.Progress2 = 0.25;
            status.MaxProgress2 = 4;
            status.ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
            status.Status3 = "Download";
            status.Progress3 = 0.75;
            status.MaxProgress3 = 8;
            status.ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;

            Assert.That(status.Source, Is.EqualTo("Capture"));
            Assert.That(status.Status, Is.EqualTo("Exposing"));
            Assert.That(status.Progress, Is.EqualTo(0.5));
            Assert.That(status.MaxProgress, Is.EqualTo(2));
            Assert.That(status.ProgressType, Is.EqualTo(ApplicationStatus.StatusProgressType.ValueOfMaxValue));
            Assert.That(status.Status2, Is.EqualTo("Dither"));
            Assert.That(status.Progress2, Is.EqualTo(0.25));
            Assert.That(status.MaxProgress2, Is.EqualTo(4));
            Assert.That(status.ProgressType2, Is.EqualTo(ApplicationStatus.StatusProgressType.ValueOfMaxValue));
            Assert.That(status.Status3, Is.EqualTo("Download"));
            Assert.That(status.Progress3, Is.EqualTo(0.75));
            Assert.That(status.MaxProgress3, Is.EqualTo(8));
            Assert.That(status.ProgressType3, Is.EqualTo(ApplicationStatus.StatusProgressType.ValueOfMaxValue));
            Assert.That(changedProperties, Does.Contain(nameof(ApplicationStatus.Source)));
            Assert.That(changedProperties, Does.Contain(nameof(ApplicationStatus.Status3)));
            Assert.That(changedProperties, Does.Contain(nameof(ApplicationStatus.ProgressType3)));
        }

        /// <summary>
        /// Verifies RMS removal keeps variance mathematically consistent for the remaining population.
        /// </summary>
        [Test]
        public void RMS_RemoveDataPoint_TwoPointPopulationLeavesSinglePointWithZeroVariance() {
            RMS rms = new RMS();
            rms.AddDataPoint(10, 20);
            rms.AddDataPoint(30, 60);

            rms.RemoveDataPoint(10, 20);

            Assert.That(rms.DataPoints, Is.EqualTo(1));
            Assert.That(rms.RA, Is.EqualTo(0d).Within(1e-9));
            Assert.That(rms.Dec, Is.EqualTo(0d).Within(1e-9));
            Assert.That(rms.Total, Is.EqualTo(0d).Within(1e-9));
        }

        /// <summary>
        /// Verifies RMS clear resets accumulated variance and peaks while preserving the configured arcsecond scale.
        /// </summary>
        [Test]
        public void RMS_Clear_AfterScaledDataPoints_ResetsStatisticsButKeepsScale() {
            RMS rms = new RMS();
            rms.SetScale(1.75);
            rms.AddDataPoint(-4, 9);
            rms.AddDataPoint(12, -16);

            rms.Clear();

            Assert.That(rms.Scale, Is.EqualTo(1.75));
            Assert.That(rms.DataPoints, Is.EqualTo(0));
            Assert.That(rms.RA, Is.EqualTo(0));
            Assert.That(rms.Dec, Is.EqualTo(0));
            Assert.That(rms.Total, Is.EqualTo(0));
            Assert.That(rms.PeakRA, Is.EqualTo(0));
            Assert.That(rms.PeakDec, Is.EqualTo(0));
        }
    }
}
