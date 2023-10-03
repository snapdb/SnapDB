//******************************************************************************************************
//  SnapTypeCustomMethods`1.cs - Gbtc
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
//  04/12/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  02/22/2014 - Steven E. Chisholm
//       Combined Value and Key methods into a single class.
//     
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Provides custom methods for working with SnapTypeBase-derived types.
/// </summary>
/// <typeparam name="T">The SnapTypeBase-derived type.</typeparam>
/// <typeparam name="T">The SnapTypeBase-derived type.</typeparam>
/// <typeparam name="T">The SnapTypeBase-derived type.</typeparam>
/// <typeparam name="T">The SnapTypeBase-derived type.</typeparam>
/// <typeparam name="T">The SnapTypeBase-derived type.</typeparam>
public class SnapTypeCustomMethods<T>
    where T : SnapTypeBase<T>, new()
{
    protected T TempKey = new();
    protected int LastFoundIndex;

    /// <summary>
    /// Performs a binary search within a memory block pointed to by <paramref name="pointer"/>.
    /// </summary>
    /// <param name="pointer">A pointer to the memory block where the search will be performed.</param>
    /// <param name="key">The key to search for.</param>
    /// <param name="recordCount">The number of records in the memory block.</param>
    /// <param name="keyValueSize">The size of each key-value pair in bytes.</param>
    /// <returns>
    /// The index of the found key-value pair if it exists; otherwise, a negative value representing the bitwise complement
    /// of the index at which the key-value pair should be inserted to maintain sorted order.
    /// </returns>
    public virtual unsafe int BinarySearch(byte* pointer, T key, int recordCount, int keyValueSize)
    {
        if (LastFoundIndex == recordCount - 1)
        {
            if (key.CompareTo(pointer + keyValueSize * LastFoundIndex) > 0) //Key > CompareKey
            {
                LastFoundIndex++;
                return ~recordCount;
            }
        }
        else if (LastFoundIndex < recordCount)
        {
            if (key.CompareTo(pointer + keyValueSize * (LastFoundIndex + 1)) == 0)
            {
                LastFoundIndex++;
                return LastFoundIndex;
            }
        }
        return BinarySearch2(pointer, key, recordCount, keyValueSize);
    }

    /// <summary>
    /// Performs a binary search within a memory block pointed to by <paramref name="pointer"/>.
    /// </summary>
    /// <param name="pointer">A pointer to the memory block where the search will be performed.</param>
    /// <param name="key">The key to search for.</param>
    /// <param name="recordCount">The number of records in the memory block.</param>
    /// <param name="keyPointerSize">The size of each key-value pair in bytes.</param>
    /// <returns>
    /// The index of the found key-value pair if it exists; otherwise, a negative value representing the bitwise complement
    /// of the index at which the key-value pair should be inserted to maintain sorted order.
    /// </returns>
    protected virtual unsafe int BinarySearch2(byte* pointer, T key, int recordCount, int keyPointerSize)
    {
        if (recordCount == 0)
            return ~0;
        int searchLowerBoundsIndex = 0;
        int searchHigherBoundsIndex = recordCount - 1;
        int compare;

        if (LastFoundIndex <= recordCount)
        {
            LastFoundIndex = Math.Min(LastFoundIndex, recordCount - 1);

            compare = key.CompareTo(pointer + keyPointerSize * LastFoundIndex);
            if (compare == 0) // Are Equal
                return LastFoundIndex;
            if (compare > 0) // Key > CompareKey
            {
                // Value is greater, check the next key.
                LastFoundIndex++;

                // There is no greater key.
                if (LastFoundIndex == recordCount)
                    return ~recordCount;

                compare = key.CompareTo(pointer + keyPointerSize * LastFoundIndex);

                if (compare == 0) // Are Equal.
                    return LastFoundIndex;
                if (compare > 0) // Key > CompareKey.
                    searchLowerBoundsIndex = LastFoundIndex + 1;
                else
                    return ~LastFoundIndex;
            }
            else
            {
                // Value is lesser, check the previous key.
                // There is no lesser key.
                if (LastFoundIndex == 0)
                    return ~0;

                LastFoundIndex--;
                compare = key.CompareTo(pointer + keyPointerSize * LastFoundIndex);

                if (compare == 0) // Are Equal.
                    return LastFoundIndex;
                if (compare > 0) // Key > CompareKey.
                {
                    LastFoundIndex++;
                    return ~LastFoundIndex;
                }

                searchHigherBoundsIndex = LastFoundIndex - 1;
            }
        }

        while (searchLowerBoundsIndex <= searchHigherBoundsIndex)
        {
            int currentTestIndex = searchLowerBoundsIndex + (searchHigherBoundsIndex - searchLowerBoundsIndex >> 1);

            compare = key.CompareTo(pointer + keyPointerSize * currentTestIndex);

            if (compare == 0) // Are Equal.
            {
                LastFoundIndex = currentTestIndex;
                return currentTestIndex;
            }
            if (compare > 0) // Key > CompareKey.
                searchLowerBoundsIndex = currentTestIndex + 1;
            else
                searchHigherBoundsIndex = currentTestIndex - 1;
        }

        LastFoundIndex = searchLowerBoundsIndex;

        return ~searchLowerBoundsIndex;
    }

    #region [ Compare Operations ]

    /// <summary>
    /// Gets if <paramref name="left"/> is greater than or equal to <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The left operand to compare.</param>
    /// <param name="right">The right operand to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public virtual unsafe bool IsGreaterThan(T left, byte* right)
    {
        return CompareTo(left, right) > 0;
    }

    /// <summary>
    /// Gets if <paramref name="left"/> is greater than <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The left operand to compare.</param>
    /// <param name="right">The right operand to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public virtual unsafe bool IsGreaterThan(byte* left, T right)
    {
        return CompareTo(left, right) > 0;
    }

    /// <summary>
    /// Compares <paramref name="left"/> to <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The left operand to compare.</param>
    /// <param name="right">The right operand to compare.</param>
    /// <returns>A value indicating the relative order of <paramref name="left"/> and <paramref name="right"/>.</returns>
    public virtual unsafe int CompareTo(T left, byte* right)
    {
        TempKey.Read(right);
        return left.CompareTo(TempKey);
    }

    /// <summary>
    /// Compares <paramref name="left"/> to <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The left operand to compare.</param>
    /// <param name="right">The right operand to compare.</param>
    /// <returns>A value indicating the relative order of <paramref name="left"/> and <paramref name="right"/>.</returns>
    public virtual unsafe int CompareTo(byte* left, T right)
    {
        TempKey.Read(left);
        return TempKey.CompareTo(right);
    }

    #endregion

}
