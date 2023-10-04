//******************************************************************************************************
//  GenericEncodedNode`2.cs - Gbtc
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
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Encoding;

namespace SnapDB.Snap.Tree;

/// <summary>
/// A TreeNode abstract class that is used for linearly encoding a class.
/// </summary>
/// <typeparam name="TKey">The type of keys stored in the nodes.</typeparam>
/// <typeparam name="TValue">The type of values stored in the nodes.</typeparam>
public unsafe class GenericEncodedNode<TKey, TValue> : SortedTreeNodeBase<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private byte[] m_buffer1;
    private byte[] m_buffer2;
    private int m_currentIndex;

    private readonly TKey m_currentKey;
    private int m_currentOffset;
    private readonly TValue m_currentValue;
    private readonly PairEncodingBase<TKey, TValue> m_encoding;
    private int m_maximumStorageSize;
    private int m_nextOffset;
    private readonly TKey m_nullKey;
    private readonly TValue m_nullValue;
    private readonly TKey m_prevKey;
    private readonly TValue m_prevValue;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericEncodedNode{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="encoding">The encoding method used for key-value pairs in the node.</param>
    /// <param name="level">The level of the node within the tree structure.</param>
    /// <remarks>
    /// This constructor creates a new node with the specified level and initializes key and value instances for use in the node.
    /// It associates the provided encoding method with the node and sets up event handlers for node index changes and cache clearing.
    /// The level should typically be 0, as this type of node is typically used at the leaf level of the tree.
    /// </remarks>
    public GenericEncodedNode(PairEncodingBase<TKey, TValue> encoding, byte level) : base(level)
    {
        if (level != 0)
            throw new ArgumentException("Level for this type is only supported on the leaf level.");
        m_currentKey = new TKey();
        m_currentValue = new TValue();
        m_prevKey = new TKey();
        m_prevValue = new TValue();
        m_nullKey = new TKey();
        m_nullValue = new TValue();
        m_nullKey.Clear();
        m_nullValue.Clear();

        NodeIndexChanged += OnNodeIndexChanged;
        ClearNodeCache();

        m_encoding = encoding;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the maximum overhead expected when combining two nodes of this type.
    /// </summary>
    /// <remarks>
    /// The maximum overhead includes the storage required for the data in both nodes, plus one additional byte.
    /// This property is used in node combining operations to estimate the maximum storage needed for the resulting node.
    /// </remarks>
    protected override int MaxOverheadWithCombineNodes => MaximumStorageSize * 2 + 1;

    /// <summary>
    /// Gets the maximum storage size, in bytes, required for encoding a single key-value pair.
    /// </summary>
    /// <remarks>
    /// This property returns the maximum storage size, in bytes, required to encode a single key-value pair
    /// using the configured encoding method. It reflects the maximum amount of space that a single pair
    /// can occupy in the data store, which can be useful for calculating storage requirements.
    /// </remarks>
    protected int MaximumStorageSize => m_encoding.MaxCompressionSize;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a new instance of <see cref="GenericEncodedNode{TKey, TValue}"/> as a clone of the current node.
    /// </summary>
    /// <param name="level">The level of the new node within the tree structure.</param>
    /// <returns>A new node instance cloned from the current node with the specified level.</returns>
    /// <remarks>
    /// This method is used to create a clone of the current node, replicating its contents and configuration.
    /// The clone has the same encoding method as the original node, and the specified level within the tree structure.
    /// </remarks>
    public override SortedTreeNodeBase<TKey, TValue> Clone(byte level)
    {
        return new GenericEncodedNode<TKey, TValue>(m_encoding.Clone(), level);
    }

    /// <summary>
    /// Creates a tree scanner specific to <see cref="GenericEncodedNode{TKey, TValue}"/> nodes.
    /// </summary>
    /// <returns>A new instance of a tree scanner designed to work with nodes of the current type.</returns>
    /// <remarks>
    /// This method is used to create a tree scanner specifically tailored to work with nodes of the <see cref="GenericEncodedNode{TKey, TValue}"/> type.
    /// The scanner is configured with the current node's encoding method, level, block size, associated stream, and sparse index retrieval function.
    /// </remarks>
    public override SortedTreeScannerBase<TKey, TValue> CreateTreeScanner()
    {
        return new GenericEncodedNodeScanner<TKey, TValue>(m_encoding, Level, BlockSize, Stream, SparseIndex.Get);
    }

    /// <summary>
    /// Encodes a key-value record into a binary stream.
    /// </summary>
    /// <param name="stream">A pointer to the binary stream where the record will be encoded.</param>
    /// <param name="prevKey">The previous key in the sequence.</param>
    /// <param name="prevValue">The previous value in the sequence.</param>
    /// <param name="currentKey">The current key to be encoded.</param>
    /// <param name="currentValue">The current value to be encoded.</param>
    /// <returns>The number of bytes written to the binary stream as a result of the encoding.</returns>
    /// <remarks>
    /// This method encodes a key-value record into the specified binary stream using the configured encoding method.
    /// It takes the previous key and value as well as the current key and value, which can be used for delta encoding.
    /// The method returns the number of bytes written to the stream during encoding.
    /// </remarks>
    protected int EncodeRecord(byte* stream, TKey prevKey, TValue prevValue, TKey currentKey, TValue currentValue)
    {
        return m_encoding.Encode(stream, prevKey, prevValue, currentKey, currentValue);
    }

    /// <summary>
    /// Decodes a key-value record from a binary stream.
    /// </summary>
    /// <param name="stream">A pointer to the binary stream containing the encoded record.</param>
    /// <param name="prevKey">The previous key in the sequence.</param>
    /// <param name="prevValue">The previous value in the sequence.</param>
    /// <param name="currentKey">The current key to be decoded.</param>
    /// <param name="currentValue">The current value to be decoded.</param>
    /// <returns>The number of bytes read from the binary stream as a result of the decoding.</returns>
    /// <remarks>
    /// This method decodes a key-value record from the specified binary stream using the configured decoding method.
    /// It takes the previous key and value as well as the current key and value, which can be used for delta decoding.
    /// The method returns the number of bytes read from the stream during decoding.
    /// </remarks>
    protected int DecodeRecord(byte* stream, TKey prevKey, TValue prevValue, TKey currentKey, TValue currentValue)
    {
        return m_encoding.Decode(stream, prevKey, prevValue, currentKey, currentValue, out _);
    }

    /// <summary>
    /// Initializes the node type, including buffer allocation and storage size calculation.
    /// </summary>
    /// <remarks>
    /// This method initializes the node type by calculating the maximum storage size required for encoding a single
    /// key-value pair, allocating two byte buffers for encoding and decoding operations, and verifying that the tree
    /// has a sufficient number of records per node to ensure efficient operation. If the required records per node
    /// condition is not met, an exception is thrown.
    /// </remarks>
    protected override void InitializeType()
    {
        m_maximumStorageSize = MaximumStorageSize;
        m_buffer1 = new byte[MaximumStorageSize];
        m_buffer2 = new byte[MaximumStorageSize];

        if ((BlockSize - HeaderSize) / MaximumStorageSize < 4)
            throw new Exception("Tree must have at least 4 records per node. Increase the block size or decrease the size of the records.");
    }

    /// <summary>
    /// Reads the value at the specified index within the node.
    /// </summary>
    /// <param name="index">The index of the value to read.</param>
    /// <param name="value">The value object where the read value will be copied.</param>
    /// <exception cref="Exception">Thrown if the provided index is equal to the record count, indicating an invalid access.</exception>
    protected override void Read(int index, TValue value)
    {
        if (index == RecordCount)
            throw new Exception();

        SeekTo(index);
        m_currentValue.CopyTo(value);
    }

    /// <summary>
    /// Reads the key and value at the specified index within the node.
    /// </summary>
    /// <param name="index">The index of the key and value to read.</param>
    /// <param name="key">The key object where the read key will be copied.</param>
    /// <param name="value">The value object where the read value will be copied.</param>
    /// <exception cref="Exception">Thrown if the provided index is equal to the record count, indicating an invalid access.</exception>
    protected override void Read(int index, TKey key, TValue value)
    {
        if (index == RecordCount)
            throw new Exception();

        SeekTo(index);
        m_currentKey.CopyTo(key);
        m_currentValue.CopyTo(value);
    }

    /// <summary>
    /// Removes the key and value at the specified index within the node, unless doing so causes underflow (i.e., less than the minimum required records).
    /// </summary>
    /// <param name="index">The index of the key and value to remove.</param>
    /// <returns><c>true</c> if the removal was successful without causing underflow; otherwise, <c>false</c>.</returns>
    /// <exception cref="NotImplementedException">Thrown if this method is not implemented in the derived class.</exception>
    protected override bool RemoveUnlessOverflow(int index)
    {
        throw new NotImplementedException();
        //if (index != (RecordCount - 1))
        //{
        //    byte* start = GetWritePointerAfterHeader() + index * KeyValueSize;
        //    Memory.Copy(start + KeyValueSize, start, (RecordCount - index - 1) * KeyValueSize);
        //}

        ////save the header
        //RecordCount--;
        //ValidBytes -= (ushort)KeyValueSize;
        //return true;
    }

    /// <summary>
    /// Requests that the current stream is inserted into the tree. Sequential insertion can only occur while the stream
    /// is in order and is entirely past the end of the tree.
    /// </summary>
    /// <param name="stream">the stream data to insert</param>
    /// <param name="isFull">
    /// if returning from this function while the node is not yet full, this means the stream
    /// can no longer be inserted sequentially and we must break out to the root and insert one at a time.
    /// </param>
    protected override void AppendSequentialStream(InsertStreamHelper<TKey, TValue> stream, out bool isFull)
    {
        int recordsAdded = 0;
        int additionalValidBytes = 0;
        byte* writePointer = GetWritePointer();

        fixed (byte* buffer = m_buffer1)
        {
            SeekTo(RecordCount);

            if (RecordCount > 0)
            {
                m_currentKey.CopyTo(stream.PrevKey);
                m_currentValue.CopyTo(stream.PrevValue);
            }
            else
            {
                stream.PrevKey.Clear();
                stream.PrevValue.Clear();
            }

        TryAgain:

            if (!stream.IsValid || !stream.IsStillSequential)
            {
                isFull = false;
                IncrementRecordCounts(recordsAdded, additionalValidBytes);
                ClearNodeCache();
                return;
            }

            int length;
            if (stream.IsKvp1)
            {
                //Key1,Value1 are the current record
                if (RemainingBytes - additionalValidBytes < m_maximumStorageSize)
                {
                    length = EncodeRecord(buffer, stream.Key2, stream.Value2, stream.Key1, stream.Value1);
                    if (RemainingBytes - additionalValidBytes < length)
                    {
                        isFull = true;
                        IncrementRecordCounts(recordsAdded, additionalValidBytes);
                        ClearNodeCache();
                        return;
                    }
                }

                length = EncodeRecord(writePointer + m_nextOffset, stream.Key2, stream.Value2, stream.Key1, stream.Value1);
                additionalValidBytes += length;
                recordsAdded++;
                m_currentOffset = m_nextOffset;
                m_nextOffset = m_currentOffset + length;

                //Inlined stream.Next()
                stream.IsValid = stream.Stream.Read(stream.Key2, stream.Value2);
                stream.IsStillSequential = stream.Key1.IsLessThan(stream.Key2);
                stream.IsKvp1 = false;
                //End Inlined
                goto TryAgain;
            }

            //Key2,Value2 are the current record
            if (RemainingBytes - additionalValidBytes < m_maximumStorageSize)
            {
                length = EncodeRecord(buffer, stream.Key1, stream.Value1, stream.Key2, stream.Value2);
                if (RemainingBytes - additionalValidBytes < length)
                {
                    isFull = true;
                    IncrementRecordCounts(recordsAdded, additionalValidBytes);
                    ClearNodeCache();
                    return;
                }
            }

            length = EncodeRecord(writePointer + m_nextOffset, stream.Key1, stream.Value1, stream.Key2, stream.Value2);
            additionalValidBytes += length;
            recordsAdded++;
            m_currentOffset = m_nextOffset;
            m_nextOffset = m_currentOffset + length;

            //Inlined stream.Next()
            stream.IsValid = stream.Stream.Read(stream.Key1, stream.Value1);
            stream.IsStillSequential = stream.Key2.IsLessThan(stream.Key1);
            stream.IsKvp1 = true;
            //End Inlined

            goto TryAgain;
        }
    }

    /// <summary>
    /// Inserts a point before the current position.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    protected override bool InsertUnlessFull(int index, TKey key, TValue value)
    {
        if (index == RecordCount)
        {
            //Insert After
            SeekTo(index);
            return InsertAfter(key, value);
        }

        //Insert Between
        SeekTo(index);
        return InsertBetween(key, value);
    }

    //protected override bool InsertUnlessFull(int index, TKey key, TValue value)
    //{
    //    throw new NotImplementedException();
    //    if (RecordCount >= m_maxRecordsPerNode)
    //        return false;

    //    byte* start = GetWritePointerAfterHeader() + index * KeyValueSize;

    //    if (index != RecordCount)
    //    {
    //        WinApi.MoveMemory(start + KeyValueSize, start, (RecordCount - index) * KeyValueSize);
    //    }

    //    //Insert the data
    //    KeyMethods.Write(start, key);
    //    ValueMethods.Write(start + KeySize, value);

    //    //save the header
    //    IncrementOneRecord(KeyValueSize);
    //    return true;
    //}

    /// <summary>
    /// Searches for the index of a specific key within the node.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// The index of the key if found within the node; otherwise, the bitwise complement of the index where the key should be inserted
    /// (if not found, the insertion point to maintain sorted order).
    /// </returns>
    protected override int GetIndexOf(TKey key)
    {
        fixed (byte* buffer = m_buffer1)
        {
            SeekTo(key, buffer);
        }

        if (m_currentIndex == RecordCount) // Beyond the end of the list
            return ~RecordCount;

        if (m_currentKey.IsEqualTo(key))
            return m_currentIndex;

        return ~m_currentIndex;
    }

    /// <summary>
    /// Splits the current node into two nodes to accommodate a new entry with the specified dividing key.
    /// </summary>
    /// <param name="newNodeIndex">The index of the new node created as a result of the split.</param>
    /// <param name="dividingKey">The key that determines the split point between the two nodes.</param>
    protected override void Split(uint newNodeIndex, TKey dividingKey)
    {
        fixed (byte* buffer = m_buffer1)
        {
            ClearNodeCache();

            while (m_currentOffset < BlockSize >> 1)
                Read();

            int storageSize = EncodeRecord(buffer, m_nullKey, m_nullValue, m_currentKey, m_currentValue);

            // Determine how many entries to shift on the split.
            int recordsInTheFirstNode = m_currentIndex; // divide by 2.
            int recordsInTheSecondNode = RecordCount - m_currentIndex;
            long sourceStartingAddress = NodePosition + m_nextOffset;
            long targetStartingAddress = newNodeIndex * BlockSize + HeaderSize + storageSize;
            int bytesToMove = ValidBytes - m_nextOffset;

            // Lookup the dividing key
            m_currentKey.CopyTo(dividingKey);

            // Do the copy
            Stream.Copy(sourceStartingAddress, targetStartingAddress, bytesToMove);

            Stream.Position = targetStartingAddress - storageSize;
            Stream.Write(buffer, storageSize);

            // Create the header of the second node.
            CreateNewNode(newNodeIndex, (ushort)recordsInTheSecondNode, (ushort)(HeaderSize + bytesToMove + storageSize), NodeIndex, RightSiblingNodeIndex, dividingKey, UpperKey);

            // Update the node that was the old right sibling
            if (RightSiblingNodeIndex != uint.MaxValue)
                SetLeftSiblingProperty(RightSiblingNodeIndex, NodeIndex, newNodeIndex);

            // Update the original header
            RecordCount = (ushort)recordsInTheFirstNode;
            ValidBytes = (ushort)m_currentOffset;
            RightSiblingNodeIndex = newNodeIndex;
            UpperKey = dividingKey;

            ClearNodeCache();
        }
    }

    //unsafe void SplitNodeThenInsert(long firstNodeIndex)
    //{
    //    //do the copy
    //    StreamLeaf.Copy(sourceStartingAddress, targetStartingAddress, copyLength);

    //    StreamLeaf.Position = targetStartingAddress - storageSize;
    //    StreamLeaf.Write(buffer, storageSize);

    //    //update the node that was the old right sibling
    //    if (currentNode.RightSiblingNodeIndex != 0)
    //    {
    //        m_tempNode.SetCurrentNode(currentNode.RightSiblingNodeIndex);
    //        m_tempNode.LeftSiblingNodeIndex = secondNodeIndex;
    //        m_tempNode.Save();
    //    }

    //    //update the second header
    //    m_newNode.ValidBytes = copyLength + storageSize + NodeHeader.Size;
    //    m_newNode.LeftSiblingNodeIndex = firstNodeIndex;
    //    m_newNode.RightSiblingNodeIndex = currentNode.RightSiblingNodeIndex;
    //    m_newNode.Save();

    //    //update the first header
    //    currentNode.ValidBytes = (int)(m_insertScanner.PositionStartOfCurrent - firstNodeIndex * BlockSize);
    //    currentNode.RightSiblingNodeIndex = secondNodeIndex;
    //    currentNode.Save();

    //    NodeWasSplit(0, firstNodeIndex, CurrentKey.Key1, CurrentKey.Key2, secondNodeIndex);
    //    m_insertScanner.Reset();

    //    if (KeyToBeInserted > CurrentKey)
    //    {
    //        LeafNodeInsert(secondNodeIndex);
    //    }
    //    else
    //    {
    //        LeafNodeInsert(firstNodeIndex);
    //    }
    //}

    /// <summary>
    /// Transfers records from the right sibling node to the left sibling node during a node merge operation.
    /// </summary>
    /// <param name="left">The left sibling node that receives the transferred records.</param>
    /// <param name="right">The right sibling node from which records are transferred.</param>
    /// <param name="bytesToTransfer">The total number of bytes to transfer from the right to the left sibling node.</param>
    protected override void TransferRecordsFromRightToLeft(Node<TKey> left, Node<TKey> right, int bytesToTransfer)
    {
        throw new NotImplementedException();
        //int recordsToTransfer = (bytesToTransfer - HeaderSize) / KeyValueSize;
        ////Transfer records from Right to Left
        //long sourcePosition = right.NodePosition + HeaderSize;
        //long destinationPosition = left.NodePosition + HeaderSize + left.RecordCount * KeyValueSize;
        //Stream.Copy(sourcePosition, destinationPosition, KeyValueSize * recordsToTransfer);

        ////Removes empty spaces from records on the right.
        //Stream.Position = right.NodePosition + HeaderSize;
        //Stream.RemoveBytes(recordsToTransfer * KeyValueSize, (right.RecordCount - recordsToTransfer) * KeyValueSize);

        ////Update number of records.
        //left.RecordCount += (ushort)recordsToTransfer;
        //left.ValidBytes += (ushort)(recordsToTransfer * KeyValueSize);
        //right.RecordCount -= (ushort)recordsToTransfer;
        //right.ValidBytes -= (ushort)(recordsToTransfer * KeyValueSize);
    }

    /// <summary>
    /// Transfers records from the left sibling node to the right sibling node during a node split operation.
    /// </summary>
    /// <param name="left">The left sibling node from which records are transferred.</param>
    /// <param name="right">The right sibling node that receives the transferred records.</param>
    /// <param name="bytesToTransfer">The total number of bytes to transfer from the left to the right sibling node.</param>
    protected override void TransferRecordsFromLeftToRight(Node<TKey> left, Node<TKey> right, int bytesToTransfer)
    {
        throw new NotImplementedException();
        //int recordsToTransfer = (bytesToTransfer - HeaderSize) / KeyValueSize;
        ////Shift existing records to make room for copy
        //Stream.Position = right.NodePosition + HeaderSize;
        //Stream.InsertBytes(recordsToTransfer * KeyValueSize, right.RecordCount * KeyValueSize);

        ////Transfer records from Left to Right
        //long sourcePosition = left.NodePosition + HeaderSize + (left.RecordCount - recordsToTransfer) * KeyValueSize;
        //long destinationPosition = right.NodePosition + HeaderSize;
        //Stream.Copy(sourcePosition, destinationPosition, KeyValueSize * recordsToTransfer);

        ////Update number of records.
        //left.RecordCount -= (ushort)recordsToTransfer;
        //left.ValidBytes -= (ushort)(recordsToTransfer * KeyValueSize);
        //right.RecordCount += (ushort)recordsToTransfer;
        //right.ValidBytes += (ushort)(recordsToTransfer * KeyValueSize);
    }

    private bool InsertAfter(TKey key, TValue value)
    {
        fixed (byte* buffer = m_buffer1)
        {
            int length = EncodeRecord(buffer, m_prevKey, m_prevValue, key, value);

            if (RemainingBytes < length)
                return false;

            EncodeRecord(GetWritePointer() + m_nextOffset, m_prevKey, m_prevValue, key, value);
            //WinApi.MoveMemory(GetWritePointer() + m_nextOffset, buffer, length);
            IncrementOneRecord(length);

            key.CopyTo(m_currentKey);
            value.CopyTo(m_currentValue);
            m_nextOffset = m_currentOffset + length;
            //ResetPositionCached();

            return true;
        }
    }

    private bool InsertBetween(TKey key, TValue value)
    {
        fixed (byte* buffer = m_buffer1, buffer2 = m_buffer2)
        {
            int shiftDelta1 = EncodeRecord(buffer, m_prevKey, m_prevValue, key, value);
            int shiftDelta2 = EncodeRecord(buffer2, key, value, m_currentKey, m_currentValue);
            int shiftDelta = shiftDelta1 + shiftDelta2;

            shiftDelta -= m_nextOffset - m_currentOffset;

            if (RemainingBytes < shiftDelta)
                return false;

            Stream.Position = NodePosition + m_currentOffset;
            if (shiftDelta < 0)
                Stream.RemoveBytes(-shiftDelta, ValidBytes - m_currentOffset);
            else
                Stream.InsertBytes(shiftDelta, ValidBytes - m_currentOffset);

            Stream.Write(buffer, shiftDelta1);
            Stream.Write(buffer2, shiftDelta2);

            IncrementOneRecord(shiftDelta);

            key.CopyTo(m_currentKey);
            value.CopyTo(m_currentValue);
            m_nextOffset = m_currentOffset + shiftDelta1;

            //ResetPositionCached();

            return true;
        }
    }

    /// <summary>
    /// Continue to seek until the end of the list is found or
    /// until the <see cref="m_currentKey"/> >= <paramref name="key"/>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="buffer"></param>
    private void SeekTo(TKey key, byte* buffer)
    {
        //ToDo: Optimize this seek algorithm
        if (m_currentIndex == 0 && key.IsLessThan(m_prevKey))
            return;

        if (m_currentIndex >= 0 && m_prevKey.IsLessThan(key))
        {
            if (!m_currentKey.IsLessThan(key) || m_currentIndex == RecordCount)
                return;

            while (Read() && m_currentKey.IsLessThan(key))
            {
            }
        }
        else
        {
            ClearNodeCache();

            while (Read() && m_currentKey.IsLessThan(key))
            {
            }
        }
    }

    /// <summary>
    /// Seeks to a specific record index within the node and loads the corresponding key and value into the node's current key and value.
    /// </summary>
    /// <param name="index">The index of the record to seek to within the node.</param>
    private void SeekTo(int index)
    {
        //Reset();
        //for (int x = 0; x <= index; x++)
        //    Read();
        if (m_currentIndex > index)
        {
            ClearNodeCache();

            for (int x = 0; x <= index; x++)
                Read();
        }
        else
        {
            for (int x = m_currentIndex; x < index; x++)
                Read();
        }
    }

    private void OnNodeIndexChanged(object sender, EventArgs e)
    {
        ClearNodeCache();
    }

    private void ClearNodeCache()
    {
        m_nextOffset = HeaderSize;
        m_currentOffset = HeaderSize;
        m_currentIndex = -1;
        m_prevKey.Clear();
        m_prevValue.Clear();
        m_currentKey.Clear();
        m_currentValue.Clear();
    }

    private bool Read()
    {
        if (m_currentIndex == RecordCount)
            throw new Exception("Read past the end of the stream");

        m_currentKey.CopyTo(m_prevKey);
        m_currentValue.CopyTo(m_prevValue);
        m_currentOffset = m_nextOffset;
        m_currentIndex++;

        if (m_currentIndex == RecordCount)
            return false;

        m_nextOffset += DecodeRecord(GetReadPointer() + m_nextOffset, m_prevKey, m_prevValue, m_currentKey, m_currentValue);
        return true;
    }

    #endregion
}