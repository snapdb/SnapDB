//******************************************************************************************************
//  TransactionalEdit.cs - Gbtc
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
//  12/02/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.Diagnostics;
using SnapDB.Immutables;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Provides the state information for a transaction on the file system.
/// </summary>
/// <remarks>Failing to call Commit or Rollback will inhibit additional transactions to be acquired.</remarks>
public sealed class TransactionalEdit : IDisposable
{
    #region [ Members ]

    // The underlying diskIO to do the read and write against.
    private readonly DiskIo m_dataReader;

    // This delegate is called when the Commit function is called and all the data has been written to the underlying file system.
    // the purpose of this delegate is to notify the calling class that this transaction is concluded since
    // only one write transaction can be acquired at a time.
    private Action? m_delHasBeenCommitted;

    // This delegate is called when the RollBack function is called. This also occurs when the object is disposed.
    // the purpose of this delegate is to notify the calling class that this transaction is concluded since
    // only one write transaction can be acquired at a time.
    private Action? m_delHasBeenRolledBack;

    // The readonly snapshot of the archive file.
    private readonly FileHeaderBlock m_fileHeaderBlock;

    // All files that have ever been opened.
    private readonly List<SubFileStream?> m_openedFiles;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates an editable copy of the transaction.
    /// </summary>
    /// <param name="dataReader">The DiskIo instance to read data from.</param>
    /// <param name="delHasBeenRolledBack">The delegate to call when this transaction has been rolled back.</param>
    /// <param name="delHasBeenCommitted">The delegate to call when this transaction has been committed.</param>
    internal TransactionalEdit(DiskIo dataReader, Action? delHasBeenRolledBack = null, Action? delHasBeenCommitted = null)
    {
        if (dataReader is null)
            throw new ArgumentNullException(nameof(dataReader));

        m_openedFiles = new List<SubFileStream?>();
        m_disposed = false;
        m_fileHeaderBlock = dataReader.LastCommittedHeader.CloneEditable();
        m_dataReader = dataReader;
        m_delHasBeenCommitted = delHasBeenCommitted;
        m_delHasBeenRolledBack = delHasBeenRolledBack;
    }

#if DEBUG
    /// <summary>
    /// Finalizes an instance of the <see cref="TransactionalEdit"/> class.
    /// </summary>
    /// <remarks>
    /// This finalizer is automatically called by the garbage collector during object cleanup.
    /// It publishes an informational log message indicating that the finalizer has been called.
    /// </remarks>
    ~TransactionalEdit()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// A list of all of the files in this collection.
    /// </summary>
    public ImmutableList<SubFileHeader?> Files
    {
        get
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            return m_fileHeaderBlock.Files;
        }
    }

    /// <summary>
    /// The GUID for this archive type.
    /// </summary>
    public Guid ArchiveType
    {
        get => m_fileHeaderBlock.ArchiveType;
        set => m_fileHeaderBlock.ArchiveType = value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="ReadSnapshot"/> object.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
            return;

        RollbackAndDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates and opens a new file on the current file system.
    /// </summary>
    public SubFileStream CreateFile(SubFileName fileName)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        m_fileHeaderBlock.CreateNewFile(fileName);

        return OpenFile(fileName);
    }

    /// <summary>
    /// Opens a ArchiveFileStream that can be used to read/write to the file passed to this function.
    /// </summary>
    /// <param name="fileIndex">The index of the file to open.</param>
    public SubFileStream OpenFile(int fileIndex)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (fileIndex < 0 || fileIndex >= m_fileHeaderBlock.Files.Count)
            throw new ArgumentOutOfRangeException(nameof(fileIndex), "The file index provided could not be found in the header.");

        SubFileHeader? subFile = m_fileHeaderBlock.Files[fileIndex];
        SubFileStream fileStream = new(m_dataReader, subFile, m_fileHeaderBlock, false);

        m_openedFiles.Add(fileStream);

        return fileStream;
    }

    /// <summary>
    /// Opens a ArchiveFileStream that can be used to read and write to the file passed to this function.
    /// </summary>
    public SubFileStream OpenFile(SubFileName fileName)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        for (int x = 0; x < Files.Count; x++)
        {
            SubFileHeader? file = Files[x];

            if (file?.FileName == fileName)
                return OpenFile(x);
        }

        throw new Exception("File does not exist");
    }

    /// <summary>
    /// This will cause the transaction to be written to the database.
    /// Also calls Dispose().
    /// </summary>
    /// <remarks>
    /// Duplicate calls to this function, or subsequent calls to RollbackTransaction will throw an exception.
    /// </remarks>
    public void CommitAndDispose()
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        foreach (SubFileStream? file in m_openedFiles)
            if (file is not null && !file.IsDisposed)
                throw new Exception("Not all files have been properly disposed.");

        try
        {
            // TODO: First commit the data, then the file system.
            m_dataReader.CommitChanges(m_fileHeaderBlock);
            m_delHasBeenCommitted?.Invoke();
        }
        finally
        {
            m_delHasBeenCommitted = null;
            m_delHasBeenRolledBack = null;
            m_disposed = true;

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// This will rollback the transaction by not writing the table of contents to the file.
    /// </summary>
    /// <remarks>Duplicate calls to this function, or subsequent calls to CommitTransaction will throw an exception.</remarks>
    public void RollbackAndDispose()
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        foreach (SubFileStream? file in m_openedFiles)
            if (file is not null && !file.IsDisposed)
                file.Dispose();

        try
        {
            m_dataReader.RollbackChanges();
            m_delHasBeenRolledBack?.Invoke();
        }

        finally
        {
            m_delHasBeenCommitted = null;
            m_delHasBeenRolledBack = null;
            m_disposed = true;

            GC.SuppressFinalize(this);
        }
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(TransactionalEdit), MessageClass.Component);

    #endregion
}