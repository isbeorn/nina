#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NINA.Test.Utility {

    [TestFixture]
    public class ObservableDictionaryTest {

        /// <summary>
        /// Verifies add, indexer update, key/value enumeration, and property-change notifications for binding consumers.
        /// </summary>
        [Test]
        public void AddAndIndexerUpdate_ChangesDictionary_RaisesNotifications() {
            ObservableDictionary<string, int> dictionary = new ObservableDictionary<string, int>();
            int notifications = 0;
            dictionary.PropertyChanged += (object sender, PropertyChangedEventArgs args) => notifications++;

            dictionary.Add("gain", 100);
            dictionary["offset"] = 25;
            dictionary["gain"] = 120;

            Assert.That(dictionary.Count, Is.EqualTo(2));
            Assert.That(dictionary["gain"], Is.EqualTo(120));
            Assert.That(dictionary["offset"], Is.EqualTo(25));
            Assert.That(dictionary.Keys, Is.EquivalentTo(new[] { "gain", "offset" }));
            Assert.That(dictionary.Values, Is.EquivalentTo(new[] { 120, 25 }));
            Assert.That(notifications, Is.EqualTo(3));
        }

        /// <summary>
        /// Verifies that setting the same value is idempotent and does not raise a redundant binding notification.
        /// </summary>
        [Test]
        public void IndexerSet_SameValue_DoesNotRaiseNotification() {
            ObservableDictionary<string, string> dictionary = new ObservableDictionary<string, string>();
            dictionary.Add("filter", "Lum");
            int notifications = 0;
            dictionary.PropertyChanged += (object sender, PropertyChangedEventArgs args) => notifications++;

            dictionary["filter"] = "Lum";

            Assert.That(notifications, Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies null-key and null-value guards so invalid dictionary mutations fail at the API boundary.
        /// </summary>
        [Test]
        public void NullKeyOrValueOperations_ThrowArgumentNullException() {
            ObservableDictionary<string, string> dictionary = new ObservableDictionary<string, string>();

            Assert.Throws<ArgumentNullException>(() => dictionary.Add(null, "Lum"));
            Assert.Throws<ArgumentNullException>(() => dictionary[null] = "Lum");
            Assert.Throws<ArgumentNullException>(() => dictionary["filter"] = null);
            Assert.Throws<ArgumentNullException>(() => dictionary.TryGetValue(null, out string _));
        }

        /// <summary>
        /// Verifies remove and clear only notify when the dictionary actually changes.
        /// </summary>
        [Test]
        public void RemoveAndClear_OnlyNotifyWhenDataChanges() {
            ObservableDictionary<string, int> dictionary = new ObservableDictionary<string, int>();
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            int notifications = 0;
            dictionary.PropertyChanged += (object sender, PropertyChangedEventArgs args) => notifications++;

            bool removedExisting = dictionary.Remove("a");
            bool removedMissing = dictionary.Remove("missing");
            dictionary.Clear();
            dictionary.Clear();

            Assert.That(removedExisting, Is.True);
            Assert.That(removedMissing, Is.False);
            Assert.That(dictionary.Count, Is.EqualTo(0));
            Assert.That(notifications, Is.EqualTo(2));
        }

        /// <summary>
        /// Verifies ICollection copy and contains behavior through the explicit interface implementation used by generic callers.
        /// </summary>
        [Test]
        public void ICollectionContract_CopyToAndContains_ReflectsStoredPairs() {
            ObservableDictionary<string, int> dictionary = new ObservableDictionary<string, int>();
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            ICollection<KeyValuePair<string, int>> collection = dictionary;
            KeyValuePair<string, int>[] target = new KeyValuePair<string, int>[3];

            collection.CopyTo(target, 1);

            Assert.That(collection.Contains(new KeyValuePair<string, int>("a", 1)), Is.True);
            Assert.That(target[0], Is.EqualTo(default(KeyValuePair<string, int>)));
            Assert.That(target[1], Is.EqualTo(new KeyValuePair<string, int>("a", 1)));
            Assert.That(target[2], Is.EqualTo(new KeyValuePair<string, int>("b", 2)));
        }
    }
}
