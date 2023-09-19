//******************************************************************************************************
//  ConcurrentIndexedDictionary.cs - Gbtc
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
//  09/06/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/14/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using System.Runtime.CompilerServices;

namespace SnapDB.Collections;

/// <summary>
/// A thread-safe indexed dictionary that can only be added to.
/// </summary>
/// <remarks>
/// This is a special purpose class that supports only the 'Add' and 'Get' operations.
/// It is designed to have indexing and dictionary lookup capabilities.
/// </remarks>
public class ConcurrentIndexedDictionary<TKey, TValue>
{
    private TValue[] m_items = new TValue[4];
    private readonly Dictionary<TKey, int> m_lookup = new Dictionary<TKey, int>();
    private readonly object m_syncRoot = new object();

    /// <summary>
    /// Gets the number of items in the dictionary.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the value at the specified index in the dictionary.
    /// </summary>
    /// <param name="index">The index of the value to get.</param>
    /// <returns>The value at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the specified index is less than 0, or if it is
    /// greater than or equal to the Count of elements in the dictionary.
    /// </exception>
    public TValue this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Count)
                ThrowIndexException();

            return m_items[index];
        }
    }

    /// <summary>
    /// Gets the value associated with the specified key in the dictionary.
    /// </summary>
    /// <param name="key">The key for which to retrieve the associated value.</param>
    /// <returns>
    /// The value associated with the specified key.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the specified key is not found in the dictionary.
    /// </exception>
    public TValue Get(TKey key)
    {
        int index;
        lock (m_syncRoot)
            index = m_lookup[key];

        return this[index];
    }

    /// <summary>
    /// Gets the index of the specified <paramref name="key"/> in the dictionary.
    /// </summary>
    /// <param name="key">The key to find the index for.</param>
    /// <returns>The index of the key in the dictionary, or -1 if the key is not found.</returns>
    public int IndexOf(TKey key)
    {
        lock (m_syncRoot)
        {
            if (m_lookup.TryGetValue(key, out int index))
                return index;

            return -1;
        }
    }

    /// <summary>
    /// Adds a key-value pair to the dictionary, and returns the index at which it was added.
    /// </summary>
    /// <param name="key">The key to add to the dictionary.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <returns>The index at which the key-value pair was added in the dictionary.</returns>
    /// <exception cref="DuplicateNameException">Thrown if the specified key already exists in the dictionary.</exception>
    public int Add(TKey key, TValue value)
    {
        lock (m_syncRoot)
        {
            if (m_lookup.ContainsKey(key))
                throw new DuplicateNameException("Key already exists");

            return InternalAdd(key, value);
        }
    }

    private int InternalAdd(TKey key, TValue value)
    {
        m_lookup.Add(key, Count);

        // As long as the count of elements in the dictionary equals the capacity of the internal items array,
        // then when the capacity is reached a new internal items array with double the capacity will be created.
        // Existing elements will be copied to the new array and the reference will be updated to point to the new array.
        if (Count == m_items.Length)
        { 
            TValue[] newItems = new TValue[m_items.Length * 2];

            m_items.CopyTo(newItems, 0);
            m_items = newItems;
        }

        m_items[Count] = value;
        Count++;

        return Count - 1;
    }

    /// <summary>
    /// Gets the value associated with the specified <paramref name="key"/> from the dictionary, or adds it if not found.
    /// </summary>
    /// <param name="key">The key to retrieve or add.</param>
    /// <param name="createFunction">A function that creates the value if the key is not found.</param>
    /// <returns>The existing value associated with the key if found, or a newly created value if the key is not found.</returns>
    public TValue GetOrAdd(TKey key, Func<TValue> createFunction)
    {
        lock (m_syncRoot)
        {
            if (m_lookup.TryGetValue(key, out int index))
                return this[index];
            
            TValue value = createFunction();
            InternalAdd(key, value);

            return value;
        }
    }

    private void ThrowIndexException()
    {
        throw new IndexOutOfRangeException("specified index is outside the range of valid indexes");
    }
}
