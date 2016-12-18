using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility
{
    public abstract class BaseINPC : INotifyPropertyChanged
    {
        
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("IsChanged");
        }

        protected void items_CollectionChanged(object sender,
               System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= new
                                           PropertyChangedEventHandler(item_PropertyChanged);
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged +=
                                       new PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        protected void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("IsChanged");
        }
    }
}
