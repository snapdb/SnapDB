﻿//******************************************************************************************************
//  SortedTree`2.cs - Gbtc
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
//  03/22/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;

namespace SnapDB.Snap.Tree;

/// <summary>
/// Provides the basic user methods with any derived B+Tree.
/// This base class translates all of the core methods into simple methods
/// that must be implemented by classes derived from this base class.
/// </summary>
/// <typeparam name="TKey">The key type for the sorted tree.</typeparam>
/// <typeparam name="TValue">The value type for the sorted tree.</typeparam>
/// <remarks>
/// This class does not support concurrent read operations.  This is due to the caching method of each tree.
/// If concurrent read operations are desired, clone the tree.
/// Trees cannot be cloned if the user plans to write to the tree.
/// </remarks>
public class SortedTree<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    /// <summary>
    /// Gets or sets the sparse index associated with this node. It provides a mapping between keys and node indices.
    /// </summary>
    protected SparseIndex<TKey> Indexer = default!;

    /// <summary>
    /// Gets or sets the storage for leaf nodes. Leaf nodes store key-value pairs in the sorted tree.
    /// </summary>
    protected SortedTreeNodeBase<TKey, TValue> LeafStorage = default!;

    private readonly SortedTreeHeader m_header;

    private bool m_isInitialized;
    private readonly TValue m_tempValue = new();

    #endregion

    #region [ Constructors ]

    internal SortedTree(BinaryStreamPointerBase stream1)
    {
        m_header = new SortedTreeHeader();
        Stream = stream1;
        m_isInitialized = false;
        AutoFlush = true;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The sorted tree will not continually call the <see cref="Flush"/> method every time the header is changed.
    /// When setting this to false, flushes must be manually invoked. Failing to do this can corrupt the SortedTree.
    /// Only set if you can guarantee that <see cref="Flush"/> will be called before disposing this class.
    /// </summary>
    public bool AutoFlush { get; set; }

    /// <summary>
    /// Gets if the sorted tree needs to be flushed to the disk.
    /// </summary>
    public bool IsDirty => m_header.IsDirty;

    /// <summary>
    /// Contains the block size that the tree nodes will be aligned on.
    /// </summary>
    protected int BlockSize => m_header.BlockSize;

    /// <summary>
    /// Contains the stream for reading and writing.
    /// </summary>
    protected BinaryStreamPointerBase Stream { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Flushes any header data that may have changed to the main stream.
    /// </summary>
    public void Flush()
    {
        if (!m_isInitialized)
            throw new Exception("Class has not been initialized");

        m_header.SaveHeader(Stream);
    }

    /// <summary>
    /// Sets a flag that requires that the header data is no longer valid.
    /// </summary>
    public void SetDirtyFlag()
    {
        m_header.IsDirty = true;
    }

    /// <summary>
    /// Adds the provided key/value to the Tree.
    /// </summary>
    /// <param name="key">the key to add</param>
    /// <param name="value">the value to add</param>
    public void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
            throw new Exception("Key already exists");
    }

    /// <summary>
    /// Attempts to add the provided key/value to the Tree.
    /// </summary>
    /// <param name="key">the key to add</param>
    /// <param name="value">the value to add</param>
    /// <returns>returns true if successful, false if a duplicate key was found</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        if (!LeafStorage.TryInsert(key, value))
            return false;

        if (IsDirty && AutoFlush)
            m_header.SaveHeader(Stream);

        return true;
    }

    /// <summary>
    /// Adds all of the points in the stream to the Tree
    /// </summary>
    /// <param name="stream">stream to add</param>
    public void AddRange(TreeStream<TKey, TValue> stream)
    {
        TKey key = new();
        TValue value = new();

        while (stream.Read(key, value))
            Add(key, value);
    }

    /// <summary>
    /// Adds all of the items in the stream to this tree. Skips any duplicate entries.
    /// </summary>
    /// <param name="stream">the stream to add.</param>
    public void TryAddRange(TreeStream<TKey, TValue> stream)
    {
        //TKey key = new TKey();
        //TValue value = new TValue();
        //while (stream.Read(key, value))
        //{
        //    TryAdd(key, value);
        //}
        //return;

        InsertStreamHelper<TKey, TValue> helper = new(stream);
        LeafStorage.TryInsertSequentialStream(helper);

        while (helper.IsValid)
        {
            if (helper.IsKvp1)
                LeafStorage.TryInsert(helper.Key1, helper.Value1);
            else
                LeafStorage.TryInsert(helper.Key2, helper.Value2);

            helper.NextDoNotCheckSequential();
        }

        if (IsDirty && AutoFlush)
            m_header.SaveHeader(Stream);
    }

    /// <summary>
    /// Tries to remove the following key from the tree.
    /// </summary>
    /// <param name="key">the key to remove</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool TryRemove(TKey key)
    {
        if (LeafStorage.TryRemove(key))
        {
            if (IsDirty && AutoFlush)
                m_header.SaveHeader(Stream);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the following key from the Tree. Assigns to the value.
    /// </summary>
    /// <param name="key">the key to look for</param>
    /// <param name="value">the place to store the value</param>
    public void Get(TKey key, TValue value)
    {
        if (!TryGet(key, value))
            throw new Exception("Key does not exists");
    }

    /// <summary>
    /// Attempts to get the following key from the Tree. Assigns to the value.
    /// </summary>
    /// <param name="key">the key to look for</param>
    /// <param name="value">the place to store the value</param>
    /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
    public bool TryGet(TKey key, TValue value)
    {
        return LeafStorage.TryGet(key, value);
    }

    /// <summary>
    /// Gets the lower and upper bounds of this tree.
    /// </summary>
    /// <param name="lowerBounds">The first key in the tree</param>
    /// <param name="upperBounds">The final key in the tree</param>
    /// <remarks>
    /// If the tree contains no data. <paramref name="lowerBounds"/> is set to it's maximum value
    /// and <paramref name="upperBounds"/> is set to it's minimum value.
    /// </remarks>
    public void GetKeyRange(TKey lowerBounds, TKey upperBounds)
    {
        LeafStorage.SeekToFirstNode();
        bool firstFound = LeafStorage.TryGetFirstRecord(lowerBounds, m_tempValue);

        LeafStorage.SeekToLastNode();
        bool lastFound = LeafStorage.TryGetLastRecord(upperBounds, m_tempValue);

        if (firstFound && lastFound)
            return;

        lowerBounds.SetMax();
        upperBounds.SetMin();
    }

    /// <summary>
    /// Creates a tree scanner that can be used to seek this tree.
    /// </summary>
    /// <returns>A tree scanner instance for the sorted tree.</returns>
    public SortedTreeScannerBase<TKey, TValue> CreateTreeScanner()
    {
        return LeafStorage.CreateTreeScanner();
    }

    /// <summary>
    /// Returns the node index address for a freshly allocated block.
    /// </summary>
    /// <returns>Node index address for a freshly allocated block.</returns>
    /// <remarks>Also saves the header data.</remarks>
    protected uint GetNextNewNodeIndex()
    {
        m_header.LastAllocatedBlock++;
        SetDirtyFlag();
        return m_header.LastAllocatedBlock;
    }

    internal void InitializeOpen()
    {
        if (m_isInitialized)
            throw new Exception("Duplicate calls to Initialize");
        m_isInitialized = true;

        SortedTree.ReadHeader(Stream, out m_header.TreeNodeType, out m_header.BlockSize);

        // Since m_loadHeader is currently null in the constructor, 
        // this will load as much data that this tree can load.
        m_header.LoadHeader(Stream);

        Initialize();

        m_header.LoadHeader(Stream);
    }

    internal void InitializeCreate(EncodingDefinition treeNodeType, int blockSize)
    {
        if (m_isInitialized)
            throw new Exception("Duplicate calls to Initialize");

        if (treeNodeType is null)
            throw new ArgumentNullException(nameof(treeNodeType));

        m_isInitialized = true;

        m_header.TreeNodeType = treeNodeType;
        m_header.BlockSize = blockSize;

        m_header.RootNodeLevel = 0;
        m_header.RootNodeIndexAddress = 1;
        m_header.LastAllocatedBlock = 1;

        Initialize();

        LeafStorage.CreateEmptyNode(m_header.RootNodeIndexAddress);
        m_header.IsDirty = true;
        m_header.SaveHeader(Stream);
    }

    private void Initialize()
    {
        Indexer = new SparseIndex<TKey>();
        LeafStorage = Library.CreateTreeNode<TKey, TValue>(m_header.TreeNodeType, 0);
        Indexer.RootHasChanged += IndexerOnRootHasChanged;
        Indexer.Initialize(Stream, m_header.BlockSize, GetNextNewNodeIndex, m_header.RootNodeLevel, m_header.RootNodeIndexAddress);
        LeafStorage.Initialize(Stream, m_header.BlockSize, GetNextNewNodeIndex, Indexer);
    }

    private void IndexerOnRootHasChanged(object sender, EventArgs eventArgs)
    {
        m_header.RootNodeLevel = Indexer.RootNodeLevel;
        m_header.RootNodeIndexAddress = Indexer.RootNodeIndexAddress;
        SetDirtyFlag();
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Opens a sorted tree using the provided stream.
    /// </summary>
    /// <param name="stream">Source stream to open.</param>
    /// <returns>Sorted tree using the provided stream.</returns>
    public static SortedTree<TKey, TValue> Open(BinaryStreamPointerBase stream)
    {
        SortedTree<TKey, TValue> tree = new(stream);
        tree.InitializeOpen();
        return tree;
    }

    /// <summary>
    /// Creates a new fixed size SortedTree using the provided stream.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="blockSize">Block size.</param>
    /// <returns>New fixed size SortedTree using the provided stream.</returns>
    public static SortedTree<TKey, TValue> Create(BinaryStreamPointerBase stream, int blockSize)
    {
        return Create(stream, blockSize, EncodingDefinition.FixedSizeCombinedEncoding);
    }

    /// <summary>
    /// Creates a new SortedTree writing to the provided streams and using the specified compression method for the tree node.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="blockSize">Block size.</param>
    /// <param name="treeNodeType">Encoding definition.</param>
    /// <returns>New SortedTree writing to the provided streams and using the specified compression method for the tree node.</returns>
    public static SortedTree<TKey, TValue> Create(BinaryStreamPointerBase stream, int blockSize, EncodingDefinition treeNodeType)
    {
        if (treeNodeType is null)
            throw new ArgumentNullException(nameof(treeNodeType));

        SortedTree<TKey, TValue> tree = new(stream);
        tree.InitializeCreate(treeNodeType, blockSize);

        return tree;
    }

    #endregion
}