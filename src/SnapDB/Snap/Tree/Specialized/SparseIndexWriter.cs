//******************************************************************************************************
//  SparseIndexWriter`1.cs - Gbtc
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
//  10/18/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/29/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Tree.Specialized;

/// <summary>
/// Contains information on how to parse the index nodes of the SortedTree
/// </summary>
public sealed class SparseIndexWriter<TKey> : TreeStream<TKey, SnapUInt32> where TKey : SnapTypeBase<TKey>, new()
{
    #region [ Members ]

    private bool m_isReading;

    private long m_readingCount;

    private readonly BinaryStreamPointerBase m_stream;
    private readonly SnapUInt32 m_value = new();

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new sparse index.
    /// </summary>
    public SparseIndexWriter()
    {
        m_stream = new BinaryStream(true);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the number of nodes in the sparse index.
    /// </summary>
    public long Count { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the data source is always sequential.
    /// </summary>
    /// <remarks>
    /// When this property is <c>true</c>, it means that the data source maintains a sequential order for its elements.
    /// In other words, the elements are stored and retrieved in a fixed order that does not change.
    /// </remarks>
    public override bool IsAlwaysSequential => true;

    /// <summary>
    /// Gets a value indicating whether the data source never contains duplicate elements.
    /// </summary>
    /// <remarks>
    /// When this property is <c>true</c>, it means that the data source does not allow duplicate elements.
    /// Each key in the data source is unique, and attempts to add duplicate keys may be ignored or overwritten.
    /// </remarks>
    public override bool NeverContainsDuplicates => true;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    /// <param name="disposing">A flag indicating whether to release both managed and unmanaged resources (<c>true</c>), or only unmanaged resources (<c>false</c>).</param>
    /// <remarks>
    /// This method is called by the <see cref="Dispose"/> method and the finalizer to release the resources used by the current instance of the class.
    /// It disposes of the underlying stream.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        m_stream.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// Adds the following node pointer to the sparse index.
    /// </summary>
    /// <param name="leftPointer">The pointer to the left element, Only used to prime the list.</param>
    /// <param name="nodeKey">the first key in the <see cref="pointer"/>. Only uses the key portion of the TKeyValue</param>
    /// <param name="pointer">the index of the later node</param>
    /// <remarks>
    /// This class will add the new node data to the parent node,
    /// or create a new root if the current root is split.
    /// </remarks>
    public void Add(uint leftPointer, TKey nodeKey, uint pointer)
    {
        if (m_isReading)
            throw new Exception("This sparse index writer has already be set in reading mode.");
        if (Count == 0)
        {
            TKey tmpKey = new();
            tmpKey.SetMin();
            tmpKey.Write(m_stream);
            m_value.Value = leftPointer;
            m_value.Write(m_stream);
            Count++;
        }

        nodeKey.Write(m_stream);
        m_value.Value = pointer;
        m_value.Write(m_stream);
        Count++;
    }

    public void SwitchToReading()
    {
        if (m_isReading)
            throw new Exception("Duplicate call.");
        m_isReading = true;
        m_stream.Position = 0;
    }

    /// <summary>
    /// Reads the next key-value pair from the data source.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a key-value pair was successfully read; otherwise, <c>false</c> if the end of the data source is reached.
    /// </returns>
    /// <remarks>
    /// This method is used to sequentially read key-value pairs from the data source.
    /// It should be called after switching to reading mode using <see cref="SwitchToReading"/> method.
    /// </remarks>
    /// <paramref name="key">
    /// The key to read./>
    /// <paramref name="value">The value to read.<paramref/>
    protected override bool ReadNext(TKey key, SnapUInt32 value)
    {
        if (!m_isReading)
            throw new Exception("Must call SwitchToReading() first.");

        if (m_readingCount < Count)
        {
            m_readingCount++;
            key.Read(m_stream);
            value.Read(m_stream);

            return true;
        }

        return false;
    }

    #endregion
}