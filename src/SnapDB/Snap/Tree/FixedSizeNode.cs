//******************************************************************************************************
//  FixedSizeNode`2.cs - Gbtc
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
//  04/16/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Encoding;
using System;

namespace SnapDB.Snap.Tree;

/// <summary>
/// A node for a <see cref="SortedTree"/> that is encoded in a fixed width. 
/// This allows binary searches and faster writing.
/// </summary>
/// <typeparam name="TKey">The type of keys stored in the nodes.</typeparam>
/// <typeparam name="TValue">The type of values stored in the nodes.</typeparam>
public unsafe class FixedSizeNode<TKey, TValue>
    : SortedTreeNodeBase<TKey, TValue>
    where TKey : SnapTypeBase<TKey>, new()
    where TValue : SnapTypeBase<TValue>, new()
{
    private int m_maxRecordsPerNode;
    private readonly PairEncodingBase<TKey, TValue> m_encoding;

    /// <summary>
    /// Creates a new class.
    /// </summary>
    /// <param name="level">The level of this node.</param>
    public FixedSizeNode(byte level)
        : base(level)
    {
        m_encoding = Library.Encodings.GetEncodingMethod<TKey, TValue>(EncodingDefinition.FixedSizeCombinedEncoding);
    }

    /// <summary>
    /// Creates a new instance of the same node type as a clone with the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The level of the new node.</param>
    /// <returns>A new node instance with the same type and the specified level.</returns>
    public override SortedTreeNodeBase<TKey, TValue> Clone(byte level)
    {
        return new FixedSizeNode<TKey, TValue>(level);
    }

    /// <summary>
    /// Returns a <see cref="SortedTreeScannerBase{TKey,TValue}"/>
    /// </summary>
    /// <returns>A new tree scanner instance.</returns>
    public override SortedTreeScannerBase<TKey, TValue> CreateTreeScanner()
    {
        return new FixedSizeNodeScanner<TKey, TValue>(Level, BlockSize, Stream, SparseIndex.Get);
    }

    /// <summary>
    /// Initializes the type-specific properties of the node, such as the maximum records per node.
    /// </summary>
    /// <remarks>
    /// This method calculates the maximum number of records that can be stored in a node based on the block size and key-value size.
    /// It checks if the tree meets the minimum requirement of having at least 4 records per node and throws an exception if not.
    /// </remarks>
    protected override void InitializeType()
    {
        m_maxRecordsPerNode = (BlockSize - HeaderSize) / KeyValueSize;

        if (m_maxRecordsPerNode < 4)
            throw new Exception("Tree must have at least 4 records per node. Increase the block size or decrease the size of the records.");
    }

    /// <summary>
    /// Gets the maximum overhead (additional space used) when combining nodes.
    /// </summary>
    /// <remarks>
    /// This property specifies the maximum additional space that can be used when nodes are combined during tree operations.
    /// The value is typically 0, indicating that combining nodes doesn't introduce any additional overhead.
    /// </remarks>
    protected override int MaxOverheadWithCombineNodes => 0;

    /// <summary>
    /// Reads the value at the specified index in the node.
    /// </summary>
    /// <param name="index">The index at which to read the value.</param>
    /// <param name="value">The value to be read from the node.</param>
    /// <remarks>
    /// This method reads the value stored at the specified index within the node's storage.
    /// It is typically used during tree operations to retrieve values associated with keys.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown if the provided index is out of range.</exception>
    protected override void Read(int index, TValue value)
    {
        value.Read(GetReadPointerAfterHeader() + KeySize + index * KeyValueSize);
    }

    /// <summary>
    /// Reads the key-value pair at the specified index in the node.
    /// </summary>
    /// <param name="index">The index at which to read the key-value pair.</param>
    /// <param name="key">The key to be read from the node.</param>
    /// <param name="value">The value to be read from the node.</param>
    /// <remarks>
    /// This method reads both the key and value stored at the specified index within the node's storage.
    /// It is typically used during tree operations to retrieve key-value pairs.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown if the provided index is out of range.</exception>
    protected override void Read(int index, TKey key, TValue value)
    {
        byte* ptr = GetReadPointerAfterHeader() + index * KeyValueSize;
        m_encoding.Decode(ptr, null, null, key, value, out _);
        //key.Read(ptr);
        //value.Read(ptr + KeySize);
    }

    /// <summary>
    /// Removes a key-value pair at the specified index unless it would cause an overflow.
    /// </summary>
    /// <param name="index">The index of the key-value pair to be removed.</param>
    /// <returns>
    /// <c>true</c> if the key-value pair was successfully removed; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method removes the key-value pair at the specified index unless removing it would result in an overflow,
    /// in which case, it will not remove the pair. Overflow occurs when the node can't accommodate the removal
    /// while maintaining its size constraints.
    /// </remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown if the provided index is out of range.</exception>
    protected override bool RemoveUnlessOverflow(int index)
    {
        if (index != RecordCount - 1)
        {
            byte* start = GetWritePointerAfterHeader() + index * KeyValueSize;
            Memory.Copy(start + KeyValueSize, start, (RecordCount - index - 1) * KeyValueSize);
        }

        // Save the header
        RecordCount--;
        ValidBytes -= (ushort)KeyValueSize;

        return true;
    }

    /// <summary>
    /// Inserts a key-value pair at the specified index unless the node is already full.
    /// </summary>
    /// <param name="index">The index at which to insert the key-value pair.</param>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to insert.</param>
    /// <returns>
    /// <c>true</c> if the key-value pair was successfully inserted; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method inserts the specified key-value pair at the specified index within the node, unless
    /// the node is already full and can't accommodate the insertion while maintaining its size constraints.
    /// </remarks>
    protected override bool InsertUnlessFull(int index, TKey key, TValue value)
    {
        if (RecordCount >= m_maxRecordsPerNode)
            return false;

        byte* start = GetWritePointerAfterHeader() + index * KeyValueSize;

        if (index != RecordCount)
        {
            int count = (RecordCount - index) * KeyValueSize;
            Buffer.MemoryCopy(start, start + KeyValueSize, count, count);
        }

        //Insert the data
        m_encoding.Encode(start, null, null, key, value);
        // Key.Write(start);
        // Value.Write(start + KeySize);

        // Save the header
        IncrementOneRecord(KeyValueSize);
        return true;
    }

    /// <summary>
    /// Appends a sequential stream of key-value pairs to the node.
    /// </summary>
    /// <param name="stream">The stream containing key-value pairs to append.</param>
    /// <param name="isFull">A boolean indicating whether the node is full after the append operation.</param>
    /// <remarks>
    /// This method appends a sequential stream of key-value pairs to the node. It keeps adding pairs
    /// until the node reaches its maximum capacity. If the node becomes full, the <paramref name="isFull"/>
    /// parameter is set to <c>true</c>; otherwise, it's set to <c>false</c>.
    /// </remarks>
    protected override void AppendSequentialStream(InsertStreamHelper<TKey, TValue> stream, out bool isFull)
    {
        //isFull = false;
        //return;

        // ToDo: Figure out why this code does not work.

        if (RecordCount >= m_maxRecordsPerNode)
        {
            isFull = true;
            return;
        }

        int recordsAdded = 0;
        int additionalValidBytes = 0;

        byte* writePointer = GetWritePointerAfterHeader();
        int offset = RecordCount * KeyValueSize;

    TryAgain:
        if (!stream.IsValid || !stream.IsStillSequential)
        {
            isFull = false;
            IncrementRecordCounts(recordsAdded, additionalValidBytes);
            return;
        }

        if (stream.IsKvp1)
        {
            // Key1,Value1 are the current record
            if (RemainingBytes - additionalValidBytes < KeyValueSize)
            {
                isFull = true;
                IncrementRecordCounts(recordsAdded, additionalValidBytes);
                return;
            }

            stream.Key1.Write(writePointer + offset);
            stream.Value1.Write(writePointer + offset + KeySize);
            additionalValidBytes += KeyValueSize;
            recordsAdded++;
            offset += KeyValueSize;

            // Inlined stream.Next()
            stream.IsValid = stream.Stream.Read(stream.Key2, stream.Value2);
            stream.IsStillSequential = stream.Key1.IsLessThan(stream.Key2);
            stream.IsKvp1 = false;
            // End Inlined
            goto TryAgain;
        }
        else
        {
            // Key2,Value2 are the current record
            if (RemainingBytes - additionalValidBytes < KeyValueSize)
            {
                isFull = true;
                IncrementRecordCounts(recordsAdded, additionalValidBytes);

                return;
            }

            stream.Key2.Write(writePointer + offset);
            stream.Value2.Write(writePointer + offset + KeySize);
            additionalValidBytes += KeyValueSize;
            recordsAdded++;
            offset += KeyValueSize;

            // Inlined stream.Next()
            stream.IsValid = stream.Stream.Read(stream.Key1, stream.Value1);
            stream.IsStillSequential = stream.Key2.IsLessThan(stream.Key1);
            stream.IsKvp1 = true;
            // End Inlined

            goto TryAgain;
        }
    }

    /// <summary>
    /// Searches for the index of the specified key within the node.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// The index of the specified key within the node, or a negative value if the key is not found.
    /// </returns>
    /// <remarks>
    /// This method performs a binary search to find the index of the specified key within the node's keys.
    /// If the key is found, its index is returned; otherwise, a negative value is returned to indicate that
    /// the key is not present in the node.
    /// </remarks>
    protected override int GetIndexOf(TKey key)
    {
        return KeyMethods.BinarySearch(GetReadPointerAfterHeader(), key, RecordCount, KeyValueSize);
    }

    /// <summary>
    /// Splits the node into two nodes and redistributes its key-value pairs.
    /// </summary>
    /// <param name="newNodeIndex">The index of the new node created during the split.</param>
    /// <param name="dividingKey">The key that determines the split point.</param>
    /// <remarks>
    /// This method splits the current node into two nodes and redistributes its key-value pairs.
    /// The splitting point is determined by the <paramref name="dividingKey"/>. Half of the records
    /// remain in the original node, and the other half is moved to the new node.
    /// </remarks>
    protected override void Split(uint newNodeIndex, TKey dividingKey)
    {
        // Determine how many entries to shift on the split.
        int recordsInTheFirstNode = RecordCount >> 1; // divide by 2.
        int recordsInTheSecondNode = RecordCount - recordsInTheFirstNode;

        long sourceStartingAddress = StartOfDataPosition + KeyValueSize * recordsInTheFirstNode;
        long targetStartingAddress = newNodeIndex * BlockSize + HeaderSize;

        // lookup the dividing key
        dividingKey.Read(Stream.GetReadPointer(sourceStartingAddress, KeySize));

        // do the copy
        Stream.Copy(sourceStartingAddress, targetStartingAddress, recordsInTheSecondNode * KeyValueSize);

        // Create the header of the second node.
        CreateNewNode(newNodeIndex, (ushort)recordsInTheSecondNode,
                      (ushort)(HeaderSize + recordsInTheSecondNode * KeyValueSize),
                      NodeIndex, RightSiblingNodeIndex, dividingKey, UpperKey);

        // update the node that was the old right sibling
        if (RightSiblingNodeIndex != uint.MaxValue)
            SetLeftSiblingProperty(RightSiblingNodeIndex, NodeIndex, newNodeIndex);

        // update the original header
        RecordCount = (ushort)recordsInTheFirstNode;
        ValidBytes = (ushort)(HeaderSize + recordsInTheFirstNode * KeyValueSize);
        RightSiblingNodeIndex = newNodeIndex;
        UpperKey = dividingKey;
    }

    /// <summary>
    /// Transfers a specified number of key-value records from the right node to the left node during a redistribution operation.
    /// </summary>
    /// <param name="left">The left node to which records are transferred.</param>
    /// <param name="right">The right node from which records are transferred.</param>
    /// <param name="bytesToTransfer">The total number of bytes to transfer, including header bytes.</param>
    /// <remarks>
    /// This method is used during a redistribution operation between two nodes. It transfers a specified number of key-value
    /// records, as well as updates record counts and valid bytes for both nodes. The records are moved from the right node
    /// to the left node while preserving their order.
    /// </remarks>
    protected override void TransferRecordsFromRightToLeft(Node<TKey> left, Node<TKey> right, int bytesToTransfer)
    {
        int recordsToTransfer = (bytesToTransfer - HeaderSize) / KeyValueSize;
        //Transfer records from Right to Left
        long sourcePosition = right.NodePosition + HeaderSize;
        long destinationPosition = left.NodePosition + HeaderSize + left.RecordCount * KeyValueSize;
        Stream.Copy(sourcePosition, destinationPosition, KeyValueSize * recordsToTransfer);

        //Removes empty spaces from records on the right.
        Stream.Position = right.NodePosition + HeaderSize;
        Stream.RemoveBytes(recordsToTransfer * KeyValueSize, (right.RecordCount - recordsToTransfer) * KeyValueSize);

        //Update number of records.
        left.RecordCount += (ushort)recordsToTransfer;
        left.ValidBytes += (ushort)(recordsToTransfer * KeyValueSize);
        right.RecordCount -= (ushort)recordsToTransfer;
        right.ValidBytes -= (ushort)(recordsToTransfer * KeyValueSize);
    }

    /// <summary>
    /// Transfers a specified number of key-value records from the left node to the right node during a redistribution operation.
    /// </summary>
    /// <param name="left">The left node from which records are transferred.</param>
    /// <param name="right">The right node to which records are transferred.</param>
    /// <param name="bytesToTransfer">The total number of bytes to transfer, including header bytes.</param>
    /// <remarks>
    /// This method is used during a redistribution operation between two nodes. It transfers a specified number of key-value
    /// records, as well as updates record counts and valid bytes for both nodes. The records are moved from the left node
    /// to the right node while preserving their order.
    /// </remarks>
    protected override void TransferRecordsFromLeftToRight(Node<TKey> left, Node<TKey> right, int bytesToTransfer)
    {
        int recordsToTransfer = (bytesToTransfer - HeaderSize) / KeyValueSize;
        // Shift existing records to make room for copy
        Stream.Position = right.NodePosition + HeaderSize;
        Stream.InsertBytes(recordsToTransfer * KeyValueSize, right.RecordCount * KeyValueSize);

        // Transfer records from Left to Right
        long sourcePosition = left.NodePosition + HeaderSize + (left.RecordCount - recordsToTransfer) * KeyValueSize;
        long destinationPosition = right.NodePosition + HeaderSize;
        Stream.Copy(sourcePosition, destinationPosition, KeyValueSize * recordsToTransfer);

        // Update number of records.
        left.RecordCount -= (ushort)recordsToTransfer;
        left.ValidBytes -= (ushort)(recordsToTransfer * KeyValueSize);
        right.RecordCount += (ushort)recordsToTransfer;
        right.ValidBytes += (ushort)(recordsToTransfer * KeyValueSize);
    }
}