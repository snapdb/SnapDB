﻿//******************************************************************************************************
//  SequentialSortedTreeWriter`2.cs - Gbtc
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

using SnapDB.IO;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Tree.Specialized;

/// <summary>
/// A specialized serialization method for writing data to a disk in the SortedTreeStore method.
/// </summary>
/// <typeparam name="TKey">The type of keys stored in the tree.</typeparam>
/// <typeparam name="TValue">The type of values stored in the tree.</typeparam>
public static class SequentialSortedTreeWriter<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Static ]

    /// <summary>
    /// Writes the supplied stream to the binary stream.
    /// </summary>
    /// <param name="stream">The stream to store the sorted tree structure.</param>
    /// <param name="blockSize">The size of each block in the stream.</param>
    /// <param name="treeNodeType">The encoding definition for tree node data.</param>
    /// <param name="treeStream">The tree stream to initialize the sorted tree structure.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/>, <paramref name="treeStream"/>, or <paramref name="treeNodeType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="treeStream"/> does not guarantee sequential reads or contains duplicates.</exception>
    public static void Create(BinaryStreamPointerBase stream, int blockSize, EncodingDefinition treeNodeType, TreeStream<TKey, TValue> treeStream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        if (treeStream is null)
            throw new ArgumentNullException(nameof(stream));
        if (treeNodeType is null)
            throw new ArgumentNullException(nameof(treeNodeType));
        if (!(treeStream.IsAlwaysSequential && treeStream.NeverContainsDuplicates))
            throw new ArgumentException("Stream must guarantee sequential reads and that it never will contain a duplicate", nameof(treeStream));

        SortedTreeHeader header = new()
        {
            TreeNodeType = treeNodeType,
            BlockSize = blockSize,
            RootNodeLevel = 0,
            RootNodeIndexAddress = 1,
            LastAllocatedBlock = 1
        };

        uint GetNextNewNodeIndex()
        {
            header.LastAllocatedBlock++;
            return header.LastAllocatedBlock;
        }

        SparseIndexWriter<TKey> indexer = new();

        NodeWriter<TKey, TValue>.Create(treeNodeType, stream, header.BlockSize, header.RootNodeLevel, header.RootNodeIndexAddress, GetNextNewNodeIndex, indexer, treeStream);

        while (indexer.Count > 0)
        {
            indexer.SwitchToReading();
            header.RootNodeLevel++;
            header.RootNodeIndexAddress = GetNextNewNodeIndex();

            SparseIndexWriter<TKey> indexer2 = new();
            NodeWriter<TKey, SnapUInt32>.Create(EncodingDefinition.FixedSizeCombinedEncoding, stream, header.BlockSize, header.RootNodeLevel, header.RootNodeIndexAddress, GetNextNewNodeIndex, indexer2, indexer);

            indexer.Dispose();
            indexer = indexer2;
        }

        indexer.Dispose();

        header.IsDirty = true;
        header.SaveHeader(stream);
    }

    #endregion
}