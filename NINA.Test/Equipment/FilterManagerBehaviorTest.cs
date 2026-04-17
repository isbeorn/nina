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
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Utility;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class FilterManagerBehaviorTest {

        /// <summary>
        /// Verifies filter-wheel profile synchronization removes duplicate slots, inserts missing slots, and trims entries beyond the physical wheel size.
        /// </summary>
        [Test]
        public void SyncFiltersWithPositions_RepairsDuplicateMissingAndExcessFilterSlots() {
            var filters = new ObserveAllCollection<FilterInfo> {
                new FilterInfo("L", 0, 0),
                new FilterInfo("Duplicate L", 10, 0),
                new FilterInfo("OIII", 20, 2),
                new FilterInfo("Too Far", 30, 5)
            };
            var sut = new FilterManager();

            ObserveAllCollection<FilterInfo> result = sut.SyncFiltersWithPositions(filters, wheelPositions: 4);

            result.Should().BeSameAs(filters);
            result.Select(x => x.Position).Should().Equal(0, 1, 2, 3);
            result.Select(x => x.Name).Should().Equal("Slot 0", "Slot 1", "OIII", "Slot 3");
        }

        /// <summary>
        /// Verifies an empty profile receives one deterministic placeholder filter per physical filter-wheel position.
        /// </summary>
        [Test]
        public void SyncFiltersWithPositions_EmptyProfileAddsSlotPlaceholders() {
            var filters = new ObserveAllCollection<FilterInfo>();
            var sut = new FilterManager();

            sut.SyncFiltersWithPositions(filters, wheelPositions: 3);

            filters.Select(x => x.Position).Should().Equal(0, 1, 2);
            filters.Select(x => x.Name).Should().Equal("Slot 0", "Slot 1", "Slot 2");
            filters.Should().OnlyContain(x => x.FocusOffset == 0);
        }
    }
}
