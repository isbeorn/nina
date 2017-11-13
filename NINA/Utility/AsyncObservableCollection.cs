using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Utility {
    public class AsyncObservableCollection<T> : ObservableCollection<T> {

        public AsyncObservableCollection() {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list) {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() => {
                    RaiseCollectionChanged(e);
                })
            );
        }

        private void RaiseCollectionChanged(object param) {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() => {
                    RaisePropertyChanged(e);
                })
            );
        }

        private void RaisePropertyChanged(object param) {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
        }

        public void AddSorted(T item,IComparer<T> comparer = null) {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;
            while (i < this.Count && comparer.Compare(this[i],item) < 0)
                i++;

            this.Insert(i,item);
        }
    }
}
