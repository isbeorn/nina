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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NINA.Test.Utility {

    [TestFixture]
    public class AsyncObservableCollectionTest {

        /// <summary>
        /// Verifies AddSorted uses the default comparer and inserts values without requiring callers to sort the collection afterward.
        /// </summary>
        [Test]
        public void AddSorted_DefaultComparer_InsertsItemsInAscendingOrder() {
            AsyncObservableCollection<int> collection = new AsyncObservableCollection<int>();

            collection.AddSorted(30);
            collection.AddSorted(10);
            collection.AddSorted(20);

            Assert.That(collection.ToArray(), Is.EqualTo(new[] { 10, 20, 30 }));
        }

        /// <summary>
        /// Verifies AddSorted accepts a custom comparer for descending or domain-specific ordering.
        /// </summary>
        [Test]
        public void AddSorted_CustomComparer_InsertsItemsUsingProvidedOrdering() {
            AsyncObservableCollection<string> collection = new AsyncObservableCollection<string>();
            IComparer<string> descendingComparer = Comparer<string>.Create((string left, string right) => string.CompareOrdinal(right, left));

            collection.AddSorted("B", descendingComparer);
            collection.AddSorted("A", descendingComparer);
            collection.AddSorted("C", descendingComparer);

            Assert.That(collection.ToArray(), Is.EqualTo(new[] { "C", "B", "A" }));
        }

        /// <summary>
        /// Verifies ObserveAllCollection raises a reset notification when an item already in the collection changes.
        /// </summary>
        [Test]
        public void ObserveAllCollection_ItemPropertyChanged_RaisesResetNotification() {
            ObservableItem item = new ObservableItem();
            ObserveAllCollection<ObservableItem> collection = new ObserveAllCollection<ObservableItem>();
            List<NotifyCollectionChangedAction> actions = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs args) => actions.Add(args.Action);

            collection.Add(item);
            item.Value = 5;

            Assert.That(actions, Does.Contain(NotifyCollectionChangedAction.Add));
            Assert.That(actions, Does.Contain(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Verifies ObserveAllCollection deregisters removed items so later item changes do not reset the collection.
        /// </summary>
        [Test]
        public void ObserveAllCollection_RemovedItemPropertyChanged_DoesNotRaiseResetNotification() {
            ObservableItem item = new ObservableItem();
            ObserveAllCollection<ObservableItem> collection = new ObserveAllCollection<ObservableItem> { item };
            List<NotifyCollectionChangedAction> actions = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs args) => actions.Add(args.Action);

            collection.Remove(item);
            item.Value = 7;

            Assert.That(actions, Is.EqualTo(new[] { NotifyCollectionChangedAction.Remove }));
        }

        private sealed class ObservableItem : INotifyPropertyChanged {
            private int value;

            public int Value {
                get => value;
                set {
                    this.value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
