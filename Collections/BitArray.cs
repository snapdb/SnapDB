//******************************************************************************************************
//  BitArray.cs - Gbtc
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
//  03/20/2012 - Steven E. Chisholm
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
/// Represents an array of bits much like the native .NET implementation, 
/// however this focuses on providing a free space bit array.
/// </summary>
public sealed class BitArray
{
    #region [ Members ]

    /// <summary>
    /// Defines the number of bits to shift to get the index of the array.
    /// </summary>
    public const int BitsPerElementShift = 5;

    /// <summary>
    /// Defines the mask to apply to get the bit position of the value.
    /// </summary>
    public const int BitsPerElementMask = BitsPerElement - 1;

    /// <summary>
    /// Defines the number of bits per array element.
    /// </summary>
    public const int BitsPerElement = sizeof(int) * 8;

    private int[] m_array;
    private int m_count;
    private int m_setCount;
    private int m_lastFoundClearedIndex;
    private int m_lastFoundSetIndex;
    private readonly bool m_initialState;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes <see cref="BitArray"/>.
    /// </summary>
    /// <param name="initialState">
    /// If this is set to <c>true</c>, all elements will be set. 
    /// If it is set to <c>false</c>, all elements will be cleared.
    /// </param>
    /// <param name="count">The number of bit positions to support.</param>
    public BitArray(bool initialState, int count = BitsPerElement)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        // If the number does not lie on a 32 bit boundary, add 1 to the number of items in the array.
        if ((count & BitsPerElementMask) != 0)
            m_array = new int[(count >> BitsPerElementShift) + 1];
        else
            m_array = new int[count >> BitsPerElementShift];

        if (initialState)
        {
            m_setCount = count;

            // If the initial state is true, set all bits to 1 (-1 in two's complement).
            for (int x = 0; x < m_array.Length; x++)            
                m_array[x] = -1; 
        }
        else
        {
            // If initial state is false, .NET initializes all memory with zeroes.
            m_setCount = 0;
        }

        m_count = count;
        m_initialState = initialState;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets or sets individual bits in this array.
    /// </summary>
    /// <param name="index">Bit position to access.</param>
    /// <returns>Bit at specified <paramref name="index"/>.</returns>
    public bool this[int index]
    {
        get => GetBit(index);
        set
        {
            if (value)
                SetBit(index);
            else
                ClearBit(index);
        }
    }

    /// <summary>
    /// Gets the number of bits this array contains.
    /// </summary>
    public int Count => m_count;

    /// <summary>
    /// Gets the number of bits that are set in this array.
    /// </summary>
    public int SetCount => m_setCount;

    /// <summary>
    /// Gets the number of bits that are cleared in this array.
    /// </summary>
    public int ClearCount => m_count - m_setCount;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Gets the status of the corresponding bit.
    /// </summary>
    /// <param name="index">Bit position to access.</param>
    /// <returns><c>true</c> if set; otherwise, <c>false</c> if cleared.</returns>
    public bool GetBit(int index)
    {
        Validate(index);

        return (m_array[index >> BitsPerElementShift] & (1 << (index & BitsPerElementMask))) != 0;
    }

    /// <summary>
    /// Gets the status of the corresponding bit.
    /// This method does not validate the bounds of the array, 
    /// and will be Aggressively Inlined.
    /// </summary>
    /// <param name="index">Bit position to access.</param>
    /// <returns><c>true</c> if set; otherwise, <c>false</c> if cleared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBitUnchecked(int index)
    {
        // The exact speed varies, but has been shown to be anywhere from 1 to 6 times faster. 
        // (All smaller than a few nanoseconds. But in an inner loop, this can be a decent improvement.)
        return (m_array[index >> BitsPerElementShift] & (1 << (index & BitsPerElementMask))) != 0;
    }

    /// <summary>
    /// Sets the corresponding bit to <c>true</c>.
    /// </summary>
    /// <param name="index">Bit position to set.</param>
    public void SetBit(int index)
    {
        TrySetBit(index);
    }

    /// <summary>
    /// Sets the corresponding bit to <c>true</c>. 
    /// Returns <c>true</c> if the bit state was changed.
    /// </summary>
    /// <param name="index">Bit position to set.</param>
    /// <returns><c>true</c> if the bit state was changed; otherwise, <c>false</c> if the bit was already set.</returns>
    public bool TrySetBit(int index)
    {
        Validate(index);

        int subBit = 1 << (index & BitsPerElementMask);
        int element = index >> BitsPerElementShift;
        int value = m_array[element];

        if ((value & subBit) == 0) // If bit is set
        {
            m_lastFoundSetIndex = 0;
            m_setCount++;
            m_array[element] = value | subBit;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all bits.
    /// </summary>
    public void ClearAll()
    {
        m_setCount = 0;
        Array.Clear(m_array, 0, m_array.Length);
    }

    /// <summary>
    /// Clears bit at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Bit position to set.</param>
    public void ClearBit(int index)
    {
        TryClearBit(index);
    }

    /// <summary>
    /// Sets the corresponding bit to <c>false</c>.
    /// Returns <c>true</c> if the bit state was changed.
    /// </summary>
    /// <param name="index">Bit position to clear.</param>
    /// <returns><c>true</c> if the bit state was changed; othwerwise, <c>false</c> if the bit was already cleared.</returns>
    public bool TryClearBit(int index)
    {
        Validate(index);
        int subBit = 1 << (index & BitsPerElementMask);
        int element = index >> BitsPerElementShift;
        int value = m_array[element];

        if ((value & subBit) != 0) //if bit is set
        {
            m_lastFoundClearedIndex = 0;
            m_setCount--;
            m_array[element] = value & ~subBit;

            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears a specified series of bits.
    /// </summary>
    /// <param name="index">Starting index to clear.</param>
    /// <param name="length">Length of bits to clear.</param>
    public void ClearBits(int index, int length)
    {
        Validate(index, length);

        for (int x = index; x < index + length; x++)
            ClearBit(x);
    }

    /// <summary>
    /// Sets all bits.
    /// </summary>
    public void SetAll()
    {
        m_setCount = m_count;

        for (int x = 0; x < m_array.Length; x++)
            m_array[x] = -1; // (-1 is all bits set)
    }

    /// <summary>
    /// Sets a specified series of bits.
    /// </summary>
    /// <param name="index">Starting index to set.</param>
    /// <param name="length">Length of bits to set.</param>
    public void SetBits(int index, int length)
    {
        Validate(index, length);

        for (int x = index; x < index + length; x++)
            SetBit(x);
    }

    /// <summary>
    /// Determines if any of the provided bits are set.
    /// </summary>
    /// <param name="index">Starting index to check.</param>
    /// <param name="length">Number of bits to check.</param>
    /// <returns><c>true</c> if all bits in range are set; otherwise, <c>false</c>.</returns>
    public bool AreAllBitsSet(int index, int length)
    {
        Validate(index, length);

        for (int x = index; x < index + length; x++)
        {
            if ((m_array[x >> BitsPerElementShift] & (1 << (x & BitsPerElementMask))) == 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if any of the provided bits are cleared.
    /// </summary>
    /// <param name="index">Starting index to check.</param>
    /// <param name="length">Number of bits to check.</param>
    /// <returns><c>true</c> if all bits in range are clear; otherwise, <c>false</c>.</returns>
    public bool AreAllBitsCleared(int index, int length)
    {
        Validate(index, length);

        for (int x = index; x < index + length; x++)
        {
            if ((m_array[x >> BitsPerElementShift] & (1 << (x & BitsPerElementMask))) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Increases the capacity of the bit array.
    /// Decreasing capacity is currently not supported.
    /// </summary>
    /// <param name="capacity">Number of bits to support.</param>
    public void SetCapacity(int capacity)
    {
        int[] array;

        if (m_count >= capacity)
            return;

        // If the number does not lie on a 32 bit boundary, add 1 to the number of items in the array.
        if ((capacity & BitsPerElementMask) != 0)
            array = new int[(capacity >> BitsPerElementShift) + 1];
        else
            array = new int[capacity >> BitsPerElementShift];

        m_array.CopyTo(array, 0);

        // If initial state is to set all of the bits, set them.
        // Note: Since the initial state already initialized any remaining bits
        // after m_count, this does not need to be done again.
        if (m_initialState)
        {
            m_setCount += capacity - m_count;

            for (int x = m_array.Length; x < array.Length; x++)
                array[x] = -1;
        }

        m_array = array;
        m_count = capacity;
    }

    /// <summary>
    /// Ensures that the bit array has a minimum capacity to accommodate a specified number of bits.
    /// </summary>
    /// <param name="capacity">Minimum number of bits the bit array should be able to hold.</param>
    /// <remarks>
    /// If the current capacity of the bit array is less than the specified capacity, the method
    /// increases the capacity to either double the current capacity or the specified capacity,
    /// whichever is greater.
    /// </remarks>
    public void EnsureCapacity(int capacity)
    {
        if (capacity > m_count)
            SetCapacity(Math.Max(m_array.Length * BitsPerElement * 2, capacity));
    }

    /// <summary>
    /// Finds the position of the next cleared (unset) bit in the bit array.
    /// </summary>
    /// <returns>Position of the next cleared bit; otherwise, -1 if no cleared bit is found.</returns>
    public int FindClearedBit()
    {
        // Get the total number of 32 - bit elements in the bit array.
        int count = m_array.Length;

        // Iterate through the elements, starting from where the previous search left off.
        for (int x = m_lastFoundClearedIndex >> BitsPerElementShift; x < count; x++)
        {
            // If the current element has at least one cleared bit (not all bits set to 1).
            if (m_array[x] != -1)
            {
                // Calculate the position of the first cleared bit within the current element.
                int position = BitMath.CountTrailingOnes((uint)m_array[x]) + (x << BitsPerElementShift);

                // Update the index of the last found cleared bit for future searches.
                m_lastFoundClearedIndex = position;

                // Check if the position is beyond the total bit count, indicating no more cleared bits.
                if (m_lastFoundClearedIndex >= m_count)
                    return -1;

                // Return the position of the cleared bit within the bit array.
                return position;
            }
        }

        // If no cleared bit is found in the entire bit array, return -1.
        return -1;
    }

    /// <summary>
    /// Finds the position of the next set (1) bit in the bit array.
    /// </summary>
    /// <returns>Position of the next set bit; otherwise, -1 if no set bit is found.</returns>
    public int FindSetBit()
    {
        // Get the total number of 32-bit elements in the bit array.
        int count = m_array.Length;

        // Iterate through the elements, starting from where the previous search left off.
        for (int x = m_lastFoundSetIndex >> BitsPerElementShift; x < count; x++)
        {
            // If the current element has at least one set bit, use this element.
            if (m_array[x] != 0)
            {
                // Calculate the position of the first set bit within the current element.
                int position = BitMath.CountTrailingZeros((uint)m_array[x]) + (x << BitsPerElementShift);

                // Update the index of the last found set bit for future searches.
                m_lastFoundSetIndex = position;

                // Check if the position is beyond the total bit count, indicating no more set bits.
                if (m_lastFoundSetIndex >= m_count)
                    return -1;

                // Return the position of the set bit within the bit array.
                return position;
            }
        }

        // If no set bit is found in the entire bit array, return -1.
        return -1;
    }

    /// <summary>
    /// Returns a yielded list of all bits that are set.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of integers representing the positions of set bits in the bit array.</returns>
    public IEnumerable<int> GetAllSetBits()
    {
        // Get the total number of 32-bit elements in the bit array.
        int count = m_array.Length;

        // Iterate through the elements of the bit array.
        for (int x = 0; x < count; x++)
        {
            // If all bits are cleared, this entire section can be skipped
            if (m_array[x] != 0)
            {
                int end = Math.Min(x * BitsPerElement + BitsPerElement, m_count);
                for (int k = x * BitsPerElement; k < end; k++)
                {
                    if (GetBitUnchecked(k))
                        yield return k;
                }
            }
        }
    }

    /// <summary>
    /// Returns a yielded list of all bits that are cleared.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of integers representing the positions of cleared bits in the bit array.</returns>
    public IEnumerable<int> GetAllClearedBits()
    {
        // Get the total number of 32-bit elements in the bit array.
        int count = m_array.Length;

        // Iterate through the elements of the bit array.
        for (int x = 0; x < count; x++)
        {
            // If all bits are cleared, this entire section can be skipped
            if (m_array[x] != -1)
            {
                int end = Math.Min(x * BitsPerElement + BitsPerElement, m_count);
                for (int k = x * BitsPerElement; k < end; k++)
                {
                    if (!GetBitUnchecked(k))
                        yield return k;
                }
            }
        }
    }

    // Validates that a given index is within the valid range in the bit array.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Validate(int index)
    {
        if (index < 0 || index >= m_count)
            ThrowException(index);
    }

    // If provided index is out of valid range, throws an out-of-range exception.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowException(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Must be greater than or equal to zero.");

        if (index >= m_count)
            throw new ArgumentOutOfRangeException(nameof(index), "Exceedes the length of the array.");
    }

    // Validates that a range of bits (specified by index AND length) is within the valid range in the bit array. 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Validate(int index, int length)
    {
        if (index < 0 || length < 0 || index + length > m_count)
            ThrowException(index, length);
    }

    // If provided index, length, or sum of both is out of valid range, throws an out-of-range exception.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowException(int index, int length)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Must be greater than or equal to zero.");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Must be greater than or equal to zero.");

        if (index + length > m_count)
            throw new ArgumentOutOfRangeException(nameof(length), "index + length exceedes the length of the array.");
    }

    #endregion
}