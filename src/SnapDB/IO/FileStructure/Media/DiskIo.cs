//******************************************************************************************************
//  DiskIo.cs - Gbtc
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
//  03/24/2012 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.Diagnostics;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// The IO system that the entire file structure uses to accomplish it's IO operations.
/// This class hands data one block at a time to requesting classes
/// and is responsible for checking the footer data of the file for corruption.
/// </summary>
internal sealed class DiskIo : IDisposable
{
    #region [ Members ]

    private readonly bool m_isReadOnly;

    private DiskMedium m_stream;

    #endregion

    #region [ Constructors ]

    private DiskIo(DiskMedium stream, bool isReadOnly)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        m_isReadOnly = isReadOnly;
        BlockSize = stream.BlockSize;
        m_stream = stream;
    }

#if DEBUG
    ~DiskIo()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the number of bytes in a single block.
    /// </summary>
    public int BlockSize { get; }

    public string FileName => m_stream.FileName;

    /// <summary>
    /// Gets the current size of the file.
    /// </summary>
    public long FileSize
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return m_stream.Length;
        }
    }

    /// <summary>
    /// Gets if the class has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets if the disk supports writing.
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return m_isReadOnly;
        }
    }

    /// <summary>
    /// Gets the file header that was the last header to be committed to the disk.
    /// </summary>
    public FileHeaderBlock LastCommittedHeader
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return m_stream.Header;
        }
    }

    /// <summary>
    /// Returns the last block that is read-only.
    /// </summary>
    public uint LastReadonlyBlock
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return m_stream.Header.LastAllocatedBlock;
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="DiskIo"/> object and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        try
        {
            m_stream?.Dispose();
        }

        finally
        {
            GC.SuppressFinalize(this);
            m_stream = null!;
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Occurs when rolling back a transaction. This will free up
    /// any temporary space allocated for the change.
    /// </summary>
    public void RollbackChanges()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_isReadOnly)
            throw new ReadOnlyException();

        m_stream.RollbackChanges();
    }

    /// <summary>
    /// Commits changes made to the file represented by this object with the specified file header block.
    /// </summary>
    /// <param name="header">The file header block containing changes to be committed.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
    /// <exception cref="ReadOnlyException">Thrown if the object is in read-only mode.</exception>
    public void CommitChanges(FileHeaderBlock header)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_isReadOnly)
            throw new ReadOnlyException();

        m_stream.CommitChanges(header);
    }

    /// <summary>
    /// Creates a new <see cref="DiskIoSession"/> for performing disk I/O operations.
    /// </summary>
    /// <param name="header">The file header block associated with the session.</param>
    /// <param name="file">The optional subfile header associated with the session, or <c>null</c> if not applicable.</param>
    /// <returns>A <see cref="DiskIoSession"/> instance for performing disk I/O operations.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the <see cref="DiskIo"/> instance is disposed.</exception>
    public DiskIoSession CreateDiskIoSession(FileHeaderBlock header, SubFileHeader? file)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        return new DiskIoSession(this, m_stream.CreateIoSession(), header, file);
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">The new extension.</param>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_stream.ChangeExtension(extension, isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_stream.ChangeShareMode(isReadOnly, isSharingEnabled);
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(DiskIo), MessageClass.Component);

    public static DiskIo CreateMemoryFile(MemoryPool pool, int fileStructureBlockSize, params Guid[] flags)
    {
        DiskMedium disk = DiskMedium.CreateMemoryFile(pool, fileStructureBlockSize, flags);

        return new DiskIo(disk, false);
    }

    public static DiskIo CreateFile(string fileName, MemoryPool pool, int fileStructureBlockSize, params Guid[] flags)
    {
        // Exclusive opening to prevent duplicate opening.
        CustomFileStream fileStream = CustomFileStream.CreateFile(fileName, pool.PageSize, fileStructureBlockSize);
        DiskMedium disk = DiskMedium.CreateFile(fileStream, pool, fileStructureBlockSize, flags);

        return new DiskIo(disk, false);
    }

    public static DiskIo OpenFile(string fileName, MemoryPool pool, bool isReadOnly)
    {
        CustomFileStream fileStream = CustomFileStream.OpenFile(fileName, pool.PageSize, out int fileStructureBlockSize, isReadOnly, true);
        DiskMedium disk = DiskMedium.OpenFile(fileStream, pool, fileStructureBlockSize);

        return new DiskIo(disk, isReadOnly);
    }

    #endregion
}