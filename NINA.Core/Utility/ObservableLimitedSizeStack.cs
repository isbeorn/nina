#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

namespace NINA.Core.Utility {

    public class AsyncObservableLimitedSizedStack<T> : ObservableLimitedSizedStack<T>, INotifyCollectionChanged, IEnumerable {

        private static SynchronizationContext _synchronizationContext =
            Application.Current?.Dispatcher != null
            ? new DispatcherSynchronizationContext(Application.Current.Dispatcher)
            : null;

        public AsyncObservableLimitedSizedStack(int maxSize) : base(maxSize) {
        }

        public AsyncObservableLimitedSizedStack(int maxSize, IEnumerable<T> collection) : base(maxSize, collection) {
        }

        private void RunOnSynchronizationContext(Action action) {
            if (SynchronizationContext.Current == _synchronizationContext) {
                action();
            } else {
                _synchronizationContext.Send(_ => action(), null);
            }
        }

        protected override void ClearItems() {
            RunOnSynchronizationContext(() => base.ClearItems());
        }

        protected override void AddItem(T item) {
            RunOnSynchronizationContext(() => base.AddItem(item));
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

        public virtual void Add(T item) {
            _lock.EnterWriteLock();
            try {
                AddItem(item);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        protected virtual void AddItem(T item) {
            this._underLyingLinkedList.AddLast(item);

            if (this._underLyingLinkedList.Count > _maxSize)
                this._underLyingLinkedList.RemoveFirst();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public int MaxSize {
            get {
                _lock.EnterReadLock();
                try {
                    return _maxSize;
                } finally {
                    _lock.ExitReadLock();
                }
            }
            set {
                _lock.EnterWriteLock();
                try {
                    _maxSize = value;
                    while (this._underLyingLinkedList.Count > _maxSize) {
                        _underLyingLinkedList.RemoveFirst();
                    }
                } finally {
                    _lock.ExitWriteLock();
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

        public bool IsReadOnly => false;

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
                ClearItems();
            } finally {
                _lock.ExitWriteLock();
            }
        }

        protected virtual void ClearItems() {
            _underLyingLinkedList.Clear();
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
            var handler = CollectionChanged;
            handler?.Invoke(this, e);
            OnPropertyChanged(nameof(Count));
        }

        private void OnPropertyChanged(string propertyname) {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action) {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            var handler = PropertyChanged;
            handler?.Invoke(this, e);
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

        public void RemoveLast() {
            _lock.EnterWriteLock();
            try {
                _underLyingLinkedList.RemoveLast();
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _underLyingLinkedList.GetEnumerator();
        }
    }
}