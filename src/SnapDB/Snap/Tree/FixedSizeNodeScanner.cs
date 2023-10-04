//******************************************************************************************************
//  FixedSizeNodeScanner.cs - Gbtc
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
//  04/26/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Tree;

/// <summary>
/// The treescanner for a fixed size node.
/// </summary>
/// <typeparam name="TKey">The type of keys in the node.</typeparam>
/// <typeparam name="TValue">The type of values associated with keys in the node.</typeparam>
public class FixedSizeNodeScanner<TKey, TValue> : SortedTreeScannerBase<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly int m_keyValueSize;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// creates a new class
    /// </summary>
    /// <param name="level">The level of the fixed-size node in the sorted tree.</param>
    /// <param name="blockSize">The size of the block containing the fixed-size node.</param>
    /// <param name="stream">The binary stream pointer for navigating the tree structure.</param>
    /// <param name="lookupKey">A delegate function for looking up keys in the tree.</param>
    public FixedSizeNodeScanner(byte level, int blockSize, BinaryStreamPointerBase stream, Func<TKey, byte, uint> lookupKey) : base(level, blockSize, stream, lookupKey)
    {
        m_keyValueSize = KeySize + ValueSize;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Reads the next key-value pair from the internal byte buffer and advances the read pointer.
    /// </summary>
    /// <param name="key">The key to read the value into.</param>
    /// <param name="value">The value to read from the buffer.</param>
    /// <remarks>
    /// This method reads the next key-value pair from the internal byte buffer.
    /// It deserializes the key and value, advances the read pointer, and increments the index of the next key-value pair.
    /// </remarks>
    protected override unsafe void InternalRead(TKey key, TValue value)
    {
        byte* ptr = Pointer + IndexOfNextKeyValue * m_keyValueSize;
        key.Read(ptr);
        value.Read(ptr + KeySize);
        IndexOfNextKeyValue++;
    }

    /// <summary>
    /// Reads and filters key-value pairs from the internal byte buffer, advancing the read pointer.
    /// </summary>
    /// <param name="key">The key to read the value into.</param>
    /// <param name="value">The value to read from the buffer.</param>
    /// <param name="filter">Optional filter to determine if the key-value pair is accepted.</param>
    /// <returns>
    /// <c>true</c> if a matching key-value pair is found; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method reads key-value pairs from the internal byte buffer.
    /// It deserializes the key and value, advances the read pointer, and increments the index of the next key-value pair.
    /// If a <paramref name="filter"/> is provided, it is used to determine if the key-value pair should be accepted.
    /// If a matching pair is found, the method returns <c>true</c>; otherwise, it continues reading until a match is found or the end of the buffer is reached, returning <c>false</c>.
    /// </remarks>
    protected override unsafe bool InternalRead(TKey key, TValue value, MatchFilterBase<TKey, TValue>? filter)
    {
    TryAgain:
        byte* ptr = Pointer + IndexOfNextKeyValue * m_keyValueSize;
        key.Read(ptr);
        value.Read(ptr + KeySize);
        IndexOfNextKeyValue++;

        if (filter.Contains(key, value))
            return true;

        if (IndexOfNextKeyValue >= RecordCount)
            return false;

        goto TryAgain;
    }

    /// <summary>
    /// Peeks at the next key-value pair in the internal byte buffer without advancing the read pointer.
    /// </summary>
    /// <param name="key">The key to read the value into.</param>
    /// <param name="value">The value to read from the buffer.</param>
    /// <remarks>
    /// This method reads the key and value of the next key-value pair in the internal byte buffer.
    /// It deserializes the key and value without changing the state of the read pointer or index.
    /// The method is used for inspecting the next key-value pair in the buffer without consuming it.
    /// </remarks>
    protected override unsafe void InternalPeek(TKey key, TValue value)
    {
        byte* ptr = Pointer + IndexOfNextKeyValue * m_keyValueSize;
        key.Read(ptr);
        value.Read(ptr + KeySize);
    }

    /// <summary>
    /// Using <see cref="SortedTreeScannerBase{TKey,TValue}.Pointer"/> to advance to the next KeyValue.
    /// </summary>
    protected override unsafe bool InternalReadWhile(TKey key, TValue value, TKey upperBounds)
    {
        byte* ptr = Pointer + IndexOfNextKeyValue * m_keyValueSize;
        key.Read(ptr);
        value.Read(ptr + KeySize);
        if (key.IsLessThan(upperBounds))
        {
            IndexOfNextKeyValue++;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Using <see cref="SortedTreeScannerBase{TKey,TValue}.Pointer"/> to advance to the next KeyValue.
    /// </summary>
    protected override unsafe bool InternalReadWhile(TKey key, TValue value, TKey upperBounds, MatchFilterBase<TKey, TValue>? filter)
    {
    TryAgain:
        byte* ptr = Pointer + IndexOfNextKeyValue * m_keyValueSize;
        key.Read(ptr);
        value.Read(ptr + KeySize);

        if (key.IsLessThan(upperBounds))
        {
            IndexOfNextKeyValue++;

            if (filter.Contains(key, value))
                return true;

            if (IndexOfNextKeyValue >= RecordCount)
                return false;

            goto TryAgain;
        }

        return false;
    }

    /// <summary>
    /// Using <see cref="SortedTreeScannerBase{TKey,TValue}.Pointer"/> to advance to the search location of the provided <see cref="key"/>.
    /// </summary>
    /// <param name="key">The key to advance to.</param>
    protected override unsafe void FindKey(TKey key)
    {
        int offset = KeyMethods.BinarySearch(Pointer, key, RecordCount, m_keyValueSize);

        if (offset < 0)
            offset = ~offset;

        IndexOfNextKeyValue = offset;
    }

    #endregion
}