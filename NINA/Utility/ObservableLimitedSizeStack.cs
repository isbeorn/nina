using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {
    public class AsyncObservableLimitedSizedStack<T> : ObservableLimitedSizedStack<T>, INotifyCollectionChanged, IEnumerable {
        private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        public AsyncObservableLimitedSizedStack(int maxSize) : base(maxSize) {
        }

        public AsyncObservableLimitedSizedStack(int maxSize, IEnumerable<T> collection) : base(maxSize, collection) {
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
    }

    public class ObservableLimitedSizedStack<T> : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable {

        private readonly int _maxSize;
        public ObservableLimitedSizedStack(int maxSize) {
            _underLyingLinkedList = new LinkedList<T>();
            _maxSize = maxSize;
        }

        public ObservableLimitedSizedStack(int maxSize, IEnumerable<T> collection) {
            if(collection.Count() > maxSize) {
                throw new Exception("Collection exceeds maximum size");
            }
            _underLyingLinkedList = new LinkedList<T>(collection);
            _maxSize = maxSize;
        }

        public void Add(T item) {
            this._underLyingLinkedList.AddLast(item);

            if (this._underLyingLinkedList.Count > _maxSize)
                this._underLyingLinkedList.RemoveFirst();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private LinkedList<T> _underLyingLinkedList;
        
        public int Count {
            get { return _underLyingLinkedList.Count; }
        }

        public void Clear() {
            _underLyingLinkedList.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool Contains(T value) {
            return _underLyingLinkedList.Contains(value);
        }

        public void CopyTo(T[] array, int index) {
            _underLyingLinkedList.CopyTo(array, index);
        }

        public bool LinkedListEquals(object obj) {
            return _underLyingLinkedList.Equals(obj);
        }

        public LinkedListNode<T> Find(T value) {
            return _underLyingLinkedList.Find(value);
        }

        public LinkedListNode<T> FindLast(T value) {
            return _underLyingLinkedList.FindLast(value);
        }

        public Type GetLinkedListType() {
            return _underLyingLinkedList.GetType();
        }
        
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            this.CollectionChanged?.Invoke(this, e);
            OnPropertyChanged(nameof(Count));
        }

        private void OnPropertyChanged(string propertyname) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyname));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action) {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            this.PropertyChanged?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        

        IEnumerator IEnumerable.GetEnumerator() {
            return (_underLyingLinkedList as IEnumerable).GetEnumerator();
        }
        
    }
}
