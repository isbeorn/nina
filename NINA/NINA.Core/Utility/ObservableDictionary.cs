using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
 
 
namespace NINA.Core.Utility {
    /// <summary>
    ///     Based on https://referencesource.microsoft.com/#PresentationFramework/src/Framework/MS/Internal/Annotations/ObservableDictionary.cs
    /// </summary>
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyPropertyChanged {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates a ContentLocatorPart with the specified type name and namespace.
        /// </summary>
        public ObservableDictionary() {
            _nameValues = new Dictionary<TKey, TValue>();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Adds a key/value pair to the ContentLocatorPart.  If a value for the key already
        ///     exists, the old value is overwritten by the new value.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="val">value</param>
        /// <exception cref="ArgumentNullException">key is null</exception>
        /// <exception cref="ArgumentException">a value for key is already present in the locator part</exception>
        public void Add(TKey key, TValue val) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }
            _nameValues.Add(key, val);
            FireDictionaryChanged();
        }

        /// <summary>
        ///     Removes all name/value pairs from the ContentLocatorPart.
        /// </summary>
        public void Clear() {
            int count = _nameValues.Count;

            if (count > 0) {
                _nameValues.Clear();

                // Only fire changed event if the dictionary actually changed
                FireDictionaryChanged();
            }
        }

        /// <summary>
        ///     Returns whether or not a value of the key exists in this ContentLocatorPart.
        /// </summary>
        /// <param name="key">the key to check for</param>
        /// <returns>true - yes, false - no</returns>
        public bool ContainsKey(TKey key) {
            return _nameValues.ContainsKey(key);
        }

        /// <summary>
        ///     Removes the key and its value from the ContentLocatorPart.
        /// </summary>
        /// <param name="key">key to be removed</param>
        /// <returns>true - the key was found in the ContentLocatorPart, false o- it wasn't</returns>
        public bool Remove(TKey key) {
            bool exists = _nameValues.Remove(key);

            // Only fire changed event if the key was actually removed
            if (exists) {
                FireDictionaryChanged();
            }

            return exists;
        }

        /// <summary>
        ///     Returns an enumerator for the key/value pairs in this ContentLocatorPart.
        /// </summary>
        /// <returns>an enumerator for the key/value pairs; never returns null</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return _nameValues.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator forthe key/value pairs in this ContentLocatorPart.
        /// </summary>
        /// <returns>an enumerator for the key/value pairs; never returns null</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)_nameValues).GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">key is null</exception>
        public bool TryGetValue(TKey key, out TValue value) {
            if (key == null)
                throw new ArgumentNullException("key");

            return _nameValues.TryGetValue(key, out value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pair"></param>
        /// <exception cref="ArgumentNullException">pair is null</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair) {
            ((ICollection<KeyValuePair<TKey, TValue>>)_nameValues).Add(pair);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">pair is null</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair) {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_nameValues).Contains(pair);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">pair is null</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair) {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_nameValues).Remove(pair);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="startIndex"></param>
        /// <exception cref="ArgumentNullException">target is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex is less than zero or greater than the lenght of target</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] target, int startIndex) {
            if (target == null)
                throw new ArgumentNullException("target");
            if (startIndex < 0 || startIndex > target.Length)
                throw new ArgumentOutOfRangeException("startIndex");

            ((ICollection<KeyValuePair<TKey, TValue>>)_nameValues).CopyTo(target, startIndex);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The number of name/value pairs in this ContentLocatorPart.
        /// </summary>
        /// <value>count of name/value pairs</value>
        public int Count => _nameValues.Count;

        /// <summary>
        ///     Indexer provides lookup of values by key.  Gets or sets the value
        ///     in the ContentLocatorPart for the specified key.  If the key does not exist
        ///     in the ContentLocatorPart,
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>the value stored in this locator part for key</returns>
        public TValue this[TKey key] {
            get {
                if (key == null) {
                    throw new ArgumentNullException("key");
                }

                TValue value = default(TValue);
                _nameValues.TryGetValue(key, out value);
                return value;
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException("key");
                }

                if (value == null) {
                    throw new ArgumentNullException("value");
                }

                TValue oldValue = default(TValue);
                _nameValues.TryGetValue(key, out oldValue);

                // If the new value is actually different, then we add it and fire
                // a change notification
                if ((oldValue == null) || !EqualityComparer<TValue>.Default.Equals(oldValue, value)) {
                    _nameValues[key] = value;
                    FireDictionaryChanged();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        ///     Returns a collection of all the keys in this ContentLocatorPart.
        /// </summary>
        /// <value>keys</value>
        public ICollection<TKey> Keys => _nameValues.Keys;

        /// <summary>
        ///     Returns a collection of all the values in this ContentLocatorPart.
        /// </summary>
        /// <value>values</value>
        public ICollection<TValue> Values => _nameValues.Values;

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Operators
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        ///
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Notify the owner this ContentLocatorPart has changed.
        /// </summary>
        private void FireDictionaryChanged() {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(null));
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     The internal data structure.
        /// </summary>
        private Dictionary<TKey, TValue> _nameValues;

        #endregion Private Fields
    }
}