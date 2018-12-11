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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        private int _maxSize;

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
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public int MaxSize {
            get {
                return _maxSize;
            }
            set {
                _maxSize = value;
                while (this._underLyingLinkedList.Count > _maxSize) {
                    _underLyingLinkedList.RemoveFirst();
                }
            }
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
                OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            } finally {
                _lock.ExitWriteLock();
            }
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
            _lock.EnterWriteLock();
            try {
                _underLyingLinkedList.CopyTo(array, index);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public bool LinkedListEquals(object obj) {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.Equals(obj);
            } finally {
                _lock.ExitReadLock();
            }
        }

        public LinkedListNode<T> Find(T value) {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.Find(value);
            } finally {
                _lock.ExitReadLock();
            }
        }

        public LinkedListNode<T> FindLast(T value) {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.FindLast(value);
            } finally {
                _lock.ExitReadLock();
            }
        }

        public Type GetLinkedListType() {
            _lock.EnterReadLock();
            try {
                return _underLyingLinkedList.GetType();
            } finally {
                _lock.ExitReadLock();
            }
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

        public bool Remove(T item) {
            _lock.EnterWriteLock();
            try {
                return _underLyingLinkedList.Remove(item);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _underLyingLinkedList.GetEnumerator();
        }
    }
}