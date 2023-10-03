//******************************************************************************************************
//  LargeArray.cs - Gbtc
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
//  09/14/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;
using System.Runtime.CompilerServices;

namespace SnapDB.Collections;

/// <summary>
/// Implementation that utilizes a jagged array structure and 
/// array expansion optimization to improve performance.
/// </summary>
/// <typeparam name="T">Array type.</typeparam>
public class LargeArray<T>
{
    // Since large arrays expand slowly, this class can quickly grow an array with millions of elements.
    // It is highly advised that these objects are structs since keeping a list of millions of classes
    // would cause the garbage collection cycles to become very slow.
    private readonly int m_size;
    private readonly int m_bitShift;
    private readonly int m_mask;
    private T[]?[] m_array;

    /// <summary>
    /// Creates a <see cref="LargeArray{T}"/> with a jagged array depth of 1024 elements.
    /// </summary>
    public LargeArray()
        : this(1024) { }

    /// <summary>
    /// Creates a <see cref="LargeArray{T}"/> with the specified jagged array depth.
    /// </summary>
    /// <param name="jaggedArrayDepth">The number of elements per jagged array (rounds up to the nearest power of 2).</param>
    public LargeArray(int jaggedArrayDepth)
    {
        m_size = (int)BitMath.RoundUpToNearestPowerOfTwo((uint)jaggedArrayDepth);
        m_mask = m_size - 1;
        m_bitShift = BitMath.CountBitsSet((uint)m_mask);
        m_array = Array.Empty<T[]?>();
        Capacity = 0;
    }

    /// <summary>
    /// Gets or sets the value in the specified index of the array.
    /// </summary>
    /// <param name="index">The index to address.</param>
    /// <returns>The value at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            Validate(index);
            return m_array[index >> m_bitShift]![index & m_mask];
        }
        set
        {
            Validate(index);
            m_array[index >> m_bitShift]![index & m_mask] = value;
        }
    }

    /// <summary>
    /// Gets the number of items in the array.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Sets the capacity of the array to at least the given length. Will not reduce the size.
    /// </summary>
    /// <param name="length">The new length (capacity) for the data structure.</param>
    /// <returns>The updated capacity of the data structure after setting it to the specified length.</returns>
    public int SetCapacity(int length)
    {
        // Variable to store the number of arrays needed to accommodate the specified length.
        int arrayCount;

        // Check if the specified length does not align with the mask (bitwise check).
        if ((length & m_mask) != 0)
            arrayCount = (length >> m_bitShift) + 1;
        else
            arrayCount = length >> m_bitShift;

        // Check if the required number of arrays is greater than the current array count.
        if (arrayCount > m_array.Length)
        {
            // Create a new array of arrays with the required number of arrays.
            T[][] newArray = new T[arrayCount][];

            // Copy existing arrays to the new array.
            m_array.CopyTo(newArray, 0);

            // Initialize any additional arrays needed with the specified size.
            for (int x = m_array.Length; x < arrayCount; x++)
                newArray[x] = new T[m_size];

            // Update the reference to the array of arrays and the total capacity.
            m_array = newArray;
            Capacity = m_array.Length * m_size;
        }

        // Return the updated capacity of the data structure.
        return Capacity;
    }

    // Validates whether a given index is within the valid range of positions in the data structure.
    // If the index is less than 0 or greater than or equal to the capacity, an exception is thrown.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Validate(int index)
    {
        if (index < 0 || index >= Capacity)
            ThrowException(index);
    }

    // Throws an ArgumentOutOfRangeException if index is outside of valid range.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowException(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Must be greater than or equal to zero.");
            
        if (index >= Capacity)
            throw new ArgumentOutOfRangeException(nameof(index), "exceeds the length of the array.");
    }

    /// <summary>
    /// Clears all elements in the data structure, setting each element to its default value.
    /// </summary>
    public void Clear()
    {
        foreach (T[]? items in m_array)
        {
            if (items is not null)
                Array.Clear(items, 0, items.Length);
        }
    }
}
