#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NINA.Utility {

    public class ObserveAllCollection<T> : AsyncObservableCollection<T> {

        public ObserveAllCollection() : base() {
        }

        public ObserveAllCollection(IEnumerable<T> list) : base(list) {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Add) {
                RegisterPropertyChanged(e.NewItems);
            } else if (e.Action == NotifyCollectionChangedAction.Remove) {
                DeregisterPropertyChanged(e.OldItems);
            } else if (e.Action == NotifyCollectionChangedAction.Replace) {
                DeregisterPropertyChanged(e.OldItems);
                RegisterPropertyChanged(e.NewItems);
            }

            base.OnCollectionChanged(e);
        }

        protected override void ClearItems() {
            DeregisterPropertyChanged(this);
            base.ClearItems();
        }

        private void RegisterPropertyChanged(IList items) {
            foreach (INotifyPropertyChanged item in items) {
                if (item != null) {
                    item.PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        private void DeregisterPropertyChanged(IList items) {
            foreach (INotifyPropertyChanged item in items) {
                if (item != null) {
                    item.PropertyChanged -= new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}