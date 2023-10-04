//******************************************************************************************************
//  NullableLargeArray.cs - Gbtc
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
//  09/01/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Collections;

/// <summary>
/// Provides a high speed list that can have elements that can be null.
/// It is similar to a <see cref="List{T}"/> except high speed lookup for
/// NextIndexOfNull-like functions is provided as well.
/// </summary>
/// <typeparam name="T">Array type.</typeparam>
public class NullableLargeArray<T> : IEnumerable<T?>
{
    #region [ Members ]

    private readonly BitArray m_isUsed;
    private readonly LargeArray<T?> m_list;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="NullableLargeArray{T}"/>.
    /// </summary>
    public NullableLargeArray()
    {
        m_list = new LargeArray<T?>();
        m_isUsed = new BitArray(false, m_list.Capacity);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the number of items that can be stored in the array.
    /// </summary>
    public int Capacity => m_list.Capacity;

    /// <summary>
    /// Gets the number of non-null items that are in the array.
    /// </summary>
    public int CountUsed => m_isUsed.SetCount;

    /// <summary>
    /// Gets the number of available spaces in the array. Equal to <see cref="Capacity"/> - <see cref="CountUsed"/>.
    /// </summary>
    public int CountFree => m_isUsed.ClearCount;

    /// <summary>
    /// Gets the provided item from the array.
    /// </summary>
    /// <param name="index">The index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    public T? this[int index]
    {
        get => GetValue(index);
        set => SetValue(index, value);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Checks if the element at the specified index has a value.
    /// </summary>
    /// <param name="index">The index to check.</param>
    /// <returns><c>true</c> if the element has a value; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Bounds checking is performed by the underlying BitArray.
    /// </remarks>
    public bool HasValue(int index)
    {
        // Bounds checking is done in BitArray.
        return m_isUsed[index];
    }

    /// <summary>
    /// Tries to get the value at the specified index if it exists.
    /// </summary>
    /// <param name="index">The index of the value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value at the specified index if it exists, or the default value for the type if not.</param>
    /// <returns><c>true</c> if the value at the specified index exists; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method checks if the element at the specified index has a value using <see cref="HasValue(int)"/>.
    /// </remarks>
    public bool TryGetValue(int index, out T? value)
    {
        if (HasValue(index))
        {
            value = m_list[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Increases the capacity of the array to at least the given length. Will not reduce the size.
    /// </summary>
    /// <param name="length"></param>
    /// <returns>The current length of the list.</returns>
    public int SetCapacity(int length)
    {
        if (length > Capacity)
        {
            m_list.SetCapacity(length);
            m_isUsed.SetCapacity(m_list.Capacity);
        }

        return Capacity;
    }

    /// <summary>
    /// Gets the specified item from the list. Throws an exception if the item is <c>null</c>.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>The item from the specified index.</returns>
    public T? GetValue(int index)
    {
        if (!HasValue(index))
            throw new NullReferenceException();

        return m_list[index];
    }

    /// <summary>
    /// Sets the following item to <c>null</c>.
    /// </summary>
    /// <param name="index"></param>
    public void SetNull(int index)
    {
        m_isUsed.ClearBit(index);
        m_list[index] = default;
    }

    /// <summary>
    /// Sets the value at the specified index.
    /// </summary>
    /// <param name="index">The index at which to set the value.</param>
    /// <param name="value">The value to set at the specified index.</param>
    /// <remarks>
    /// This method sets the value at the specified index and marks it as used using <see cref="BitArray.SetBit(int)"/>.
    /// </remarks>
    public void SetValue(int index, T? value)
    {
        m_isUsed.SetBit(index);
        m_list[index] = value;
    }

    /// <summary>
    /// Overwrites the value at the specified index.
    /// </summary>
    /// <param name="index">The index of the value to overwrite.</param>
    /// <param name="value">The new value to set at the specified index.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index does not exist.</exception>
    /// <remarks>
    /// This method replaces the existing value at the specified index with the new value.
    /// </remarks>
    public void OverwriteValue(int index, T? value)
    {
        if (!HasValue(index))
            throw new IndexOutOfRangeException("Index does not exist.");

        m_list[index] = value;
    }

    /// <summary>
    /// Adds a new value to the list at the nearest possible empty location.
    /// If there is not enough room, the list is automatically expanded.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>The index where the value was placed.</returns>
    public int AddValue(T? value)
    {
        int index = FindFirstEmptyIndex();

        if (index < 0)
        {
            SetCapacity(Capacity + 1);
            index = FindFirstEmptyIndex();
        }

        SetValue(index, value);

        return index;
    }

    /// <summary>
    /// Clears all elements in the list
    /// </summary>
    public void Clear()
    {
        m_list.Clear();
        m_isUsed.ClearAll();
    }

    private int FindFirstEmptyIndex()
    {
        return m_isUsed.FindClearedBit();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the non-null elements of this collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
    /// </returns>
    /// <filterpriority>1</filterpriority>
    public IEnumerator<T?> GetEnumerator()
    {
        return m_isUsed.GetAllSetBits().Select(index => m_list[index]).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}