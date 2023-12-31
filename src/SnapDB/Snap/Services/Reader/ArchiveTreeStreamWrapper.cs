﻿//******************************************************************************************************
//  ArchiveTreeStreamWrapper'2.cs - Gbtc
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
//  10/19/2014 - Steven E. Chisholm
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
/// Wraps a <see cref="ArchiveTableSummary{TKey,TValue}"/> within a <see cref="TreeStream{TKey,TValue}"/>
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ArchiveTreeStreamWrapper<TKey, TValue> : TreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly SortedTreeScannerBase<TKey, TValue> m_scanner;
    private SortedTreeTableReadSnapshot<TKey, TValue> m_snapshot;
    private readonly ArchiveTableSummary<TKey, TValue> m_table;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="ArchiveTreeStreamWrapper{TKey,TValue}"/>
    /// </summary>
    /// <param name="table">The table to wrap.</param>
    public ArchiveTreeStreamWrapper(SortedTreeTable<TKey, TValue> table) : this(new ArchiveTableSummary<TKey, TValue>(table))
    {
    }

    /// <summary>
    /// Creates a <see cref="ArchiveTreeStreamWrapper{TKey,TValue}"/>
    /// </summary>
    /// <param name="table">The table to wrap.</param>
    public ArchiveTreeStreamWrapper(ArchiveTableSummary<TKey, TValue> table)
    {
        m_table = table;
        m_snapshot = m_table.ActiveSnapshotInfo.CreateReadSnapshot();
        m_scanner = m_snapshot.GetTreeScanner();
        m_scanner.SeekToStart();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets a value indicating whether the data source is always sequential.
    /// </summary>
    public override bool IsAlwaysSequential => true;

    /// <summary>
    /// Gets a value indicating that the data source never contains duplicates.
    /// </summary>
    public override bool NeverContainsDuplicates => true;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ArchiveTreeStreamWrapper{TKey,TValue}"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!m_disposed)
            try
            {
                // This will be done regardless of whether the object is finalized or disposed.
                if (disposing)
                {
                    // This will be done only when the object is disposed by calling Dispose().
                    m_snapshot.Dispose();
                    m_snapshot = null;
                }
            }
            finally
            {
                m_disposed = true; // Prevent duplicate dispose.
                base.Dispose(disposing); // Call base class Dispose().
            }
    }

    /// <summary>
    /// Reads the next key-value pair from the underlying data source using the scanner.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="value">The value to read.</param>
    /// <returns>True if the read operation was successful, otherwise false.</returns>
    protected override bool ReadNext(TKey key, TValue value)
    {
        return m_scanner.Read(key, value);
    }

    #endregion
}