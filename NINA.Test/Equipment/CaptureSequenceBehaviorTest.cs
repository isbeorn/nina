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
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Model;
using System.Collections.ObjectModel;
using System.Text;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class CaptureSequenceBehaviorTest {

        /// <summary>
        /// Verifies progress and total exposure counts remain internally consistent when either side is changed by the sequencer.
        /// </summary>
        [Test]
        public void CaptureSequence_ProgressAndTotalExposureCountsClampEachOther() {
            var sequence = new CaptureSequence {
                TotalExposureCount = 3
            };

            sequence.ProgressExposureCount = 5;
            sequence.TotalExposureCount.Should().Be(5);

            sequence.TotalExposureCount = 2;
            sequence.ProgressExposureCount.Should().Be(2);
        }

        /// <summary>
        /// Verifies cloning preserves exposure-science options while keeping the original and clone as distinct mutable sequence objects.
        /// </summary>
        [Test]
        public void CaptureSequence_ClonePreservesAcquisitionOptionsWithoutSharingSequenceIdentity() {
            var filter = new FilterInfo("Ha", 12, 3);
            var original = new CaptureSequence(120, CaptureSequence.ImageTypes.LIGHT, filter, new BinningMode(2, 2), 7) {
                Gain = 101,
                Offset = 22,
                Dither = true,
                DitherAmount = 4,
                ProgressExposureCount = 2,
                Enabled = false
            };

            CaptureSequence clone = original.Clone();

            clone.Should().NotBeSameAs(original);
            clone.ExposureTime.Should().Be(120);
            clone.ImageType.Should().Be(CaptureSequence.ImageTypes.LIGHT);
            clone.FilterType.Should().BeSameAs(filter);
            clone.Binning.X.Should().Be(2);
            clone.Binning.Y.Should().Be(2);
            clone.TotalExposureCount.Should().Be(7);
            clone.Gain.Should().Be(101);
            clone.Offset.Should().Be(22);
            clone.Dither.Should().BeTrue();
            clone.DitherAmount.Should().Be(4);
            clone.ProgressExposureCount.Should().Be(0);
            clone.Enabled.Should().BeTrue();
        }

        /// <summary>
        /// Verifies standard sequence mode skips disabled and completed entries while preserving acquisition order.
        /// </summary>
        [Test]
        public void CaptureSequenceList_GetNextSequenceItemStandardSkipsCompletedAndDisabledItems() {
            CaptureSequence first = CreateSequence("L", total: 2, progress: 2, enabled: true);
            CaptureSequence disabled = CreateSequence("R", total: 2, progress: 0, enabled: false);
            CaptureSequence next = CreateSequence("G", total: 2, progress: 0, enabled: true);
            var list = new CaptureSequenceList();
            list.Add(first);
            list.Add(disabled);
            list.Add(next);

            CaptureSequence result = list.GetNextSequenceItem(first);

            result.Should().BeSameAs(next);
            list.Count.Should().Be(2);
        }

        /// <summary>
        /// Verifies rotate mode cycles through enabled sequences, skips disabled entries, and stops only after all enabled entries are complete.
        /// </summary>
        [Test]
        public void CaptureSequenceList_GetNextSequenceItemRotateCyclesEnabledItemsUntilAllComplete() {
            CaptureSequence first = CreateSequence("L", total: 2, progress: 1, enabled: true);
            CaptureSequence disabled = CreateSequence("R", total: 2, progress: 0, enabled: false);
            CaptureSequence second = CreateSequence("G", total: 2, progress: 0, enabled: true);
            var list = new CaptureSequenceList {
                Mode = SequenceMode.ROTATE
            };
            list.Add(first);
            list.Add(disabled);
            list.Add(second);

            list.GetNextSequenceItem(first).Should().BeSameAs(second);

            first.ProgressExposureCount = first.TotalExposureCount;
            second.ProgressExposureCount = second.TotalExposureCount;
            list.GetNextSequenceItem(second).Should().BeNull();
        }

        /// <summary>
        /// Verifies target toggles keep the slew, center, and rotate workflow flags in a valid dependency chain.
        /// </summary>
        [Test]
        public void CaptureSequenceList_TargetWorkflowFlagsMaintainDependencies() {
            var list = new CaptureSequenceList();

            list.RotateTarget = true;
            list.RotateTarget.Should().BeTrue();
            list.CenterTarget.Should().BeTrue();
            list.SlewToTarget.Should().BeTrue();

            list.CenterTarget = false;
            list.RotateTarget.Should().BeFalse();
            list.SlewToTarget.Should().BeTrue();

            list.SlewToTarget = false;
            list.CenterTarget.Should().BeFalse();
            list.RotateTarget.Should().BeFalse();
        }

        /// <summary>
        /// Verifies manual coordinate component setters update the J2000 target coordinates, DSO mirror, and normalized position angle.
        /// </summary>
        [Test]
        public void CaptureSequenceList_CoordinateComponentSettersUpdateCoordinatesAndDsoMirror() {
            var list = new CaptureSequenceList {
                TargetName = "M42",
                PositionAngle = -45
            };

            list.RAHours = 5;
            list.RAMinutes = 35;
            list.RASeconds = 17.3;
            list.NegativeDec = true;
            list.DecDegrees = -5;
            list.DecMinutes = 23;
            list.DecSeconds = 28;

            list.PositionAngle.Should().BeApproximately(315, 1e-10);
            list.Coordinates.RA.Should().BeApproximately(5 + (35 / 60d) + (17.3 / 3600d), 1e-10);
            list.Coordinates.Dec.Should().BeApproximately(-(5 + (23 / 60d) + (28 / 3600d)), 1e-10);
            list.DSO.Name.Should().Be("M42");
            list.DSO.Coordinates.RA.Should().BeApproximately(list.Coordinates.RA, 1e-10);
            list.DSO.RotationPositionAngle.Should().BeApproximately(315, 1e-10);
        }

        /// <summary>
        /// Verifies sequence XML loading migrates legacy DARKFLAT frames and remaps serialized filters by name or slot position.
        /// </summary>
        [Test]
        public void CaptureSequenceList_LoadMigratesDarkFlatAndRemapsFiltersByProfile() {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <CaptureSequenceList TargetName="Target" Mode="STANDARD" RAHours="0" RAMinutes="0" RASeconds="0" DecDegrees="0" DecMinutes="0" DecSeconds="0">
                  <CaptureSequence>
                    <Enabled>true</Enabled>
                    <ExposureTime>30</ExposureTime>
                    <ImageType>DARKFLAT</ImageType>
                    <FilterType>
                      <Name>Old Red</Name>
                      <FocusOffset>0</FocusOffset>
                      <Position>2</Position>
                    </FilterType>
                    <Binning>
                      <X>1</X>
                      <Y>1</Y>
                    </Binning>
                    <TotalExposureCount>1</TotalExposureCount>
                    <ProgressExposureCount>0</ProgressExposureCount>
                    <Gain>-1</Gain>
                    <Offset>-1</Offset>
                    <Dither>false</Dither>
                    <DitherAmount>1</DitherAmount>
                  </CaptureSequence>
                </CaptureSequenceList>
                """;
            var filters = new Collection<FilterInfo> {
                new FilterInfo("L", 0, 0),
                new FilterInfo("R", 10, 2)
            };
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            CaptureSequenceList list = CaptureSequenceList.Load(stream, "legacy.xml", filters, latitude: 48.1, longitude: 11.6);

            list.Should().NotBeNull();
            list.Items.Should().ContainSingle();
            list.Items[0].ImageType.Should().Be(CaptureSequence.ImageTypes.DARK);
            list.Items[0].FilterType.Should().BeSameAs(filters[1]);
        }

        private static CaptureSequence CreateSequence(string filterName, int total, int progress, bool enabled) {
            return new CaptureSequence(60, CaptureSequence.ImageTypes.LIGHT, new FilterInfo(filterName, 0, 0), new BinningMode(1, 1), total) {
                ProgressExposureCount = progress,
                Enabled = enabled
            };
        }
    }
}
