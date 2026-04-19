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
using NINA.Core.Utility;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NINA.Test.Model {

    [TestFixture]
    public class PagedListTest {

        /// <summary>
        /// Verifies that construction loads the first page and exposes stable one-based page metadata for a partial final page.
        /// </summary>
        [Test]
        public void Constructor_ItemsSpanPartialFinalPage_LoadsFirstPageAndMetadata() {
            PagedList<int> pagedList = new PagedList<int>(3, Enumerable.Range(1, 8));

            Assert.That(pagedList.Count, Is.EqualTo(8));
            Assert.That(pagedList.Pages, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(pagedList.CurrentPage, Is.EqualTo(1));
            Assert.That(pagedList.PageStartIndex, Is.EqualTo(1));
            Assert.That(pagedList.PageEndIndex, Is.EqualTo(3));
            Assert.That(pagedList.ItemPage, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        /// <summary>
        /// Verifies forward and backward command navigation, including the final page range where fewer items remain.
        /// </summary>
        [Test]
        public async Task NavigationCommands_MoveAcrossPagesAndRespectBoundaries() {
            PagedList<string> pagedList = new PagedList<string>(2, new[] { "L", "R", "G", "B", "Ha" });

            await ((IAsyncCommand)pagedList.NextPageCommand).ExecuteAsync(null);
            Assert.That(pagedList.CurrentPage, Is.EqualTo(2));
            Assert.That(pagedList.ItemPage, Is.EqualTo(new[] { "G", "B" }));
            Assert.That(pagedList.PageStartIndex, Is.EqualTo(3));
            Assert.That(pagedList.PageEndIndex, Is.EqualTo(4));

            await ((IAsyncCommand)pagedList.LastPageCommand).ExecuteAsync(null);
            Assert.That(pagedList.CurrentPage, Is.EqualTo(3));
            Assert.That(pagedList.ItemPage, Is.EqualTo(new[] { "Ha" }));
            Assert.That(pagedList.PageStartIndex, Is.EqualTo(5));
            Assert.That(pagedList.PageEndIndex, Is.EqualTo(5));
            Assert.That(pagedList.NextPageCommand.CanExecute(null), Is.False);
            Assert.That(pagedList.LastPageCommand.CanExecute(null), Is.False);

            await ((IAsyncCommand)pagedList.PrevPageCommand).ExecuteAsync(null);
            Assert.That(pagedList.CurrentPage, Is.EqualTo(2));
            Assert.That(pagedList.ItemPage, Is.EqualTo(new[] { "G", "B" }));
        }

        /// <summary>
        /// Verifies that an empty source has no page range and that commands remain non-executable at the boundaries.
        /// </summary>
        [Test]
        public void Constructor_EmptyItems_ExposesEmptyPageWithoutInvalidIndices() {
            PagedList<int> pagedList = new PagedList<int>(5, Enumerable.Empty<int>());

            Assert.That(pagedList.Count, Is.EqualTo(0));
            Assert.That(pagedList.Pages, Is.Empty);
            Assert.That(pagedList.ItemPage, Is.Empty);
            Assert.That(pagedList.CurrentPage, Is.EqualTo(0));
            Assert.That(pagedList.PageStartIndex, Is.EqualTo(0));
            Assert.That(pagedList.PageEndIndex, Is.EqualTo(0));
            Assert.That(pagedList.FirstPageCommand.CanExecute(null), Is.False);
            Assert.That(pagedList.PrevPageCommand.CanExecute(null), Is.False);
            Assert.That(pagedList.NextPageCommand.CanExecute(null), Is.False);
            Assert.That(pagedList.LastPageCommand.CanExecute(null), Is.False);
        }

        /// <summary>
        /// Verifies that selected item changes raise the specific property notification expected by binding consumers.
        /// </summary>
        [Test]
        public void SelectedItem_Set_RaisesPropertyChanged() {
            PagedList<string> pagedList = new PagedList<string>(2, new[] { "Lum", "Red" });
            List<string> changedProperties = new List<string>();
            pagedList.PropertyChanged += (object sender, PropertyChangedEventArgs args) => changedProperties.Add(args.PropertyName);

            pagedList.SelectedItem = "Red";

            Assert.That(pagedList.SelectedItem, Is.EqualTo("Red"));
            Assert.That(changedProperties, Does.Contain(nameof(PagedList<string>.SelectedItem)));
        }
    }
}
