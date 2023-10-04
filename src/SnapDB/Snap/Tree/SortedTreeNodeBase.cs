//******************************************************************************************************
//  SortedTreeNodeBase`2.cs - Gbtc
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

using SnapDB.IO;

namespace SnapDB.Snap.Tree;

/// <summary>
/// An abstract base class for sorted tree nodes, used in SortedTree structures.
/// </summary>
/// <typeparam name="TKey">The type of keys in the tree.</typeparam>
/// <typeparam name="TValue">The type of values associated with keys in the tree.</typeparam>
public abstract partial class SortedTreeNodeBase<TKey, TValue> : Node<TKey> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    /// <summary>
    /// The size in bytes of a key-value pair.
    /// </summary>
    protected readonly int KeyValueSize;

    /// <summary>
    /// The sparse index used for navigation.
    /// </summary>
    protected SparseIndex<TKey> SparseIndex;

    private Func<uint> m_getNextNewNodeIndex;
    private bool m_initialized;
    private int m_minRecordNodeBytes;
    private Node<TKey> m_tempNode1;
    private Node<TKey> m_tempNode2;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="SortedTreeNodeBase{TKey, TValue}"/> class with the specified level.
    /// </summary>
    /// <param name="level">The level of the tree node.</param>
    protected SortedTreeNodeBase(byte level) : base(level)
    {
        m_initialized = false;
        KeyValueSize = KeySize + new TValue().Size;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a clone of the tree node with the specified level.
    /// </summary>
    /// <param name="level">The level of the cloned node.</param>
    /// <returns>A new instance of the cloned tree node.</returns>
    public abstract SortedTreeNodeBase<TKey, TValue> Clone(byte level);


    /// <summary>
    /// Initializes the required parameters for this tree to function. Must be called once.
    /// </summary>
    /// <param name="stream">The binary stream to use.</param>
    /// <param name="blockSize">The size of each block.</param>
    /// <param name="getNextNewNodeIndex">A function to get the next new node index.</param>
    /// <param name="sparseIndex">The sparse index to use.</param>
    public void Initialize(BinaryStreamPointerBase stream, int blockSize, Func<uint> getNextNewNodeIndex, SparseIndex<TKey> sparseIndex)
    {
        if (m_initialized)
            throw new Exception("Duplicate calls to initialize");

        m_initialized = true;
        InitializeNode(stream, blockSize);

        m_tempNode1 = new Node<TKey>(stream, blockSize, Level);
        m_tempNode2 = new Node<TKey>(stream, blockSize, Level);
        SparseIndex = sparseIndex;
        m_minRecordNodeBytes = BlockSize >> 2;
        m_getNextNewNodeIndex = getNextNewNodeIndex;

        InitializeType();
    }

    /// <summary>
    /// Determines which sibling node that this node can be combined with.
    /// </summary>
    /// <param name="key">The key of the child node that needs to be checked.</param>
    /// <param name="canCombineLeft">Outputs <c>true</c> if combining with the left child is supported; otherwise, <c>false</c>.</param>
    /// <param name="canCombineRight">Outputs <c>true</c> if combining with the right child is supported; otherwise, <c>false</c>.</param>
    public void CanCombineWithSiblings(TKey key, out bool canCombineLeft, out bool canCombineRight)
    {
        if (Level == 0)
            throw new NotSupportedException("This function cannot be used at the leaf level.");

        NavigateToNode(key);

        int search = GetIndexOf(key);
        if (search < 0)
            throw new KeyNotFoundException();

        canCombineLeft = search > 0;
        canCombineRight = search < RecordCount - 1;
    }


    /// <summary>
    /// Returns a tree scanner class for this tree node.
    /// </summary>
    /// <returns>A tree scanner instance.</returns>
    public abstract SortedTreeScannerBase<TKey, TValue> CreateTreeScanner();

    /// <summary>
    /// Navigates to the node that contains the specified key.
    /// </summary>
    /// <param name="key">The key of concern.</param>
    private void NavigateToNode(TKey key)
    {
        if (!IsKeyInsideBounds(key))
            SetNodeIndex(SparseIndex.Get(key, Level));
    }

    #endregion
}