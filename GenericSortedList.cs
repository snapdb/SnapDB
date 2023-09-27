#region Assembly System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using SnapDB.Snap.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Generic
{
    //
    // Summary:
    //     Represents a collection of key/value pairs that are sorted by key based on the
    //     associated System.Collections.Generic.IComparer`1 implementation.
    //
    // Type parameters:
    //   TKey:
    //     The type of keys in the collection.
    //
    //   TValue:
    //     The type of values in the collection.
    [Serializable]
    [DebuggerTypeProxy(typeof(System_DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [ComVisible(false)]
    [__DynamicallyInvokable]
    public class GenericSortedList<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        [Serializable]
        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
        {
            private GenericSortedList<TKey, TValue> _sortedList;

            private TKey key;

            private TValue value;

            private int index;

            private int version;

            private int getEnumeratorRetType;

            internal const int KeyValuePair = 1;

            internal const int DictEntry = 2;

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return key;
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return new DictionaryEntry(key, value);
                }
            }

            public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(key, value);

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    if (getEnumeratorRetType == 2)
                    {
                        return new DictionaryEntry(key, value);
                    }

                    return new KeyValuePair<TKey, TValue>(key, value);
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return value;
                }
            }

            internal Enumerator(SortedList<TKey, TValue> sortedList, int getEnumeratorRetType)
            {
                _sortedList = sortedList;
                index = 0;
                version = _sortedList.version;
                this.getEnumeratorRetType = getEnumeratorRetType;
                key = default(TKey);
                value = default(TValue);
            }

            public void Dispose()
            {
                index = 0;
                key = default(TKey);
                value = default(TValue);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                if ((uint)index < (uint)_sortedList.Count)
                {
                    key = _sortedList.keys[index];
                    value = _sortedList.values[index];
                    index++;
                    return true;
                }

                index = _sortedList.Count + 1;
                key = default(TKey);
                value = default(TValue);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                key = default(TKey);
                value = default(TValue);
            }
        }

        [Serializable]
        private sealed class SortedListKeyEnumerator : IEnumerator<TKey>, IDisposable, IEnumerator
        {
            private SortedList<TKey, TValue> _sortedList;

            private int index;

            private int version;

            private TKey currentKey;

            public TKey Current => currentKey;

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return currentKey;
                }
            }

            internal SortedListKeyEnumerator(SortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                version = sortedList.version;
            }

            public void Dispose()
            {
                index = 0;
                currentKey = default(TKey);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                if ((uint)index < (uint)_sortedList.Count)
                {
                    currentKey = _sortedList.keys[index];
                    index++;
                    return true;
                }

                index = _sortedList.Count + 1;
                currentKey = default(TKey);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                currentKey = default(TKey);
            }
        }

        [Serializable]
        private sealed class SortedListValueEnumerator : IEnumerator<TValue>, IDisposable, IEnumerator
        {
            private SortedList<TKey, TValue> _sortedList;

            private int index;

            private int version;

            private TValue currentValue;

            public TValue Current => currentValue;

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }

                    return currentValue;
                }
            }

            internal SortedListValueEnumerator(SortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                version = sortedList.version;
            }

            public void Dispose()
            {
                index = 0;
                currentValue = default(TValue);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                if ((uint)index < (uint)_sortedList.Count)
                {
                    currentValue = _sortedList.values[index];
                    index++;
                    return true;
                }

                index = _sortedList.Count + 1;
                currentValue = default(TValue);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                currentValue = default(TValue);
            }
        }

        [Serializable]
        [DebuggerTypeProxy(typeof(System_DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        private sealed class KeyList : IList<TKey>, ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection
        {
            private SortedList<TKey, TValue> _dict;

            public int Count => _dict._size;

            public bool IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dict).SyncRoot;

            public TKey this[int index]
            {
                get
                {
                    return _dict.GetKey(index);
                }
                set
                {
                    ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
                }
            }

            internal KeyList(SortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            public void Add(TKey key)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public void Clear()
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public bool Contains(TKey key)
            {
                return _dict.ContainsKey(key);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                try
                {
                    Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }
            }

            public void Insert(int index, TKey value)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new SortedListKeyEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new SortedListKeyEnumerator(_dict);
            }

            public int IndexOf(TKey key)
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                int num = Array.BinarySearch(_dict.keys, 0, _dict.Count, key, _dict.comparer);
                if (num >= 0)
                {
                    return num;
                }

                return -1;
            }

            public bool Remove(TKey key)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
                return false;
            }

            public void RemoveAt(int index)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }
        }

        [Serializable]
        [DebuggerTypeProxy(typeof(System_DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        private sealed class ValueList : IList<TValue>, ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection
        {
            private SortedList<TKey, TValue> _dict;

            public int Count => _dict._size;

            public bool IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)_dict).SyncRoot;

            public TValue this[int index]
            {
                get
                {
                    return _dict.GetByIndex(index);
                }
                set
                {
                    ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
                }
            }

            internal ValueList(SortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            public void Add(TValue key)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public void Clear()
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public bool Contains(TValue value)
            {
                return _dict.ContainsValue(value);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                try
                {
                    Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }
            }

            public void Insert(int index, TValue value)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new SortedListValueEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new SortedListValueEnumerator(_dict);
            }

            public int IndexOf(TValue value)
            {
                return Array.IndexOf(_dict.values, value, 0, _dict.Count);
            }

            public bool Remove(TValue value)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
                return false;
            }

            public void RemoveAt(int index)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_SortedListNestedWrite);
            }
        }

        private TKey[] keys;

        private TValue[] values;

        private int _size;

        private int version;

        private IComparer<TKey> comparer;

        private KeyList keyList;

        private ValueList valueList;

        [NonSerialized]
        private object _syncRoot;

        private static TKey[] emptyKeys = new TKey[0];

        private static TValue[] emptyValues = new TValue[0];

        private const int _defaultCapacity = 4;

        private const int MaxArrayLength = 2146435071;

        //
        // Summary:
        //     Gets or sets the number of elements that the System.Collections.Generic.SortedList`2
        //     can contain.
        //
        // Returns:
        //     The number of elements that the System.Collections.Generic.SortedList`2 can contain.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     System.Collections.Generic.SortedList`2.Capacity is set to a value that is less
        //     than System.Collections.Generic.SortedList`2.Count.
        //
        //   T:System.OutOfMemoryException:
        //     There is not enough memory available on the system.
        [__DynamicallyInvokable]
        public int Capacity
        {
            [__DynamicallyInvokable]
            get
            {
                return keys.Length;
            }
            [__DynamicallyInvokable]
            set
            {
                if (value == keys.Length)
                {
                    return;
                }

                if (value < _size)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
                }

                if (value > 0)
                {
                    TKey[] destinationArray = new TKey[value];
                    TValue[] destinationArray2 = new TValue[value];
                    if (_size > 0)
                    {
                        Array.Copy(keys, 0, destinationArray, 0, _size);
                        Array.Copy(values, 0, destinationArray2, 0, _size);
                    }

                    keys = destinationArray;
                    values = destinationArray2;
                }
                else
                {
                    keys = emptyKeys;
                    values = emptyValues;
                }
            }
        }

        //
        // Summary:
        //     Gets the System.Collections.Generic.IComparer`1 for the sorted list.
        //
        // Returns:
        //     The System.IComparable`1 for the current System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public IComparer<TKey> Comparer
        {
            [__DynamicallyInvokable]
            get
            {
                return comparer;
            }
        }

        //
        // Summary:
        //     Gets the number of key/value pairs contained in the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     The number of key/value pairs contained in the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public int Count
        {
            [__DynamicallyInvokable]
            get
            {
                return _size;
            }
        }

        //
        // Summary:
        //     Gets a collection containing the keys in the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     A System.Collections.Generic.IList`1 containing the keys in the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public IList<TKey> Keys
        {
            [__DynamicallyInvokable]
            get
            {
                return GetKeyListHelper();
            }
        }

        [__DynamicallyInvokable]
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            [__DynamicallyInvokable]
            get
            {
                return GetKeyListHelper();
            }
        }

        //
        // Summary:
        //     Gets an System.Collections.ICollection containing the keys of the System.Collections.IDictionary.
        //
        // Returns:
        //     An System.Collections.ICollection containing the keys of the System.Collections.IDictionary.
        [__DynamicallyInvokable]
        ICollection IDictionary.Keys
        {
            [__DynamicallyInvokable]
            get
            {
                return GetKeyListHelper();
            }
        }

        [__DynamicallyInvokable]
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            [__DynamicallyInvokable]
            get
            {
                return GetKeyListHelper();
            }
        }

        //
        // Summary:
        //     Gets a collection containing the values in the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     A System.Collections.Generic.IList`1 containing the values in the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public IList<TValue> Values
        {
            [__DynamicallyInvokable]
            get
            {
                return GetValueListHelper();
            }
        }

        [__DynamicallyInvokable]
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            [__DynamicallyInvokable]
            get
            {
                return GetValueListHelper();
            }
        }

        //
        // Summary:
        //     Gets an System.Collections.ICollection containing the values in the System.Collections.IDictionary.
        //
        // Returns:
        //     An System.Collections.ICollection containing the values in the System.Collections.IDictionary.
        [__DynamicallyInvokable]
        ICollection IDictionary.Values
        {
            [__DynamicallyInvokable]
            get
            {
                return GetValueListHelper();
            }
        }

        [__DynamicallyInvokable]
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            [__DynamicallyInvokable]
            get
            {
                return GetValueListHelper();
            }
        }

        [__DynamicallyInvokable]
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            [__DynamicallyInvokable]
            get
            {
                return false;
            }
        }

        //
        // Summary:
        //     Gets a value indicating whether the System.Collections.IDictionary is read-only.
        //
        // Returns:
        //     true if the System.Collections.IDictionary is read-only; otherwise, false. In
        //     the default implementation of System.Collections.Generic.SortedList`2, this property
        //     always returns false.
        [__DynamicallyInvokable]
        bool IDictionary.IsReadOnly
        {
            [__DynamicallyInvokable]
            get
            {
                return false;
            }
        }

        //
        // Summary:
        //     Gets a value indicating whether the System.Collections.IDictionary has a fixed
        //     size.
        //
        // Returns:
        //     true if the System.Collections.IDictionary has a fixed size; otherwise, false.
        //     In the default implementation of System.Collections.Generic.SortedList`2, this
        //     property always returns false.
        [__DynamicallyInvokable]
        bool IDictionary.IsFixedSize
        {
            [__DynamicallyInvokable]
            get
            {
                return false;
            }
        }

        //
        // Summary:
        //     Gets a value indicating whether access to the System.Collections.ICollection
        //     is synchronized (thread safe).
        //
        // Returns:
        //     true if access to the System.Collections.ICollection is synchronized (thread
        //     safe); otherwise, false. In the default implementation of System.Collections.Generic.SortedList`2,
        //     this property always returns false.
        [__DynamicallyInvokable]
        bool ICollection.IsSynchronized
        {
            [__DynamicallyInvokable]
            get
            {
                return false;
            }
        }

        //
        // Summary:
        //     Gets an object that can be used to synchronize access to the System.Collections.ICollection.
        //
        // Returns:
        //     An object that can be used to synchronize access to the System.Collections.ICollection.
        //     In the default implementation of System.Collections.Generic.SortedList`2, this
        //     property always returns the current instance.
        [__DynamicallyInvokable]
        object ICollection.SyncRoot
        {
            [__DynamicallyInvokable]
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }

                return _syncRoot;
            }
        }

        //
        // Summary:
        //     Gets or sets the value associated with the specified key.
        //
        // Parameters:
        //   key:
        //     The key whose value to get or set.
        //
        // Returns:
        //     The value associated with the specified key. If the specified key is not found,
        //     a get operation throws a System.Collections.Generic.KeyNotFoundException and
        //     a set operation creates a new element using the specified key.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.Collections.Generic.KeyNotFoundException:
        //     The property is retrieved and key does not exist in the collection.
        [__DynamicallyInvokable]
        public TValue this[TKey key]
        {
            [__DynamicallyInvokable]
            get
            {
                int num = IndexOfKey(key);
                if (num >= 0)
                {
                    return values[num];
                }

                ThrowHelper.ThrowKeyNotFoundException();
                return default(TValue);
            }
            [__DynamicallyInvokable]
            set
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                int num = Array.BinarySearch(keys, 0, _size, key, comparer);
                if (num >= 0)
                {
                    values[num] = value;
                    version++;
                }
                else
                {
                    Insert(~num, key, value);
                }
            }
        }

        //
        // Summary:
        //     Gets or sets the element with the specified key.
        //
        // Parameters:
        //   key:
        //     The key of the element to get or set.
        //
        // Returns:
        //     The element with the specified key, or null if key is not in the dictionary or
        //     key is of a type that is not assignable to the key type TKey of the System.Collections.Generic.SortedList`2.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.ArgumentException:
        //     A value is being assigned, and key is of a type that is not assignable to the
        //     key type TKey of the System.Collections.Generic.SortedList`2.-or-A value is being
        //     assigned, and value is of a type that is not assignable to the value type TValue
        //     of the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        object IDictionary.this[object key]
        {
            [__DynamicallyInvokable]
            get
            {
                if (IsCompatibleKey(key))
                {
                    int num = IndexOfKey((TKey)key);
                    if (num >= 0)
                    {
                        return values[num];
                    }
                }

                return null;
            }
            [__DynamicallyInvokable]
            set
            {
                if (!IsCompatibleKey(key))
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
                }

                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
                try
                {
                    TKey key2 = (TKey)key;
                    try
                    {
                        this[key2] = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                    }
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
                }
            }
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that is empty, has the default initial capacity, and uses the default System.Collections.Generic.IComparer`1.
        [__DynamicallyInvokable]
        public SortedList()
        {
            keys = emptyKeys;
            values = emptyValues;
            _size = 0;
            comparer = Comparer<TKey>.Default;
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that is empty, has the specified initial capacity, and uses the default System.Collections.Generic.IComparer`1.
        //
        // Parameters:
        //   capacity:
        //     The initial number of elements that the System.Collections.Generic.SortedList`2
        //     can contain.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     capacity is less than zero.
        [__DynamicallyInvokable]
        public SortedList(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNumRequired);
            }

            keys = new TKey[capacity];
            values = new TValue[capacity];
            comparer = Comparer<TKey>.Default;
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that is empty, has the default initial capacity, and uses the specified System.Collections.Generic.IComparer`1.
        //
        // Parameters:
        //   comparer:
        //     The System.Collections.Generic.IComparer`1 implementation to use when comparing
        //     keys.-or-null to use the default System.Collections.Generic.Comparer`1 for the
        //     type of the key.
        [__DynamicallyInvokable]
        public SortedList(IComparer<TKey> comparer)
            : this()
        {
            if (comparer != null)
            {
                this.comparer = comparer;
            }
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that is empty, has the specified initial capacity, and uses the specified System.Collections.Generic.IComparer`1.
        //
        // Parameters:
        //   capacity:
        //     The initial number of elements that the System.Collections.Generic.SortedList`2
        //     can contain.
        //
        //   comparer:
        //     The System.Collections.Generic.IComparer`1 implementation to use when comparing
        //     keys.-or-null to use the default System.Collections.Generic.Comparer`1 for the
        //     type of the key.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     capacity is less than zero.
        [__DynamicallyInvokable]
        public SortedList(int capacity, IComparer<TKey> comparer)
            : this(comparer)
        {
            Capacity = capacity;
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that contains elements copied from the specified System.Collections.Generic.IDictionary`2,
        //     has sufficient capacity to accommodate the number of elements copied, and uses
        //     the default System.Collections.Generic.IComparer`1.
        //
        // Parameters:
        //   dictionary:
        //     The System.Collections.Generic.IDictionary`2 whose elements are copied to the
        //     new System.Collections.Generic.SortedList`2.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     dictionary is null.
        //
        //   T:System.ArgumentException:
        //     dictionary contains one or more duplicate keys.
        [__DynamicallyInvokable]
        public SortedList(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, (IComparer<TKey>)null)
        {
        }

        //
        // Summary:
        //     Initializes a new instance of the System.Collections.Generic.SortedList`2 class
        //     that contains elements copied from the specified System.Collections.Generic.IDictionary`2,
        //     has sufficient capacity to accommodate the number of elements copied, and uses
        //     the specified System.Collections.Generic.IComparer`1.
        //
        // Parameters:
        //   dictionary:
        //     The System.Collections.Generic.IDictionary`2 whose elements are copied to the
        //     new System.Collections.Generic.SortedList`2.
        //
        //   comparer:
        //     The System.Collections.Generic.IComparer`1 implementation to use when comparing
        //     keys.-or-null to use the default System.Collections.Generic.Comparer`1 for the
        //     type of the key.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     dictionary is null.
        //
        //   T:System.ArgumentException:
        //     dictionary contains one or more duplicate keys.
        [__DynamicallyInvokable]
        public SortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
            : this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            dictionary.Keys.CopyTo(keys, 0);
            dictionary.Values.CopyTo(values, 0);
            Array.Sort(keys, values, comparer);
            _size = dictionary.Count;
        }

        //
        // Summary:
        //     Adds an element with the specified key and value into the System.Collections.Generic.SortedList`2.
        //
        // Parameters:
        //   key:
        //     The key of the element to add.
        //
        //   value:
        //     The value of the element to add. The value can be null for reference types.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.ArgumentException:
        //     An element with the same key already exists in the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            int num = Array.BinarySearch(keys, 0, _size, key, comparer);
            if (num >= 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
            }

            Insert(~num, key, value);
        }

        [__DynamicallyInvokable]
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        [__DynamicallyInvokable]
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int num = IndexOfKey(keyValuePair.Key);
            if (num >= 0 && EqualityComparer<TValue>.Default.Equals(values[num], keyValuePair.Value))
            {
                return true;
            }

            return false;
        }

        [__DynamicallyInvokable]
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int num = IndexOfKey(keyValuePair.Key);
            if (num >= 0 && EqualityComparer<TValue>.Default.Equals(values[num], keyValuePair.Value))
            {
                RemoveAt(num);
                return true;
            }

            return false;
        }

        //
        // Summary:
        //     Adds an element with the provided key and value to the System.Collections.IDictionary.
        //
        // Parameters:
        //   key:
        //     The System.Object to use as the key of the element to add.
        //
        //   value:
        //     The System.Object to use as the value of the element to add.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.ArgumentException:
        //     key is of a type that is not assignable to the key type TKey of the System.Collections.IDictionary.-or-value
        //     is of a type that is not assignable to the value type TValue of the System.Collections.IDictionary.-or-An
        //     element with the same key already exists in the System.Collections.IDictionary.
        [__DynamicallyInvokable]
        void IDictionary.Add(object key, object value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);
            try
            {
                TKey key2 = (TKey)key;
                try
                {
                    Add(key2, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }

        private KeyList GetKeyListHelper()
        {
            if (keyList == null)
            {
                keyList = new KeyList(this);
            }

            return keyList;
        }

        private ValueList GetValueListHelper()
        {
            if (valueList == null)
            {
                valueList = new ValueList(this);
            }

            return valueList;
        }

        //
        // Summary:
        //     Removes all elements from the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public void Clear()
        {
            version++;
            Array.Clear(keys, 0, _size);
            Array.Clear(values, 0, _size);
            _size = 0;
        }

        //
        // Summary:
        //     Determines whether the System.Collections.IDictionary contains an element with
        //     the specified key.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.IDictionary.
        //
        // Returns:
        //     true if the System.Collections.IDictionary contains an element with the key;
        //     otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }

            return false;
        }

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.SortedList`2 contains a specific
        //     key.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     true if the System.Collections.Generic.SortedList`2 contains an element with
        //     the specified key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        public bool ContainsKey(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.SortedList`2 contains a specific
        //     value.
        //
        // Parameters:
        //   value:
        //     The value to locate in the System.Collections.Generic.SortedList`2. The value
        //     can be null for reference types.
        //
        // Returns:
        //     true if the System.Collections.Generic.SortedList`2 contains an element with
        //     the specified value; otherwise, false.
        [__DynamicallyInvokable]
        public bool ContainsValue(TValue value)
        {
            return IndexOfValue(value) >= 0;
        }

        [__DynamicallyInvokable]
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - arrayIndex < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            for (int i = 0; i < Count; i++)
            {
                KeyValuePair<TKey, TValue> keyValuePair = (array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]));
            }
        }

        //
        // Summary:
        //     Copies the elements of the System.Collections.ICollection to an System.Array,
        //     starting at a particular System.Array index.
        //
        // Parameters:
        //   array:
        //     The one-dimensional System.Array that is the destination of the elements copied
        //     from System.Collections.ICollection. The System.Array must have zero-based indexing.
        //
        //   arrayIndex:
        //     The zero-based index in array at which copying begins.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     array is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     arrayIndex is less than zero.
        //
        //   T:System.ArgumentException:
        //     array is multidimensional.-or-array does not have zero-based indexing.-or-The
        //     number of elements in the source System.Collections.ICollection is greater than
        //     the available space from arrayIndex to the end of the destination array.-or-The
        //     type of the source System.Collections.ICollection cannot be cast automatically
        //     to the type of the destination array.
        [__DynamicallyInvokable]
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - arrayIndex < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            KeyValuePair<TKey, TValue>[] array2 = array as KeyValuePair<TKey, TValue>[];
            if (array2 != null)
            {
                for (int i = 0; i < Count; i++)
                {
                    array2[i + arrayIndex] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                }

                return;
            }

            object[] array3 = array as object[];
            if (array3 == null)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
            }

            try
            {
                for (int j = 0; j < Count; j++)
                {
                    array3[j + arrayIndex] = new KeyValuePair<TKey, TValue>(keys[j], values[j]);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
            }
        }

        private void EnsureCapacity(int min)
        {
            int num = ((keys.Length == 0) ? 4 : (keys.Length * 2));
            if ((uint)num > 2146435071u)
            {
                num = 2146435071;
            }

            if (num < min)
            {
                num = min;
            }

            Capacity = num;
        }

        private TValue GetByIndex(int index)
        {
            if (index < 0 || index >= _size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }

            return values[index];
        }

        //
        // Summary:
        //     Returns an enumerator that iterates through the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerator`1 of type System.Collections.Generic.KeyValuePair`2
        //     for the System.Collections.Generic.SortedList`2.
        [__DynamicallyInvokable]
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        [__DynamicallyInvokable]
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        //
        // Summary:
        //     Returns an System.Collections.IDictionaryEnumerator for the System.Collections.IDictionary.
        //
        // Returns:
        //     An System.Collections.IDictionaryEnumerator for the System.Collections.IDictionary.
        [__DynamicallyInvokable]
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, 2);
        }

        //
        // Summary:
        //     Returns an enumerator that iterates through a collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator that can be used to iterate through the collection.
        [__DynamicallyInvokable]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        private TKey GetKey(int index)
        {
            if (index < 0 || index >= _size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }

            return keys[index];
        }

        //
        // Summary:
        //     Searches for the specified key and returns the zero-based index within the entire
        //     System.Collections.Generic.SortedList`2.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.Generic.SortedList`2.
        //
        // Returns:
        //     The zero-based index of key within the entire System.Collections.Generic.SortedList`2,
        //     if found; otherwise, -1.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        public int IndexOfKey(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            int num = Array.BinarySearch(keys, 0, _size, key, comparer);
            if (num < 0)
            {
                return -1;
            }

            return num;
        }

        //
        // Summary:
        //     Searches for the specified value and returns the zero-based index of the first
        //     occurrence within the entire System.Collections.Generic.SortedList`2.
        //
        // Parameters:
        //   value:
        //     The value to locate in the System.Collections.Generic.SortedList`2. The value
        //     can be null for reference types.
        //
        // Returns:
        //     The zero-based index of the first occurrence of value within the entire System.Collections.Generic.SortedList`2,
        //     if found; otherwise, -1.
        [__DynamicallyInvokable]
        public int IndexOfValue(TValue value)
        {
            return Array.IndexOf(values, value, 0, _size);
        }

        private void Insert(int index, TKey key, TValue value)
        {
            if (_size == keys.Length)
            {
                EnsureCapacity(_size + 1);
            }

            if (index < _size)
            {
                Array.Copy(keys, index, keys, index + 1, _size - index);
                Array.Copy(values, index, values, index + 1, _size - index);
            }

            keys[index] = key;
            values[index] = value;
            _size++;
            version++;
        }

        //
        // Summary:
        //     Gets the value associated with the specified key.
        //
        // Parameters:
        //   key:
        //     The key whose value to get.
        //
        //   value:
        //     When this method returns, the value associated with the specified key, if the
        //     key is found; otherwise, the default value for the type of the value parameter.
        //     This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the System.Collections.Generic.SortedList`2 contains an element with
        //     the specified key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        public bool TryGetValue(TKey key, out TValue value)
        {
            int num = IndexOfKey(key);
            if (num >= 0)
            {
                value = values[num];
                return true;
            }

            value = default(TValue);
            return false;
        }

        //
        // Summary:
        //     Removes the element at the specified index of the System.Collections.Generic.SortedList`2.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the element to remove.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     index is less than zero.-or-index is equal to or greater than System.Collections.Generic.SortedList`2.Count.
        [__DynamicallyInvokable]
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }

            _size--;
            if (index < _size)
            {
                Array.Copy(keys, index + 1, keys, index, _size - index);
                Array.Copy(values, index + 1, values, index, _size - index);
            }

            keys[_size] = default(TKey);
            values[_size] = default(TValue);
            version++;
        }

        //
        // Summary:
        //     Removes the element with the specified key from the System.Collections.Generic.SortedList`2.
        //
        // Parameters:
        //   key:
        //     The key of the element to remove.
        //
        // Returns:
        //     true if the element is successfully removed; otherwise, false. This method also
        //     returns false if key was not found in the original System.Collections.Generic.SortedList`2.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        public bool Remove(TKey key)
        {
            int num = IndexOfKey(key);
            if (num >= 0)
            {
                RemoveAt(num);
            }

            return num >= 0;
        }

        //
        // Summary:
        //     Removes the element with the specified key from the System.Collections.IDictionary.
        //
        // Parameters:
        //   key:
        //     The key of the element to remove.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        [__DynamicallyInvokable]
        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }

        //
        // Summary:
        //     Sets the capacity to the actual number of elements in the System.Collections.Generic.SortedList`2,
        //     if that number is less than 90 percent of current capacity.
        [__DynamicallyInvokable]
        public void TrimExcess()
        {
            int num = (int)((double)keys.Length * 0.9);
            if (_size < num)
            {
                Capacity = _size;
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            return key is TKey;
        }

        public static implicit operator GenericSortedList<TKey, TValue>(SortedList<Guid, ArchiveTableSummary<TKey, TValue>> v)
        {
            throw new NotImplementedException();
        }
    }
}
#if false // Decompilation log
'8' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
#endif
