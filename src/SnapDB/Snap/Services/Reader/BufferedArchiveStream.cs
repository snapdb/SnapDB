//******************************************************************************************************
//  BufferedArchiveStream'2.cs - Gbtc
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
//  10/25/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;

namespace SnapDB.Snap.Services.Reader;

/// <summary>
/// Represents a buffered stream for reading data from an archive table, where TKey and TValue are specific SnapTypeBase types.
/// </summary>
/// <typeparam name="TKey">The key type for the archive data.</typeparam>
/// <typeparam name="TValue">The value type for the archive data.</typeparam>
public class BufferedArchiveStream<TKey, TValue> : IDisposable where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    /// <summary>
    /// Gets or sets a flag indicating whether the cache is valid.
    /// </summary>
    public bool CacheIsValid;

    /// <summary>
    /// Gets or sets the cached key.
    /// </summary>
    public TKey CacheKey = new();

    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public TValue CacheValue = new();

    /// <summary>
    /// Gets or sets the sorted tree scanner used for reading data.
    /// </summary>
    public SortedTreeScannerBase<TKey, TValue> Scanner;

    private SortedTreeTableReadSnapshot<TKey, TValue> m_snapshot;

    private readonly ArchiveTableSummary<TKey, TValue> m_table;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates an instance of the BufferedArchiveStream class with the specified index and table.
    /// </summary>
    /// <param name="index">The index value used to disassociate the archive file.</param>
    /// <param name="table">The ArchiveTableSummary associated with the stream.</param>
    public BufferedArchiveStream(int index, ArchiveTableSummary<TKey, TValue> table)
    {
        Index = index;
        m_table = table;
        m_snapshot = m_table.ActiveSnapshotInfo.CreateReadSnapshot();
        Scanner = m_snapshot.GetTreeScanner();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// An index value that is used to disassociate the archive file.
    /// </summary>
    public int Index { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Disposes of the resources used by the BufferedArchiveStream.
    /// </summary>
    public void Dispose()
    {
        if (m_snapshot is not null)
        {
            m_snapshot.Dispose();
            m_snapshot = null;
        }
    }

    /// <summary>
    /// Updates the cached value by peeking at the next key-value pair in the scanner.
    /// </summary>
    public void UpdateCachedValue()
    {
        CacheIsValid = Scanner.Peek(CacheKey, CacheValue);
    }

    /// <summary>
    /// Skips to the next key in the scanner and updates the cached value.
    /// </summary>
    public void SkipToNextKeyAndUpdateCachedValue()
    {
        CacheIsValid = Scanner.Read(CacheKey, CacheValue);
        CacheIsValid = Scanner.Peek(CacheKey, CacheValue);
    }

    /// <summary>
    /// Seeks to the specified key in the scanner and updates the cached value.
    /// </summary>
    /// <param name="key">The key to seek to.</param>
    public void SeekToKeyAndUpdateCacheValue(TKey key)
    {
        Scanner.SeekToKey(key);
        CacheIsValid = Scanner.Peek(CacheKey, CacheValue);
    }

    #endregion
}