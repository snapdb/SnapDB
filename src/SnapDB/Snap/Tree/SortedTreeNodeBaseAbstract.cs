//******************************************************************************************************
//  SortedTreeNodeBase_Abstract`2.cs - Gbtc
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

namespace SnapDB.Snap.Tree;

public partial class SortedTreeNodeBase<TKey, TValue>
{
    #region [ Properties ]

    /// <summary>
    /// Gets the maximum overhead allowed when combining nodes during tree operations.
    /// </summary>
    protected abstract int MaxOverheadWithCombineNodes { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Initializes the specific type of tree node.
    /// </summary>
    protected abstract void InitializeType();

    /// <summary>
    /// Reads the value at the specified index and stores it in the provided <paramref name="value"/>.
    /// </summary>
    /// <param name="index">The index of the value to read.</param>
    /// <param name="value">The value to store the read data.</param>
    protected abstract void Read(int index, TValue value);

    /// <summary>
    /// Reads both the key and value at the specified index and stores them in the provided <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <param name="index">The index of the key-value pair to read.</param>
    /// <param name="key">The key to store the read key data.</param>
    /// <param name="value">The value to store the read value data.</param>
    protected abstract void Read(int index, TKey key, TValue value);

    /// <summary>
    /// Removes the element at the specified index unless it causes an overflow in the node.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <returns><c><c>true</c></c> if the element was removed; otherwise, <c><c>false</c></c>.</returns>
    protected abstract bool RemoveUnlessOverflow(int index);

    /// <summary>
    /// Inserts the provided key and value into the current node unless it is full.
    /// Note: A duplicate key has already been detected and will never be passed to this function.
    /// </summary>
    /// <param name="index">The index where the key-value pair should be inserted.</param>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to insert.</param>
    /// <returns><c><c>true</c></c> if the insertion was successful; otherwise, <c><c>false</c></c>.</returns>
    protected abstract bool InsertUnlessFull(int index, TKey key, TValue value);

    /// <summary>
    /// Requests the insertion of the current stream into the tree. Sequential insertion can only occur while the stream
    /// is in order and entirely past the end of the tree.
    /// </summary>
    /// <param name="stream">The stream data to insert.</param>
    /// <param name="isFull">
    /// If returning from this function while the node is not yet full, this means the stream
    /// can no longer be inserted sequentially, and we must break out to the root and insert one at a time.
    /// </param>
    protected abstract void AppendSequentialStream(InsertStreamHelper<TKey, TValue> stream, out bool isFull);

    /// <summary>
    /// Gets the index of the specified key in the node.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The index of the key if found; otherwise, a negative value.</returns>
    protected abstract int GetIndexOf(TKey key);

    /// <summary>
    /// Splits the node into two nodes, creating a new node with the specified <paramref name="newNodeIndex"/> and <paramref name="dividingKey"/>.
    /// </summary>
    /// <param name="newNodeIndex">The index of the new node.</param>
    /// <param name="dividingKey">The key that divides the node during the split.</param>
    protected abstract void Split(uint newNodeIndex, TKey dividingKey);

    /// <summary>
    /// Transfers records from the right node to the left node, moving <paramref name="bytesToTransfer"/> bytes.
    /// </summary>
    /// <param name="left">The left node.</param>
    /// <param name="right">The right node.</param>
    /// <param name="bytesToTransfer">The number of bytes to transfer from right to left.</param>
    protected abstract void TransferRecordsFromRightToLeft(Node<TKey> left, Node<TKey> right, int bytesToTransfer);

    /// <summary>
    /// Transfers records from the left node to the right node, moving <paramref name="bytesToTransfer"/> bytes.
    /// </summary>
    /// <param name="left">The left node.</param>
    /// <param name="right">The right node.</param>
    /// <param name="bytesToTransfer">The number of bytes to transfer from left to right.</param>
    protected abstract void TransferRecordsFromLeftToRight(Node<TKey> left, Node<TKey> right, int bytesToTransfer);

    #endregion
}