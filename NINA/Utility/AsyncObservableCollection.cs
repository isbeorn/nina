#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Utility {

    public class AsyncObservableCollection<T> : ObservableCollection<T> {

        private SynchronizationContext _synchronizationContext =
            Application.Current?.Dispatcher != null
            ? new DispatcherSynchronizationContext(Application.Current.Dispatcher)
            : null;

        public AsyncObservableCollection() {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list) {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (SynchronizationContext.Current == _synchronizationContext) {
                // Execute the CollectionChanged event on the current thread
                RaiseCollectionChanged(e);
            } else {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object param) {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (SynchronizationContext.Current == _synchronizationContext) {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            } else {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object param) {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
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