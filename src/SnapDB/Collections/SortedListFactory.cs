//******************************************************************************************************
//  SortedListFactory.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/20/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Collections;

/// <summary>
/// Quickly creates a <see cref="SortedList"/> from a provided list of keys and values.
/// </summary>
public static class SortedListFactory
{
    #region [ Members ]

    // A wrapper class that creates a SortedList from a set of key/value pair. 
    private class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
    {
        #region [ Constructors ]

        // Initializes a new instance of the DictionaryWrapper<TKey, TValue> class with the provided collections of keys and values.
        public DictionaryWrapper(ICollection<TKey> keys, ICollection<TValue> values)
        {
            Keys = keys;
            Values = values;
        }

        #endregion

        #region [ Properties ]

        public int Count => Keys.Count;

        public bool IsReadOnly => true;

        public TValue this[TKey key]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ICollection<TKey> Keys { get; }

        public ICollection<TValue> Values { get; }

        #endregion

        #region [ Methods ]

        // Converts the dictionary to a sorted list and returns it.
        public SortedList<TKey, TValue> ToSortedList()
        {
            return new SortedList<TKey, TValue>(this);
        }

        // Returns an enumerator that iterates through the key-value pairs in the dictionary.
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            // Iterate through keys and values to yield key-value pairs.
            using IEnumerator<TKey> keys = Keys.GetEnumerator();
            using IEnumerator<TValue> values = Values.GetEnumerator();

            while (keys.MoveNext() && values.MoveNext())
                yield return new KeyValuePair<TKey, TValue>(keys.Current, values.Current);
        }

        // Returns an enumerator that iterates through the key-value pairs in the dictionary.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // The following methods and properties are implemented for IDictionary<TKey, TValue> but are not used in this context.
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public void Add(TKey key, TValue value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a sorted list from a provided keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the sorted list.</typeparam>
    /// <typeparam name="TValue">The type of values in the sorted list.</typeparam>
    /// <param name="keys">A collection of keys to be used in the sorted list.</param>
    /// <param name="values">A collection of values to be associated with the keys in the sorted list.</param>
    /// <returns>A sorted list containing the specified keys and values.</returns>
    public static SortedList<TKey, TValue> Create<TKey, TValue>(ICollection<TKey> keys, ICollection<TValue> values) where TKey : notnull
    {
        // Creates a sorted list from the keys and values using a helper class.
        return new DictionaryWrapper<TKey, TValue>(keys, values).ToSortedList();
    }

    #endregion
}