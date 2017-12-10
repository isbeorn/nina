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
using System.Windows;
using System.Windows.Threading;

namespace NINA.Utility {
    public class AsyncObservableLimitedSizedStack<T> : ObservableLimitedSizedStack<T>, INotifyCollectionChanged, IEnumerable {
        private SynchronizationContext _synchronizationContext = new DispatcherSynchronizationContext(
                    Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);

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

    public class ObservableLimitedSizedStack<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable {

        private readonly int _maxSize;
        public ObservableLimitedSizedStack(int maxSize) {
            _underLyingLinkedList = new LinkedList<T>();
            _maxSize = maxSize;
        }

        public ObservableLimitedSizedStack(int maxSize, IEnumerable<T> collection) {
            if (collection.Count() > maxSize) {
                throw new Exception("Collection exceeds maximum size");
            }
            _underLyingLinkedList = new LinkedList<T>(collection);
            _maxSize = maxSize;
        }

        protected ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void Add(T item) {
            _lock.EnterWriteLock();
            try {


                this._underLyingLinkedList.AddLast(item);

                if (this._underLyingLinkedList.Count > _maxSize)
                    this._underLyingLinkedList.RemoveFirst();
            } finally {
                _lock.ExitWriteLock();
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private LinkedList<T> _underLyingLinkedList;

        public int Count {
            get {
                _lock.EnterReadLock();
                try {
                    return _underLyingLinkedList.Count;
                } finally {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public LinkedListNode<T> First() {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.First;
            } finally {
                _lock.ExitReadLock();
            }            
        }

        public void Clear() {
            _lock.EnterWriteLock();
            try {
                _underLyingLinkedList.Clear();
            } finally {
                _lock.EnterWriteLock();
            }            
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool Contains(T value) {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.Contains(value);
            } finally {
                _lock.ExitReadLock();
            }            
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
            _lock.EnterReadLock();
            try {
                this.CollectionChanged?.Invoke(this, e);
                OnPropertyChanged(nameof(Count));
            } finally {
                _lock.ExitReadLock();
            }
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

        public bool Remove(T item) {
            _lock.EnterWriteLock();
            try {
                return _underLyingLinkedList.Remove(item);
            } finally {
                _lock.EnterWriteLock();
            }            
        }

        public IEnumerator<T> GetEnumerator() {
            return _underLyingLinkedList.GetEnumerator();
        }
    }
}
