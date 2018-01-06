using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
    public class ObserveAllCollection<T> : ObservableCollection<T> {
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
