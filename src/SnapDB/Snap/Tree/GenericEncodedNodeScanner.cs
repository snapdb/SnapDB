//******************************************************************************************************
//  GenericEncodedNodeScanner.cs - Gbtc
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
//  05/07/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//     
//  09/22/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Encoding;
using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Tree;

/// <summary>
/// Base class for reading from a node that is encoded and must be read sequentially through the node.
/// </summary>
/// <typeparam name="TKey">The type of keys stored in the nodes.</typeparam>
/// <typeparam name="TValue">The type of values stored in the nodes.</typeparam>
public unsafe class GenericEncodedNodeScanner<TKey, TValue>
        : SortedTreeScannerBase<TKey, TValue>
    where TKey : SnapTypeBase<TKey>, new()
    where TValue : SnapTypeBase<TValue>, new()
{
    private readonly PairEncodingBase<TKey, TValue> m_encoding;
    private readonly TKey m_prevKey;
    private readonly TValue m_prevValue;
    private int m_nextOffset;
    private readonly TKey m_tmpKey;
    private readonly TValue m_tmpValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEncodedNodeScanner{TKey, TValue}"/> class with the specified encoding, node level, block size, binary stream, and key lookup function.
    /// </summary>
    /// <param name="encoding">The encoding method used for key and value pairs.</param>
    /// <param name="level">The level of the node (0 for leaf nodes).</param>
    /// <param name="blockSize">The size of a block or node in bytes.</param>
    /// <param name="stream">The binary stream from which to read data.</param>
    /// <param name="lookupKey">A function for looking up keys based on an input key and direction.</param>
    public GenericEncodedNodeScanner(PairEncodingBase<TKey, TValue> encoding, byte level, int blockSize, BinaryStreamPointerBase stream, Func<TKey, byte, uint> lookupKey)
        : base(level, blockSize, stream, lookupKey)
    {
        m_encoding = encoding;
        m_nextOffset = 0;
        m_prevKey = new TKey();
        m_prevValue = new TValue();
        m_prevKey.Clear();
        m_prevValue.Clear();
        m_tmpKey = new TKey();
        m_tmpValue = new TValue();
    }

    /// <summary>
    /// Reads and decodes the next key-value pair at the current position for peeking without advancing the position.
    /// </summary>
    /// <param name="key">The key to be populated with the peeked key data.</param>
    /// <param name="value">The value to be populated with the peeked value data.</param>
    protected override void InternalPeek(TKey key, TValue value)
    {
        byte* stream = Pointer + m_nextOffset;
        m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out _);
    }

    /// <summary>
    /// Reads and decodes the next key-value pair at the current position and advances the position.
    /// </summary>
    /// <param name="key">The key to be populated with the read key data.</param>
    /// <param name="value">The value to be populated with the read value data.</param>
    protected override void InternalRead(TKey key, TValue value)
    {
        byte* stream = Pointer + m_nextOffset;
        int length = m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out _);
        key.CopyTo(m_prevKey);
        value.CopyTo(m_prevValue);
        m_nextOffset += length;
        IndexOfNextKeyValue++;
    }

    /// <summary>
    /// Reads and decodes the next key-value pair at the current position, advances the position, and checks if it matches the specified filter.
    /// </summary>
    /// <param name="key">The key to be populated with the read key data.</param>
    /// <param name="value">The value to be populated with the read value data.</param>
    /// <param name="filter">An optional filter to check if the key-value pair matches certain criteria.</param>
    /// <returns><c>true</c> if the key-value pair matches the filter; otherwise, <c>false</c>.</returns>
    protected override bool InternalRead(TKey key, TValue value, MatchFilterBase<TKey, TValue>? filter)
    {
    TryAgain:
        byte* stream = Pointer + m_nextOffset;
        int length = m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out _);
        key.CopyTo(m_prevKey);
        value.CopyTo(m_prevValue);
        m_nextOffset += length;
        IndexOfNextKeyValue++;

        if (filter.Contains(key, value))
            return true;

        if (IndexOfNextKeyValue >= RecordCount)
            return false;

        goto TryAgain;
    }

    /// <summary>
    /// Reads and decodes the next key-value pair at the current position, advances the position,
    /// and continues reading as long as the key is less than the specified upper bounds.
    /// </summary>
    /// <param name="key">The key to be populated with the read key data.</param>
    /// <param name="value">The value to be populated with the read value data.</param>
    /// <param name="upperBounds">The upper bounds for keys, reading continues as long as the key is less than this value.</param>
    /// <returns><c>true</c> if the key-value pair matches the condition; otherwise, <c>false</c>.</returns>
    protected override bool InternalReadWhile(TKey key, TValue value, TKey upperBounds)
    {
        byte* stream = Pointer + m_nextOffset;
        int length = m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out _);

        if (key.IsLessThan(upperBounds))
        {
            key.CopyTo(m_prevKey);
            value.CopyTo(m_prevValue);
            m_nextOffset += length;
            IndexOfNextKeyValue++;

            return true;
        }
        return false;
    }

    /// <summary>
    /// Reads and decodes the next key-value pair at the current position, advances the position,
    /// and continues reading as long as the key is less than the specified upper bounds and matches the filter condition.
    /// </summary>
    /// <param name="key">The key to be populated with the read key data.</param>
    /// <param name="value">The value to be populated with the read value data.</param>
    /// <param name="upperBounds">The upper bounds for keys, reading continues as long as the key is less than this value.</param>
    /// <param name="filter">An optional filter to apply to the read key-value pairs.</param>
    /// <returns>True if the key-value pair matches the condition; otherwise, false.</returns>
    protected override bool InternalReadWhile(TKey key, TValue value, TKey upperBounds, MatchFilterBase<TKey, TValue>? filter)
    {
    TryAgain:

        byte* stream = Pointer + m_nextOffset;
        int length = m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out _);

        if (key.IsLessThan(upperBounds))
        {
            key.CopyTo(m_prevKey);
            value.CopyTo(m_prevValue);
            m_nextOffset += length;
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
    /// Finds the specified key within the node and positions the scanner at the first key-value pair that matches or is greater than the specified key.
    /// </summary>
    /// <param name="key">The key to find within the node.</param>
    protected override void FindKey(TKey key)
    {
        OnNoadReload();
        while (IndexOfNextKeyValue < RecordCount && InternalReadWhile(m_tmpKey, m_tmpValue, key))
        {
        }
    }

    /// <summary>
    /// Occurs when a node's data is reset.
    /// Derived classes can override this 
    /// method if fields need to be reset when a node is loaded.
    /// </summary>
    protected override void OnNoadReload()
    {
        m_nextOffset = 0;
        m_prevKey.Clear();
        m_prevValue.Clear();
    }
}