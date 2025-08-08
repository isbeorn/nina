#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Core.Utility {

    public class AsyncObservableCollection<T> : ObservableCollection<T> {

        private readonly SynchronizationContext _synchronizationContext =
            Application.Current?.Dispatcher != null
            ? new DispatcherSynchronizationContext(Application.Current.Dispatcher)
            : null;

        public AsyncObservableCollection() {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list) {
        }

        protected void RunOnSynchronizationContext(Action action) {
            if (SynchronizationContext.Current == _synchronizationContext) {
                action();
            } else {
                _synchronizationContext.Send(_ => action(), null);
            }
        }

        protected override void InsertItem(int index, T item) {
            RunOnSynchronizationContext(() => base.InsertItem(index, item));
        }

        protected override void RemoveItem(int index) {
            RunOnSynchronizationContext(() => base.RemoveItem(index));
        }

        protected override void SetItem(int index, T item) {
            RunOnSynchronizationContext(() => base.SetItem(index, item));
        }

        protected override void MoveItem(int oldIndex, int newIndex) {
            RunOnSynchronizationContext(() => base.MoveItem(oldIndex, newIndex));
        }

        protected override void ClearItems() {
            RunOnSynchronizationContext(() => base.ClearItems());
        }

        public void AddSorted(T item, IComparer<T> comparer = null) {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;
            while (i < this.Count && comparer.Compare(this[i], item) < 0)
                i++;

            this.Insert(i, item);
        }
    }
}