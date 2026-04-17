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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NINA.Test.Utility {

    [TestFixture]
    public class ObservableLimitedSizedStackTest {

        /// <summary>
        /// Verifies that adding more items than the maximum size evicts the oldest values and notifies observers.
        /// </summary>
        [Test]
        public void Add_ExceedsMaxSize_EvictsOldestAndRaisesNotifications() {
            ObservableLimitedSizedStack<int> stack = new ObservableLimitedSizedStack<int>(3);
            int collectionNotifications = 0;
            int countNotifications = 0;
            stack.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs args) => collectionNotifications++;
            stack.PropertyChanged += (object sender, PropertyChangedEventArgs args) => {
                if (args.PropertyName == nameof(ObservableLimitedSizedStack<int>.Count)) {
                    countNotifications++;
                }
            };

            stack.Add(1);
            stack.Add(2);
            stack.Add(3);
            stack.Add(4);

            Assert.That(stack.ToArray(), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(stack.First().Value, Is.EqualTo(2));
            Assert.That(collectionNotifications, Is.EqualTo(4));
            Assert.That(countNotifications, Is.EqualTo(4));
        }

        /// <summary>
        /// Verifies that reducing MaxSize trims existing values from the front and preserves the most recent entries.
        /// </summary>
        [Test]
        public void MaxSize_SetSmaller_TrimsOldestValues() {
            ObservableLimitedSizedStack<string> stack = new ObservableLimitedSizedStack<string>(5, new[] { "L", "R", "G", "B" });

            stack.MaxSize = 2;

            Assert.That(stack.MaxSize, Is.EqualTo(2));
            Assert.That(stack.ToArray(), Is.EqualTo(new[] { "G", "B" }));
        }

        /// <summary>
        /// Verifies constructor validation when the initial collection cannot fit inside the requested maximum size.
        /// </summary>
        [Test]
        public void Constructor_CollectionExceedsMaxSize_ThrowsException() {
            Assert.Throws<Exception>(() => new ObservableLimitedSizedStack<int>(2, new[] { 1, 2, 3 }));
        }

        /// <summary>
        /// Verifies ICollection operations for contains, copy, remove, clear, and linked-list lookup helpers.
        /// </summary>
        [Test]
        public void CollectionOperations_ValidInputs_ReflectUnderlyingLinkedList() {
            ObservableLimitedSizedStack<int> stack = new ObservableLimitedSizedStack<int>(4, new[] { 1, 2, 3 });
            int[] target = new int[5];

            stack.CopyTo(target, 1);
            bool removed = stack.Remove(2);
            stack.Add(4);
            stack.RemoveLast();

            Assert.That(target, Is.EqualTo(new[] { 0, 1, 2, 3, 0 }));
            Assert.That(removed, Is.True);
            Assert.That(stack.Contains(2), Is.False);
            Assert.That(stack.Find(3).Value, Is.EqualTo(3));
            Assert.That(stack.FindLast(3).Value, Is.EqualTo(3));
            Assert.That(stack.GetLinkedListType(), Is.EqualTo(typeof(LinkedList<int>)));
            Assert.That(stack.LinkedListEquals(stack), Is.False);

            stack.Clear();
            Assert.That(stack.Count, Is.EqualTo(0));
        }
    }
}
