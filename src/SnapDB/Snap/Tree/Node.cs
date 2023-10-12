//******************************************************************************************************
//  Node`1.cs - Gbtc
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
//  03/15/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;

namespace SnapDB.Snap.Tree;

/// <summary>
/// Contains basic data about a node in the SortedTree.
/// </summary>
/// <typeparam name="TKey">The key that the SortedTree contains.</typeparam>
public unsafe class Node<TKey> where TKey : SnapTypeBase<TKey>, new()
{
    #region [ Members ]

    /// <summary>
    /// Occurs when the node index is changed or cleared.
    /// </summary>
    protected event EventHandler NodeIndexChanged;

    /// <summary>
    /// The size in bytes of an index value within the node's header.
    /// </summary>
    protected const int IndexSize = sizeof(uint);

    /// <summary>
    /// Offset within a node's header where the left sibling node's index is stored.
    /// </summary>
    protected const int OffsetOfLeftSibling = OffsetOfValidBytes + sizeof(ushort);

    /// <summary>
    /// Offset within a node's header where the lower bounds of the node are stored.
    /// </summary>
    protected const int OffsetOfLowerBounds = OffsetOfRightSibling + IndexSize;

    /// <summary>
    /// Offset within a node's header where the level of the node is stored.
    /// </summary>
    protected const int OffsetOfNodeLevel = OffsetOfVersion + sizeof(byte);

    /// <summary>
    /// Offset within a node's header where the record count is stored.
    /// </summary>
    protected const int OffsetOfRecordCount = OffsetOfNodeLevel + sizeof(byte);

    /// <summary>
    /// Offset within a node's header where the right sibling node's index is stored.
    /// </summary>
    protected const int OffsetOfRightSibling = OffsetOfLeftSibling + IndexSize;

    /// <summary>
    /// Offset within a node's header where the number of valid bytes is stored.
    /// </summary>
    protected const int OffsetOfValidBytes = OffsetOfRecordCount + sizeof(ushort);

    /// <summary>
    /// Offset within a node's header where the version information is stored.
    /// </summary>
    protected const int OffsetOfVersion = 0;

    /// <summary>
    /// Version constant representing the current version of the object.
    /// </summary>
    private const byte Version = 0;

    /// <summary>
    /// Block size of the node.
    /// </summary>
    protected int BlockSize;

    /// <summary>
    /// Custom methods for handling keys of type TKey.
    /// </summary>
    protected SnapTypeCustomMethods<TKey> KeyMethods;

    /// <summary>
    /// Size of a key in bytes.
    /// </summary>
    protected int KeySize;

    /// <summary>
    /// Level of the node within the tree hierarchy.
    /// </summary>
    protected byte Level;

    /// <summary>
    /// Binary stream pointer used for reading and writing data.
    /// </summary>
    protected BinaryStreamPointerBase Stream;

    /// <summary>
    /// Flag indicating whether the object has been initialized.
    /// </summary>
    private bool m_initialized;

    /// <summary>
    /// Index of the left sibling node.
    /// </summary>
    private uint m_leftSiblingNodeIndex;

    /// <summary>
    /// The lower key bound for the node.
    /// </summary>
    private TKey m_lowerKey;

    /// <summary>
    /// Pointer to the start of the node's data.
    /// </summary>
    private byte* m_pointer;

    /// <summary>
    /// Pointer to the position immediately after the node's header.
    /// </summary>
    private byte* m_pointerAfterHeader;

    /// <summary>
    /// Version number for reading from the pointer.
    /// </summary>
    private long m_pointerReadVersion;

    /// <summary>
    /// Version number for writing to the pointer.
    /// </summary>
    private long m_pointerWriteVersion;

    /// <summary>
    /// Number of records in the node.
    /// </summary>
    private ushort m_recordCount;

    /// <summary>
    /// Index of the right sibling node.
    /// </summary>
    private uint m_rightSiblingNodeIndex;

    /// <summary>
    /// The upper key bound for the node.
    /// </summary>
    private TKey m_upperKey;

    /// <summary>
    /// Number of valid bytes in the node.
    /// </summary>
    private ushort m_validBytes;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="Node{TKey}"/> class with the specified level.
    /// </summary>
    /// <param name="level">The level of the node within the tree hierarchy.</param>
    protected Node(byte level)
    {
        Level = level;
        KeyMethods = new TKey().CreateValueMethods();
        KeySize = new TKey().Size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Node{TKey}"/> class by reading node data from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The binary stream containing node data.</param>
    /// <param name="blockSize">The size of the node's block within the stream.</param>
    /// <param name="level">The level of the node within the tree hierarchy.</param>
    public Node(BinaryStreamPointerBase stream, int blockSize, byte level) : this(level)
    {
        InitializeNode(stream, blockSize);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The index of the left sibling. <see cref="uint.MaxValue"/> is the null case.
    /// </summary>
    public uint LeftSiblingNodeIndex
    {
        get => m_leftSiblingNodeIndex;
        set
        {
            *(uint*)(GetWritePointer() + OffsetOfLeftSibling) = value;
            m_leftSiblingNodeIndex = value;
        }
    }

    /// <summary>
    /// The lower bounds of the node. This is an inclusive bounds and always valid.
    /// </summary>
    public TKey LowerKey
    {
        get => m_lowerKey;
        set
        {
            value.Write(GetWritePointer() + OffsetOfLowerBounds);
            value.CopyTo(m_lowerKey);
        }
    }

    /// <summary>
    /// Gets the node index of this current node.
    /// </summary>
    public uint NodeIndex { get; private set; }

    /// <summary>
    /// Gets the first position for the current node.
    /// </summary>
    public long NodePosition => BlockSize * NodeIndex;

    /// <summary>
    /// Gets or sets the number of records in this node.
    /// </summary>
    public ushort RecordCount
    {
        get => m_recordCount;
        set
        {
            *(ushort*)(GetWritePointer() + OffsetOfRecordCount) = value;
            m_recordCount = value;
        }
    }

    /// <summary>
    /// The index of the right sibling. <see cref="uint.MaxValue"/> is the null case.
    /// </summary>
    public uint RightSiblingNodeIndex
    {
        get => m_rightSiblingNodeIndex;
        set
        {
            *(uint*)(GetWritePointer() + OffsetOfRightSibling) = value;
            m_rightSiblingNodeIndex = value;
        }
    }

    /// <summary>
    /// The upper bounds of the node. This is an exclusive bounds and is valid
    /// when there is a sibling to the right. If there is no sibling to the right,
    /// it should still be valid except for the maximum key value condition.
    /// </summary>
    public TKey UpperKey
    {
        get => m_upperKey;
        set
        {
            value.Write(GetWritePointer() + OffsetOfUpperBounds);
            value.CopyTo(m_upperKey);
        }
    }

    /// <summary>
    /// The number of bytes that are used in this node.
    /// </summary>
    public ushort ValidBytes
    {
        get => m_validBytes;
        set
        {
            *(ushort*)(GetWritePointer() + OffsetOfValidBytes) = value;
            m_validBytes = value;
        }
    }

    /// <summary>
    /// Gets the byte offset of the header size.
    /// </summary>
    protected int HeaderSize => OffsetOfLowerBounds + KeySize * 2;

    /// <summary>
    /// Is the index of the left sibling null, i.e., equal to <see cref="uint.MaxValue"/>
    /// </summary>
    protected bool IsLeftSiblingIndexNull => m_leftSiblingNodeIndex == uint.MaxValue;

    /// <summary>
    /// Is the index of the right sibling null, i.e., equal to <see cref="uint.MaxValue"/>
    /// </summary>
    protected bool IsRightSiblingIndexNull => m_rightSiblingNodeIndex == uint.MaxValue;

    /// <summary>
    /// Gets or sets the number of unused bytes in the node.
    /// </summary>
    protected ushort RemainingBytes => (ushort)(BlockSize - m_validBytes);

    /// <summary>
    /// The position that points to the location right after the header which is the
    /// start of the data within the node.
    /// </summary>
    protected long StartOfDataPosition => NodeIndex * BlockSize + HeaderSize;

    /// <summary>
    /// Gets the byte offset of the upper bounds key.
    /// </summary>
    private int OffsetOfUpperBounds => OffsetOfLowerBounds + KeySize;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Invalidates the current node.
    /// </summary>
    public void Clear()
    {
        NodeIndexChanged?.Invoke(this, EventArgs.Empty);
        // InsideNodeBoundary = m_BoundsFalse;
        NodeIndex = uint.MaxValue;
        m_pointerReadVersion = -1;
        m_pointerWriteVersion = -1;
        m_recordCount = 0;
        m_validBytes = (ushort)HeaderSize;
        m_leftSiblingNodeIndex = uint.MaxValue;
        m_rightSiblingNodeIndex = uint.MaxValue;
        UpperKey.Clear();
        LowerKey.Clear();
    }

    /// <summary>
    /// Sets the node index to the specified value, updating the node's internal state.
    /// </summary>
    /// <param name="nodeIndex">The new node index to set.</param>
    /// <exception cref="Exception">Thrown when an invalid node index is provided or when the node is not supposed to access the underlying node level.</exception>
    public void SetNodeIndex(uint nodeIndex)
    {
        if (nodeIndex == uint.MaxValue)
            throw new Exception("Invalid Node Index");

        if (NodeIndex != nodeIndex)
        {
            NodeIndexChanged?.Invoke(this, EventArgs.Empty);
            NodeIndex = nodeIndex;
            m_pointerReadVersion = -1;
            m_pointerWriteVersion = -1;
            byte* ptr = GetReadPointer();
            if (ptr[OffsetOfNodeLevel] != Level)
                throw new Exception("This node is not supposed to access the underlying node level.");
            m_recordCount = *(ushort*)(ptr + OffsetOfRecordCount);
            m_validBytes = *(ushort*)(ptr + OffsetOfValidBytes);
            m_leftSiblingNodeIndex = *(uint*)(ptr + OffsetOfLeftSibling);
            m_rightSiblingNodeIndex = *(uint*)(ptr + OffsetOfRightSibling);
            LowerKey.Read(ptr + OffsetOfLowerBounds);
            UpperKey.Read(ptr + OffsetOfUpperBounds);
        }
    }

    /// <summary>
    /// Creates an empty node with the specified node index.
    /// </summary>
    /// <param name="newNodeIndex">The index of the new node to create.</param>
    public void CreateEmptyNode(uint newNodeIndex)
    {
        TKey key = new();
        byte* ptr = Stream.GetWritePointer(newNodeIndex * BlockSize, BlockSize);
        ptr[OffsetOfVersion] = Version;
        ptr[OffsetOfNodeLevel] = Level;
        *(ushort*)(ptr + OffsetOfRecordCount) = 0;
        *(ushort*)(ptr + OffsetOfValidBytes) = (ushort)HeaderSize;
        *(uint*)(ptr + OffsetOfLeftSibling) = uint.MaxValue;
        *(uint*)(ptr + OffsetOfRightSibling) = uint.MaxValue;
        key.SetMin();
        key.Write(ptr + OffsetOfLowerBounds);
        key.SetMax();
        key.Write(ptr + OffsetOfUpperBounds);
        SetNodeIndex(newNodeIndex);
    }

    /// <summary>
    /// Checks if the specified key falls within the bounds of this node.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    /// <c>true</c> if the key is within the node's bounds; otherwise, <c>false</c>.
    /// </returns>
    public bool IsKeyInsideBounds(TKey key)
    {
        return NodeIndex != uint.MaxValue && (LeftSiblingNodeIndex == uint.MaxValue || LowerKey.IsLessThanOrEqualTo(key)) && (RightSiblingNodeIndex == uint.MaxValue || key.IsLessThan(UpperKey));
    }

    /// <summary>
    /// Seeks the current node to the right sibling node. Throws an exception if the navigation fails.
    /// </summary>
    public void SeekToRightSibling()
    {
        SetNodeIndex(RightSiblingNodeIndex);
    }

    /// <summary>
    /// Seeks the current node to the left sibling node. Throws an exception if the navigation fails.
    /// </summary>
    public void SeekToLeftSibling()
    {
        SetNodeIndex(LeftSiblingNodeIndex);
    }

    /// <summary>
    /// Gets a read pointer positioned immediately after the node header.
    /// </summary>
    /// <returns>A pointer to the location after the node header in the data stream.</returns>
    public byte* GetReadPointerAfterHeader()
    {
        if (Stream.PointerVersion != m_pointerReadVersion)
            UpdateReadPointer();

        return m_pointerAfterHeader;
    }

    /// <summary>
    /// Initializes the node with the given binary stream and block size.
    /// </summary>
    /// <param name="stream">The binary stream containing the node data.</param>
    /// <param name="blockSize">The size of the node's data block.</param>
    /// <exception cref="Exception">Thrown if the method is called multiple times (duplicate initialization).</exception>
    protected void InitializeNode(BinaryStreamPointerBase stream, int blockSize)
    {
        if (m_initialized)
            throw new Exception("Duplicate calls to initialize");

        m_initialized = true;
        Stream = stream;
        BlockSize = blockSize;
        m_lowerKey = new TKey();
        m_upperKey = new TKey();
        Clear();
    }

    /// <summary>
    /// Modifies both the <see cref="RecordCount"/> and <see cref="ValidBytes"/> in one function call.
    /// </summary>
    /// <param name="additionalValidBytes">The number of bytes to increase <see cref="ValidBytes"/> by.</param>
    protected void IncrementOneRecord(int additionalValidBytes)
    {
        ushort* ptr = (ushort*)(GetWritePointer() + OffsetOfRecordCount);
        m_recordCount++;
        m_validBytes += (ushort)additionalValidBytes;
        ptr[0]++;
        ptr[1] += (ushort)additionalValidBytes;
    }

    /// <summary>
    /// Increments the record counts and valid bytes of the node by the specified amounts.
    /// </summary>
    /// <param name="recordCount">The number of records to add to the node's record count.</param>
    /// <param name="additionalValidBytes">The number of additional valid bytes to add to the node's valid bytes count.</param>
    protected void IncrementRecordCounts(int recordCount, int additionalValidBytes)
    {
        ushort* ptr = (ushort*)(GetWritePointer() + OffsetOfRecordCount);
        m_recordCount += (ushort)recordCount;
        m_validBytes += (ushort)additionalValidBytes;
        ptr[0] += (ushort)recordCount;
        ptr[1] += (ushort)additionalValidBytes;
    }

    /// <summary>
    /// Creates a new node with the provided data.
    /// </summary>
    /// <param name="nodeIndex">The index of the new node.</param>
    /// <param name="recordCount">The record count for the new node.</param>
    /// <param name="validBytes">The valid bytes count for the new node.</param>
    /// <param name="leftSibling">The index of the left sibling node.</param>
    /// <param name="rightSibling">The index of the right sibling node.</param>
    /// <param name="lowerKey">The lower key for the new node.</param>
    /// <param name="upperKey">The upper key for the new node.</param>
    protected void CreateNewNode(uint nodeIndex, ushort recordCount, ushort validBytes, uint leftSibling, uint rightSibling, TKey lowerKey, TKey upperKey)
    {
        byte* ptr = Stream.GetWritePointer(nodeIndex * BlockSize, BlockSize);
        ptr[OffsetOfVersion] = Version;
        ptr[OffsetOfNodeLevel] = Level;
        *(ushort*)(ptr + OffsetOfRecordCount) = recordCount;
        *(ushort*)(ptr + OffsetOfValidBytes) = validBytes;
        *(uint*)(ptr + OffsetOfLeftSibling) = leftSibling;
        *(uint*)(ptr + OffsetOfRightSibling) = rightSibling;
        lowerKey.Write(ptr + OffsetOfLowerBounds);
        upperKey.Write(ptr + OffsetOfUpperBounds);
    }

    /// <summary>
    /// Sets the left sibling property of a node with the specified index to a new value.
    /// </summary>
    /// <param name="nodeIndex">The index of the node whose left sibling property is being set.</param>
    /// <param name="oldValue">The expected old value of the left sibling property.</param>
    /// <param name="newValue">The new value to set for the left sibling property.</param>
    protected void SetLeftSiblingProperty(uint nodeIndex, uint oldValue, uint newValue)
    {
        byte* ptr = Stream.GetWritePointer(BlockSize * nodeIndex, BlockSize);
        if (ptr[OffsetOfNodeLevel] != Level)
            throw new Exception("This node is not supposed to access the underlying node level.");

        if (*(uint*)(ptr + OffsetOfLeftSibling) != oldValue)
            throw new Exception("old value is not what was expected in the node.");
        *(uint*)(ptr + OffsetOfLeftSibling) = newValue;

        if (NodeIndex == nodeIndex)
            m_leftSiblingNodeIndex = newValue;
    }

    /// <summary>
    /// Retrieves the valid bytes count from the specified node's header.
    /// </summary>
    /// <param name="nodeIndex">The index of the node from which to retrieve valid bytes.</param>
    /// <returns>The count of valid bytes stored in the specified node's header.</returns>
    protected int GetValidBytes(uint nodeIndex)
    {
        byte* ptr = Stream.GetReadPointer(BlockSize * nodeIndex, BlockSize);
        if (ptr[OffsetOfNodeLevel] != Level)
            throw new Exception("This node is not supposed to access the underlying node level.");

        return *(ushort*)(ptr + OffsetOfValidBytes);
    }

    /// <summary>
    /// Retrieves a read pointer to the current node's data.
    /// </summary>
    /// <returns>A pointer to the current node's data for reading.</returns>
    protected byte* GetReadPointer()
    {
        if (Stream.PointerVersion != m_pointerReadVersion)
            UpdateReadPointer();

        return m_pointer;
    }

    /// <summary>
    /// Retrieves a write pointer to the current node's data.
    /// </summary>
    /// <returns>A pointer to the current node's data for writing.</returns>
    protected byte* GetWritePointer()
    {
        if (Stream.PointerVersion != m_pointerWriteVersion)
            UpdateWritePointer();

        return m_pointer;
    }

    /// <summary>
    /// Retrieves a write pointer to the data area after the node's header.
    /// </summary>
    /// <returns>A pointer to the data area after the node's header for writing.</returns>
    protected byte* GetWritePointerAfterHeader()
    {
        if (Stream.PointerVersion != m_pointerWriteVersion)
            UpdateWritePointer();

        return m_pointerAfterHeader;
    }

    private void UpdateReadPointer()
    {
        m_pointer = Stream.GetReadPointer(BlockSize * NodeIndex, BlockSize, out bool ptrSupportsWrite);
        m_pointerAfterHeader = m_pointer + HeaderSize;
        m_pointerReadVersion = Stream.PointerVersion;
        if (ptrSupportsWrite)
            m_pointerWriteVersion = Stream.PointerVersion;
        else
            m_pointerWriteVersion = -1;
    }

    private void UpdateWritePointer()
    {
        m_pointer = Stream.GetWritePointer(BlockSize * NodeIndex, BlockSize);
        m_pointerAfterHeader = m_pointer + HeaderSize;
        m_pointerReadVersion = Stream.PointerVersion;
        m_pointerWriteVersion = Stream.PointerVersion;
    }

    #endregion
}