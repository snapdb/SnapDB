﻿//******************************************************************************************************
//  SortedTreeTable.cs - Gbtc
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
//  05/19/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure;

namespace SnapDB.Snap.Storage;

/// <summary>
/// Represents an individual table contained within the file.
/// </summary>
/// <typeparam name="TKey">The key type used in the sorted tree table.</typeparam>
/// <typeparam name="TValue">The value type used in the sorted tree table.</typeparam>
public partial class SortedTreeTable<TKey, TValue> : IDisposable where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private Editor m_activeEditor;

    private readonly SubFileName m_fileName;
    private readonly TransactionalFileStructure m_fileStructure;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="SortedTreeTable{TKey, TValue}"/> class
    /// with the specified file structure, file name, and base file.
    /// </summary>
    /// <param name="fileStructure">The transactional file structure associated with the table.</param>
    /// <param name="fileName">The subfile name of the table.</param>
    /// <param name="baseFile">The base file associated with the table.</param>
    internal SortedTreeTable(TransactionalFileStructure fileStructure, SubFileName fileName, SortedTreeFile baseFile)
    {
        BaseFile = baseFile;
        m_fileName = fileName;
        m_fileStructure = fileStructure;
        FirstKey = new TKey();
        LastKey = new TKey();
        using SortedTreeTableReadSnapshot<TKey, TValue> snapshot = BeginRead();
        snapshot.GetKeyRange(FirstKey, LastKey);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the unique identifier (ID) of the archive associated with this table.
    /// </summary>
    public Guid ArchiveId => BaseFile.Snapshot.Header.ArchiveId;

    /// <summary>
    /// Gets the archive file where this table exists.
    /// </summary>
    public SortedTreeFile BaseFile { get; }

    /// <summary>
    /// The first key.  Note: Values only update on commit.
    /// </summary>
    public TKey FirstKey { get; }

    /// <summary>
    /// Determines if the archive file has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The last key.  Note: Values only update on commit.
    /// </summary>
    public TKey LastKey { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Closes the archive file. If there is a current transaction,
    /// that transaction is immediately rolled back and disposed.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            m_activeEditor?.Dispose();
            m_fileStructure.Dispose();
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Acquires a read snapshot of the current archive file.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="SortedTreeTableSnapshotInfo{TKey, TValue}"/> representing the acquired read snapshot.
    /// </returns>
    /// <remarks>
    /// Once the snapshot has been acquired, any future commits
    /// will not effect this snapshot. The snapshot has a tiny footprint
    /// and allows an unlimited number of reads that can be created.
    /// </remarks>
    public SortedTreeTableSnapshotInfo<TKey, TValue> AcquireReadSnapshot()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        return new SortedTreeTableSnapshotInfo<TKey, TValue>(m_fileStructure, m_fileName);
    }

    /// <summary>
    /// Allows the user to get a read snapshot on the table.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="SortedTreeTableReadSnapshot{TKey, TValue}"/> representing the read snapshot.
    /// </returns>
    public SortedTreeTableReadSnapshot<TKey, TValue> BeginRead()
    {
        return AcquireReadSnapshot().CreateReadSnapshot();
    }

    /// <summary>
    /// Begins an edit of the current archive table.
    /// </summary>
    /// <remarks>
    /// Concurrent editing of a file is not supported. Subsequent calls will
    /// throw an exception rather than blocking. This is to encourage
    /// proper synchronization at a higher layer.
    /// Wrap the return value of this function in a Using block so the dispose
    /// method is always called.
    /// </remarks>
    /// <example>
    /// using (ArchiveFile.ArchiveFileEditor editor = archiveFile.BeginEdit())
    /// {
    /// editor.AddPoint(key, value);
    /// editor.AddPoint(key, value);
    /// editor.Commit();
    /// }
    /// </example>
    /// <returns>The current status of the edit of the archive table.</returns>
    public SortedTreeTableEditor<TKey, TValue> BeginEdit()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_activeEditor is not null)
            throw new Exception("Only one concurrent edit is supported");

        m_activeEditor = new Editor(this);

        return m_activeEditor;
    }

    #endregion

    ///// <summary>
    ///// Closes and deletes the Archive File. Also calls dispose.
    ///// If this is a memory archive, it will release the memory space to the buffer pool.
    ///// </summary>
}