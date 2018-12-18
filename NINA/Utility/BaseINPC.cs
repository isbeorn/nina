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

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NINA.Utility {

    [System.Serializable()]
    public abstract class BaseINPC : INotifyPropertyChanged {
        [field: System.NonSerialized]
        ConcurrentDictionary<string, Timer> delayedTimers = new ConcurrentDictionary<string, Timer>();

        protected void DelayedPropertyChanged([CallerMemberName] string propertyName = null, TimeSpan delay = default(TimeSpan)) {
            if (delay == default(TimeSpan)) {
                delay = TimeSpan.FromMilliseconds(500);
            }
            if (delayedTimers == null) {
                delayedTimers = new ConcurrentDictionary<string, Timer>();
            }

            if (!delayedTimers.ContainsKey(propertyName)) {
                var timer = new Timer(new TimerCallback((x) => TimedPropertyChanged(propertyName)), new object(), delay.Milliseconds, Timeout.Infinite);
                delayedTimers.TryAdd(propertyName, timer);
            }

        }

        private void TimedPropertyChanged([CallerMemberName] string propertyName = null) {
            lock (delayedTimers) {
                if (delayedTimers.ContainsKey(propertyName)) {
                    delayedTimers[propertyName].Change(Timeout.Infinite, Timeout.Infinite);
                    Timer t;
                    delayedTimers.TryRemove(propertyName, out t);
                    RaisePropertyChanged(propertyName);
                }
            }
        }

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
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= new
                                           PropertyChangedEventHandler(Item_PropertyChanged);
            }
            if (e.NewItems != null) {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged +=
                                       new PropertyChangedEventHandler(Item_PropertyChanged);
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