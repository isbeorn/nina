#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections;
using System.Collections.Generic;
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
