﻿//******************************************************************************************************
//  SortedTreeScannerBase`2.cs - Gbtc
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
//  03/19/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//     
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Tree;

/// <summary>
/// Base class for reading from any implementation of a sorted trees.
/// </summary>
/// <typeparam name="TKey">The key type to use in the <see cref="SortedTreeScannerBase{TKey, TValue}"/>.</typeparam>
/// <typeparam name="TValue">The value type to use in the <see cref="SortedTreeScannerBase{TKey, TValue}"/>.</typeparam>
public abstract unsafe class SortedTreeScannerBase<TKey, TValue> : SeekableTreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private const int IndexSize = sizeof(uint);
    private const int OffsetOfLeftSibling = OffsetOfValidBytes + sizeof(ushort);
    private const int OffsetOfLowerBounds = OffsetOfRightSibling + IndexSize;
    private const int OffsetOfNodeLevel = OffsetOfVersion + sizeof(byte);
    private const int OffsetOfRecordCount = OffsetOfNodeLevel + sizeof(byte);
    private const int OffsetOfRightSibling = OffsetOfLeftSibling + IndexSize;
    private const int OffsetOfValidBytes = OffsetOfRecordCount + sizeof(ushort);

    private const int OffsetOfVersion = 0;


    /// <summary>
    /// Represents the size of the block within the node.
    /// </summary>
    protected readonly int BlockSize;

    /// <summary>
    /// The index number of the next key/value that needs to be read.
    /// The valid range of this field is [0, RecordCount - 1].
    /// </summary>
    protected int IndexOfNextKeyValue;

    // private TKey m_lowerKey;
    // private TKey m_upperKey;
    /// <summary>
    /// Represents the custom methods for the keys used in this context.
    /// </summary>
    protected SnapTypeCustomMethods<TKey> KeyMethods;

    /// <summary>
    /// Represents the size, in bytes, of the keys used in this context.
    /// </summary>
    protected int KeySize;

    /// <summary>
    /// Represents the lower bound key used in this context.
    /// </summary>
    protected TKey LowerKey = new();

    /// <summary>
    /// Represents the stream used in this context.
    /// </summary>
    protected readonly BinaryStreamPointerBase Stream;

    /// <summary>
    /// Represents the upper bound key used in this context.
    /// </summary>
    protected TKey UpperKey = new();

    /// <summary>
    /// Represents the size, in bytes, of the values used in this context.
    /// </summary>
    protected int ValueSize;

    private readonly byte m_level;
    private readonly Func<TKey, byte, uint> m_lookupKey;
    private readonly TKey m_tempKey;

    #endregion

    #region [ Constructors ]

    // protected int OffsetOfUpperBounds;
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEncodedNodeScanner{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="level">The level of the node within the tree.</param>
    /// <param name="blockSize">The size of each block within the node.</param>
    /// <param name="stream">The binary stream to read data from.</param>
    /// <param name="lookupKey">A function for looking up a key.</param>
    protected SortedTreeScannerBase(byte level, int blockSize, BinaryStreamPointerBase stream, Func<TKey, byte, uint> lookupKey)
    {
        m_tempKey = new TKey();
        // m_lowerKey = new TKey();
        // m_upperKey = new TKey();
        m_lookupKey = lookupKey;
        m_level = level;

        // m_currentNode = new Node(stream, blockSize);
        KeyMethods = m_tempKey.CreateValueMethods();
        KeySize = new TKey().Size;
        ValueSize = new TValue().Size;

        // OffsetOfUpperBounds = OffsetOfLowerBounds + KeySize;
        HeaderSize = OffsetOfLowerBounds + 2 * KeySize;
        BlockSize = blockSize;
        Stream = stream;
        PointerVersion = -1;
        IndexOfNextKeyValue = 0;
        RecordCount = 0;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets if the stream is always in sequential order. Do not return <c>true</c> unless it is guaranteed that
    /// the data read from this stream is sequential.
    /// </summary>
    public override bool IsAlwaysSequential => true;

    /// <summary>
    /// Gets if the stream will never return duplicate keys. Do not return true unless it is guaranteed that
    /// the data read from this stream will never contain duplicates.
    /// </summary>
    public override bool NeverContainsDuplicates => true;

    /// <summary>
    /// The number of bytes in the header of any given node.
    /// </summary>
    protected int HeaderSize { get; }

    /// <summary>
    /// The node index of the previous sibling.
    /// uint.MaxValue means there is no sibling to the right.
    /// </summary>
    protected uint LeftSiblingNodeIndex { get; private set; }

    /// <summary>
    /// The index of the current node.
    /// </summary>
    protected uint NodeIndex { get; private set; }

    /// <summary>
    /// The pointer that is right after the header of the node.
    /// </summary>
    protected byte* Pointer { get; private set; }

    /// <summary>
    /// The pointer version of the <see cref="Pointer"/>.
    /// Compare to Stream.PointerVersion to find out if
    /// this pointer is current.
    /// </summary>
    protected long PointerVersion { get; private set; }

    /// <summary>
    /// The number of records in the current node.
    /// </summary>
    protected ushort RecordCount { get; private set; }

    /// <summary>
    /// The node index of the next sibling.
    /// uint.MaxValue means there is no sibling to the right.
    /// </summary>
    protected uint RightSiblingNodeIndex { get; private set; }

    /// <summary>
    /// Gets the byte offset of the upper bounds key.
    /// </summary>
    private int OffsetOfUpperBounds => OffsetOfLowerBounds + KeySize;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Seeks to the start of SortedTree.
    /// </summary>
    public virtual void SeekToStart()
    {
        m_tempKey.SetMin();
        SeekToKey(m_tempKey);
    }

    /// <summary>
    /// Seeks the stream to the first value greater than or equal to <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to seek to.</param>
    public override void SeekToKey(TKey key)
    {
        LoadNode(FindLeafNodeAddress(key));
        FindKey(key);
    }

    /// <summary>
    /// Reads the next point, but doees not advance the position of the stream.
    /// </summary>
    /// <param name="key">the key to write the results to</param>
    /// <param name="value">the value to write the results to</param>
    /// <returns>
    /// <c>true</c> if a point is found; otherwise, the end of the stream has been encountered and <c>false</c> is returned.
    /// </returns>
    public bool Peek(TKey key, TValue value)
    {
        if (Stream.PointerVersion == PointerVersion)
            // A light weight function that can be called quickly since 99% of the time, this logic statement will return successfully.
            if (IndexOfNextKeyValue < RecordCount)
            {
                InternalPeek(key, value);
                return true;
            }

        return PeekCatchAll(key, value);
    }

    /// <summary>
    /// Continues to advance the stream
    /// but stops short of returning the point that is equal to
    /// the provided key.
    /// </summary>
    /// <param name="key">Where to store the key.</param>
    /// <param name="value">Where to store the value.</param>
    /// <param name="upperBounds">
    /// The test condition. Will return <c>false</c> if the returned point would have
    /// exceeded this value.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the point returned is valid; otherwise, <c>false</c> if:
    /// The point read is greater than or equal to <paramref name="upperBounds"/> or 
    /// The end of the stream is reached or 
    /// The end of the current node has been reached.
    /// </returns>
    public virtual bool ReadWhile(TKey key, TValue value, TKey upperBounds)
    {
        if (Stream.PointerVersion == PointerVersion)
            // A light weight function that can be called quickly since 99% of the time, this logic statement will return successfully.
            if (IndexOfNextKeyValue < RecordCount)
            {
                if (UpperKey.IsLessThan(upperBounds))
                {
                    InternalRead(key, value);
                    return true;
                }

                return InternalReadWhile(key, value, upperBounds);
            }

        return ReadWhileCatchAll(key, value, upperBounds);
    }


    /// <summary>
    /// Using the provided filter, continues to advance the stream
    /// but stops short of returning the point that is equal to
    /// the provided key.
    /// </summary>
    /// <param name="key">Where to store the key.</param>
    /// <param name="value">Where to store the value.</param>
    /// <param name="upperBounds">
    /// the test condition. Will return <c>false</c> if the returned point would have
    /// exceedd this value
    /// </param>
    /// <param name="filter">The filter to apply to the reading.</param>
    /// <returns>
    /// Returns <c>true</c> if the point returned is valid; otherwise, <c>false</c> if:
    /// The point read is greater than or equal to <paramref name="upperBounds"/> or  
    /// The end of the stream is reached or 
    /// The end of the current node has been reached.
    /// </returns>
    public virtual bool ReadWhile(TKey key, TValue value, TKey upperBounds, MatchFilterBase<TKey, TValue>? filter)
    {
        if (Stream.PointerVersion == PointerVersion && IndexOfNextKeyValue < RecordCount)
        {
            if (UpperKey.IsLessThan(upperBounds))
                return InternalRead(key, value, filter);
            return InternalReadWhile(key, value, upperBounds, filter);
        }

        return ReadWhileCatchAll(key, value, upperBounds, filter);
    }

    /// <summary>
    /// Reads the key-value pair backwardish (in reverse order) from the tree node.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="value">The value to read.</param>
    /// <returns>
    ///   <c>true</c> if there are more records to read; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Note: This method will read each leaf node in reverse order. However, each leaf itself will be sorted
    /// ascending, only when moving from leaf node to leave node will this occur in reverse order.
    /// Note: This functionality should be used only to inspect the contests of a file, and not be
    /// used to attempt supporting reverse readings of files.
    /// </remarks>
    public bool ReadBackwardish(TKey key, TValue value)
    {
        // If there are no more records in the current node.
        if (IndexOfNextKeyValue >= RecordCount)
        {
            // If the last leaf node, return false
            if (LeftSiblingNodeIndex == uint.MaxValue)
            {
                key.Clear();
                value.Clear();
                Dispose();
                return false;
            }

            LoadNode(LeftSiblingNodeIndex);
        }

        // If the pointer data is no longer valid, refresh the pointer.
        if (Stream.PointerVersion != PointerVersion)
            RefreshPointer();
        // Reads the next key in the sequence.
        InternalRead(key, value);

        return true;
    }

    /// <summary>
    /// Peeks at the next key and value in the node without advancing the cursor.
    /// </summary>
    /// <param name="key">The key to peek into.</param>
    /// <param name="value">The value to peek into.</param>
    protected abstract void InternalPeek(TKey key, TValue value);

    /// <summary>
    /// Reads the next key and value from the node and advances the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    protected abstract void InternalRead(TKey key, TValue value);

    /// <summary>
    /// Reads the next key and value from the node while applying a filter and advances the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <param name="filter">The filter to apply during reading.</param>
    /// <returns><c>true</c> if the read key and value pass the filter; otherwise, <c>false</c>.</returns>
    protected abstract bool InternalRead(TKey key, TValue value, MatchFilterBase<TKey, TValue>? filter);

    /// <summary>
    /// Reads the next key and value from the node while the key is less than the upper bounds and advances the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <param name="upperBounds">The upper bounds for key comparison.</param>
    /// <returns><c>true</c> if the read key and value meet the condition; otherwise, <c>false</c>.</returns>
    protected abstract bool InternalReadWhile(TKey key, TValue value, TKey upperBounds);

    /// <summary>
    /// Reads the next key and value from the node while applying a filter and the key is less than the upper bounds, and advances the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <param name="upperBounds">The upper bounds for key comparison.</param>
    /// <param name="filter">The filter to apply during reading.</param>
    /// <returns><c>true</c> if the read key and value pass the filter and meet the condition; otherwise, <c>false</c>.</returns>
    protected abstract bool InternalReadWhile(TKey key, TValue value, TKey upperBounds, MatchFilterBase<TKey, TValue>? filter);

    /// <summary>
    /// Using <see cref="Pointer"/> advance to the search location of the provided <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to advance to.</param>
    protected abstract void FindKey(TKey key);

    /// <summary>
    /// Gets the block index when seeking for the provided key.
    /// </summary>
    /// <param name="key">The key to start the search from.</param>
    /// <returns>The resultant block index found after seeking for the provided key.</returns>
    protected uint FindLeafNodeAddress(TKey key)
    {
        return m_lookupKey(key, m_level);
    }

    /// <summary>
    /// Occurs when a node's data is reset.
    /// Derived classes can override this
    /// method if fields need to be reset when a node is loaded.
    /// </summary>
    protected virtual void OnNoadReload()
    {
    }

    /// <summary>
    /// Peeks at the next key and value in the node without advancing the cursor and handles various cases.
    /// </summary>
    /// <param name="key">The key to peek into.</param>
    /// <param name="value">The value to peek into.</param>
    /// <returns>True if the peek was successful and there are more records; otherwise, false.</returns>
    protected bool PeekCatchAll(TKey key, TValue value)
    {
        // If there are no more records in the current node.
        if (IndexOfNextKeyValue >= RecordCount)
        {
            // If the last leaf node, return <c>false</c>.
            if (RightSiblingNodeIndex == uint.MaxValue)
            {
                key.Clear();
                value.Clear();
                Dispose();

                return false;
            }

            LoadNode(RightSiblingNodeIndex);
        }

        // If the pointer data is no longer valid, refresh the pointer.
        if (Stream.PointerVersion != PointerVersion)
            RefreshPointer();
        // Reads the next key in the sequence.
        InternalPeek(key, value);
        return true;
    }

    /// <summary>
    /// Reads key-value pairs in the node while the key is less than the upper bounds,
    /// handling various cases and advancing the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <param name="upperBounds">The upper bounds for key comparison.</param>
    /// <returns><c>true</c> if there are more records matching the condition; otherwise, <c>false</c>.</returns>
    protected bool ReadWhileCatchAll(TKey key, TValue value, TKey upperBounds)
    {
        // If there are no more records in the current node.
        if (IndexOfNextKeyValue >= RecordCount)
        {
            // If the last leaf node, return false
            if (RightSiblingNodeIndex == uint.MaxValue)
            {
                key.Clear();
                value.Clear();
                Dispose();
                return false;
            }

            LoadNode(RightSiblingNodeIndex);
        }

        // If the pointer data is no longer valid, refresh the pointer.
        if (Stream.PointerVersion != PointerVersion)
            RefreshPointer();
        // Reads the next key in the sequence.
        if (UpperKey.IsLessThan(upperBounds))
        {
            InternalRead(key, value);
            return true;
        }

        return InternalReadWhile(key, value, upperBounds);
    }

    /// <summary>
    /// Reads key-value pairs in the node while the key is less than the upper bounds,
    /// applying an optional filter, and advancing the cursor.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <param name="upperBounds">The upper bounds for key comparison.</param>
    /// <param name="filter">An optional filter to apply to matching records.</param>
    /// <returns><c>true</c> if there are more records matching the condition; otherwise, <c>false</c>.</returns>
    protected bool ReadWhileCatchAll(TKey key, TValue value, TKey upperBounds, MatchFilterBase<TKey, TValue>? filter)
    {
        // If there are no more records in the current node.
        if (IndexOfNextKeyValue >= RecordCount)
        {
            // If the last leaf node, return false
            if (RightSiblingNodeIndex == uint.MaxValue)
            {
                key.Clear();
                value.Clear();
                Dispose();

                return false;
            }

            LoadNode(RightSiblingNodeIndex);
        }

        // If the pointer data is no longer valid, refresh the pointer
        if (Stream.PointerVersion != PointerVersion)
            RefreshPointer();

        // Reads the next key in the sequence.
        if (UpperKey.IsLessThan(upperBounds))
            return InternalRead(key, value, filter);

        return InternalReadWhile(key, value, upperBounds, filter);
    }

    /// <summary>
    /// Advances the stream to the next value.
    /// If before the beginning of the stream, advances to the first value.
    /// </summary>
    /// <param name="key">The key to advance to.</param>
    /// <param name="value">The value associated with the key to advance to.</param>
    /// <returns><c>true</c> if the advance was successful; otherwise, <c>false</c> if the end of the stream was reached.</returns>
    protected override bool ReadNext(TKey key, TValue value)
    {
        if (Stream.PointerVersion == PointerVersion)
            // A light weight function that can be called quickly since 99% of the time, this logic statement will return successfully.
            if (IndexOfNextKeyValue < RecordCount)
            {
                InternalRead(key, value);
                return true;
            }

        return ReadCatchAll(key, value);
    }

    /// <summary>
    /// A catch all read function.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="value">The value associated with the key to read.</param>
    /// <returns><c>true</c> if the advance was successful; otherwise, <c>false</c> to indicate that the end of stream has been reached.</returns>
    protected bool ReadCatchAll(TKey key, TValue value)
    {
        // If there are no more records in the current node.
        if (IndexOfNextKeyValue >= RecordCount)
        {
            // If the last leaf node, return false
            if (RightSiblingNodeIndex == uint.MaxValue)
            {
                key.Clear();
                value.Clear();
                Dispose();
                return false;
            }

            LoadNode(RightSiblingNodeIndex);
        }

        // If the pointer data is no longer valid, refresh the pointer.
        if (Stream.PointerVersion != PointerVersion)
            RefreshPointer();
        // Reads the next key in the sequence.
        InternalRead(key, value);
        return true;
    }

    /// <summary>
    /// Loads the header data for the provided node.
    /// </summary>
    /// <param name="index">The node index.</param>
    /// <exception cref="ArgumentNullException">
    /// occurs when <paramref name="index"/>
    /// is equal to uint.MaxValue.
    /// </exception>
    private void LoadNode(uint index)
    {
        if (index == uint.MaxValue)
            throw new ArgumentNullException(nameof(index), "Cannot be uint.MaxValue. Which is null.");

        NodeIndex = index;

        RefreshPointer();

        byte* ptr = Pointer - HeaderSize;
        if (ptr[OffsetOfNodeLevel] != m_level)
            throw new Exception("This node is not supposed to access the underlying node level.");

        RecordCount = *(ushort*)(ptr + OffsetOfRecordCount);
        LeftSiblingNodeIndex = *(uint*)(ptr + OffsetOfLeftSibling);
        RightSiblingNodeIndex = *(uint*)(ptr + OffsetOfRightSibling);
        LowerKey.Read(ptr + OffsetOfLowerBounds);
        UpperKey.Read(ptr + OffsetOfUpperBounds);
        IndexOfNextKeyValue = 0;
        OnNoadReload();
    }

    /// <summary>
    /// Gets the pointer for the provided block.
    /// </summary>
    private void RefreshPointer()
    {
        Pointer = Stream.GetReadPointer(NodeIndex * BlockSize, BlockSize) + HeaderSize;
        PointerVersion = Stream.PointerVersion;
    }

    #endregion
}