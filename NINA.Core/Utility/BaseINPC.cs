#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace NINA.Core.Utility {

    public abstract class BaseINPC : ObservableObject {

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void ChildChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged("IsChanged");
        }

        protected void Items_CollectionChanged(object sender,
               System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (INotifyPropertyChanged item in e.OldItems) {
                    item.PropertyChanged -= new
                                           PropertyChangedEventHandler(Item_PropertyChanged);
                }
            }
            if (e.NewItems != null) {
                foreach (INotifyPropertyChanged item in e.NewItems) {
                    item.PropertyChanged +=
                                       new PropertyChangedEventHandler(Item_PropertyChanged);
                }
            }
        }

        protected void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged("IsChanged");
        }

        protected void RaiseAllPropertiesChanged() {
            OnPropertyChanged(new PropertyChangedEventArgs(null));
        }
    }

    [System.Serializable()]
    [DataContract]
    [Obsolete($"This class is used for migration purposes when serialization attribute is required, which is not compatible with ObservableObject")]
    public abstract class SerializableINPC : INotifyPropertyChanged {

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: System.NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void ChildChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged("IsChanged");
        }

        protected void Items_CollectionChanged(object sender,
               System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (INotifyPropertyChanged item in e.OldItems) {
                    item.PropertyChanged -= new
                                           PropertyChangedEventHandler(Item_PropertyChanged);
                }
            }
            if (e.NewItems != null) {
                foreach (INotifyPropertyChanged item in e.NewItems) {
                    item.PropertyChanged +=
                                       new PropertyChangedEventHandler(Item_PropertyChanged);
                }
            }
        }

        protected void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged("IsChanged");
        }

        protected void RaiseAllPropertiesChanged() {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}