//******************************************************************************************************
//  NodeHeader`1.cs - Gbtc
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
//  10/09/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/29/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap.Tree.Specialized;

/// <summary>
/// Contains basic data about a node in the SortedTree.
/// </summary>
/// <typeparam name="TKey">The key that the SortedTree contains.</typeparam>
public unsafe class NodeHeader<TKey> where TKey : SnapTypeBase<TKey>, new()
{
    #region [ Members ]

    /// <summary>
    /// The version of the node header.
    /// </summary>
    public const byte Version = 0;

    /// <summary>
    /// The size, in bytes, of an index field.
    /// </summary>
    protected const int IndexSize = sizeof(uint);

    /// <summary>
    /// The offset of the left sibling node index field within the node header.
    /// </summary>
    protected const int OffsetOfLeftSibling = OffsetOfValidBytes + sizeof(ushort);

    /// <summary>
    /// The offset of the lower bounds field within the node header.
    /// </summary>
    protected const int OffsetOfLowerBounds = OffsetOfRightSibling + IndexSize;

    /// <summary>
    /// The offset of the node level field within the node header.
    /// </summary>
    protected const int OffsetOfNodeLevel = OffsetOfVersion + 1;

    /// <summary>
    /// The offset of the record count field within the node header.
    /// </summary>
    protected const int OffsetOfRecordCount = OffsetOfNodeLevel + sizeof(byte);

    /// <summary>
    /// The offset of the right sibling node index field within the node header.
    /// </summary>
    protected const int OffsetOfRightSibling = OffsetOfLeftSibling + IndexSize;

    /// <summary>
    /// The offset of the valid bytes field within the node header.
    /// </summary>
    protected const int OffsetOfValidBytes = OffsetOfRecordCount + sizeof(ushort);

    /// <summary>
    /// The offset of the version field within the node header.
    /// </summary>
    protected const int OffsetOfVersion = 0;

    /// <summary>
    /// The size, in bytes, of the node block.
    /// </summary>
    public int BlockSize;

    /// <summary>
    /// The size, in bytes, of a key.
    /// </summary>
    public int KeySize;

    /// <summary>
    /// The index of the left sibling node.
    /// </summary>
    public uint LeftSiblingNodeIndex;

    /// <summary>
    /// The level of the node within the B-tree structure.
    /// </summary>
    public readonly byte Level;

    /// <summary>
    /// The lower key associated with the node.
    /// </summary>
    public TKey LowerKey;

    /// <summary>
    /// The index of the node.
    /// </summary>
    public uint NodeIndex;

    /// <summary>
    /// The number of records within the node.
    /// </summary>
    public ushort RecordCount;

    /// <summary>
    /// The index of the right sibling node.
    /// </summary>
    public uint RightSiblingNodeIndex;

    /// <summary>
    /// The upper key associated with the node.
    /// </summary>
    public TKey UpperKey;

    /// <summary>
    /// The number of valid bytes within the node.
    /// </summary>
    public ushort ValidBytes;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// The constructor that is used for inheriting. Must call Initialize before using it.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="blockSize"></param>
    public NodeHeader(byte level, int blockSize)
    {
        Level = level;
        BlockSize = blockSize;
        LowerKey = new TKey();
        UpperKey = new TKey();
        KeySize = UpperKey.Size;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the byte offset of the header size.
    /// </summary>
    public int HeaderSize => OffsetOfLowerBounds + KeySize * 2;

    /// <summary>
    /// Gets the number of remaining bytes in the current data block.
    /// </summary>
    public ushort RemainingBytes => (ushort)(BlockSize - ValidBytes);

    /// <summary>
    /// Gets the byte offset of the upper bounds key.
    /// </summary>
    private int OffsetOfUpperBounds => OffsetOfLowerBounds + KeySize;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the node header data to a memory location pointed to by a byte pointer.
    /// </summary>
    /// <param name="ptr">A pointer to the memory location where the node header data should be saved.</param>
    public void Save(byte* ptr)
    {
        ptr[0] = Version;
        ptr[OffsetOfNodeLevel] = Level;
        *(ushort*)(ptr + OffsetOfRecordCount) = RecordCount;
        *(ushort*)(ptr + OffsetOfValidBytes) = ValidBytes;
        *(uint*)(ptr + OffsetOfLeftSibling) = LeftSiblingNodeIndex;
        *(uint*)(ptr + OffsetOfRightSibling) = RightSiblingNodeIndex;
        LowerKey.Write(ptr + OffsetOfLowerBounds);
        UpperKey.Write(ptr + OffsetOfUpperBounds);
    }

    #endregion
}