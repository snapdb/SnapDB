//******************************************************************************************************
//  TransactionalFileStructure.cs - Gbtc
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
//  10/14/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using Gemstone.Diagnostics;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// This class is responsible for managing the transactions that occur on the file system.
/// Therefore, it keeps up with the latest snapshot of the file allocation table,
/// permits only a single concurrent edit of the archive system, and determines when a file
/// can be deleted when there are no read or write transactions. It also containst the IO system.
/// </summary>
public class TransactionalFileStructure : IDisposable
{
    #region [ Members ]

    /// <summary>
    /// Contains the current read transaction.
    /// </summary>
    private ReadSnapshot m_currentReadTransaction;

    /// <summary>
    /// Contains the current active transaction. If this <c>null</c>, there is no active transaction.
    /// </summary>
    private TransactionalEdit? m_currentTransaction;

    /// <summary>
    /// Contains the disk IO subsystem for accessing the file.
    /// </summary>
    private DiskIo m_diskIo;

    /// <summary>
    /// Determines if this object has been disposed.
    /// </summary>
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    private TransactionalFileStructure(DiskIo diskIo)
    {
        m_diskIo = diskIo;
        m_currentReadTransaction = new ReadSnapshot(diskIo);
    }

#if DEBUG
    /// <summary>
    /// Finalizes an instance of the <see cref="TransactionalFileStructure"/> class.
    /// </summary>
    /// <remarks>
    /// This finalizer is automatically called by the garbage collector during object cleanup.
    /// It publishes an informational log message indicating that the finalizer has been called.
    /// </remarks>
    ~TransactionalFileStructure()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the current size of the archive.
    /// </summary>
    public long ArchiveSize => m_diskIo.FileSize;

    /// <summary>
    /// Gets the file name for the <see cref="TransactionalFileStructure"/>
    /// </summary>
    public string FileName => m_diskIo.FileName;

    /// <summary>
    /// Gets the last committed read snapshot on the file system.
    /// </summary>
    public ReadSnapshot Snapshot => m_currentReadTransaction;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="TransactionalFileStructure"/> object.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
            return;

        try
        {
            if (m_currentTransaction is not null)
            {
                m_currentTransaction.Dispose();
                m_currentTransaction = null;
            }

            if (m_diskIo is not null)
            {
                m_diskIo.Dispose();
                m_diskIo = null!;
            }
        }

        finally
        {
            GC.SuppressFinalize(this);
            m_disposed = true; // Prevent duplicate dispose.
        }
    }

    /// <summary>
    /// Starts a transactional edit on the specified file.
    /// </summary>
    /// <returns>The file being transactionally edited.</returns>
    /// <exception cref="Exception">Thrown if there already exists one edit transaction, or it has been opened in read-only mode.</exception>
    public TransactionalEdit BeginEdit()
    {
        if (m_diskIo.IsReadOnly)
            throw new Exception("File has been opened in readonly mode");

        TransactionalEdit transaction = new(m_diskIo, OnTransactionRolledBack, OnTransactionCommitted);
        Interlocked.CompareExchange(ref m_currentTransaction, transaction, null);

        if (m_currentTransaction != transaction)
            throw new Exception("Only one edit transaction can exist at one time.");

        return m_currentTransaction;
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">The new extension.</param>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_diskIo.ChangeExtension(extension, isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_diskIo.ChangeShareMode(isReadOnly, isSharingEnabled);
    }

    private void OnTransactionCommitted()
    {
        m_currentReadTransaction = new ReadSnapshot(m_diskIo);
        Thread.MemoryBarrier();
        m_currentTransaction = null;
    }

    private void OnTransactionRolledBack()
    {
        m_currentTransaction = null;
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(TransactionalFileStructure), MessageClass.Component);

    /// <summary>
    /// Creates a new archive file that is completely in memory.
    /// </summary>
    /// <param name="blockSize">The size of the data block.</param>
    /// <param name="flags">Flags associated with the specified disk.</param>
    /// <returns>The new disk created in memory.</returns>
    public static TransactionalFileStructure CreateInMemory(int blockSize, params Guid[] flags)
    {
        DiskIo disk = DiskIo.CreateMemoryFile(Globals.MemoryPool, blockSize, flags);

        return new TransactionalFileStructure(disk);
    }

    /// <summary>
    /// Creates a new archive editable file using the provided file.
    /// </summary>
    /// <param name="fileName">The name of the new file.</param>
    /// <param name="blockSize">The size of the data block.</param>
    /// <param name="flags">The flags associated with the specified new file.</param>
    /// <returns>The new archive editable file.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the file name is null.</exception>
    /// <exception cref="Exception">Thrown if the name of the new file already exists.</exception>
    public static TransactionalFileStructure CreateFile(string fileName, int blockSize, params Guid[] flags)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        if (File.Exists(fileName))
            throw new Exception("fileName Already Exists");

        DiskIo disk = DiskIo.CreateFile(fileName, Globals.MemoryPool, blockSize, flags);

        return new TransactionalFileStructure(disk);
    }

    /// <summary>
    /// Opens an existing file.
    /// </summary>
    /// <param name="fileName">The name of the existing file to open.</param>
    /// <param name="isReadOnly">A boolean that tells us whether or not the file is read-only.</param>
    /// <returns><c>true</c> if the file is read-only; otherwise, <c>false</c> if not.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the file name is null.</exception>
    /// <exception cref="Exception">Thrown if the file name does not exist.</exception>
    public static TransactionalFileStructure OpenFile(string fileName, bool isReadOnly)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        if (!File.Exists(fileName))
            throw new Exception("fileName Does Not Exists");

        DiskIo disk = DiskIo.OpenFile(fileName, Globals.MemoryPool, isReadOnly);

        if (!isReadOnly && disk.LastCommittedHeader.IsSimplifiedFileFormat)
        {
            disk.Dispose();

            throw new Exception("Cannot open a simplified file structure with write support.");
        }

        return new TransactionalFileStructure(disk);
    }

    #endregion
}