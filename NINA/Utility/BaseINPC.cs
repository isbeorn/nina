using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NINA.Utility {

    [System.Serializable()]
    public abstract class BaseINPC : INotifyPropertyChanged {

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