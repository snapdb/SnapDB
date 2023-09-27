//******************************************************************************************************
//  DiskMedium.cs - Gbtc
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
//  02/22/2013 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.Unmanaged;
using Gemstone.Diagnostics;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Provides read and write access to all of the different types of disk types
/// to use to store the file structure.
/// </summary>
internal class DiskMedium
    : IDisposable
{

    private static readonly LogPublisher Log = Logger.CreatePublisher(typeof(DiskMedium), MessageClass.Component);

    #region [ Members ]

    /// <summary>
    /// The file block header.
    /// </summary>
    private FileHeaderBlock m_header;
    /// <summary>
    /// The underlying disk implementation.
    /// </summary>
    private IDiskMediumCoreFunctions m_disk;
    /// <summary>
    /// The number of bytes in a given block. Typically 4KB in size.
    /// </summary>
    private readonly int m_blockSize;
    /// <summary>
    /// Prevents duplicate calls to dispose.
    /// </summary>
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Class is created through static methods of this class.
    /// </summary>
    /// <param name="disk">The underlying disk medium.</param>
    /// <param name="header">The header data to use.</param>
    private DiskMedium(IDiskMediumCoreFunctions disk, FileHeaderBlock header)
    {
        m_header = header;
        m_disk = disk;
        m_blockSize = header.BlockSize;
    }

    #endregion

#if DEBUG
    ~DiskMedium()
    {
        Log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #region [ Properties ]

    /// <summary>
    /// Gets the current number of bytes used by the file system. 
    /// This is only intended to be an approximate figure. 
    /// </summary>
    public long Length => m_disk.Length;

    /// <summary>
    /// Gets the most recent committed header from the archive file.
    /// </summary>
    public FileHeaderBlock Header => m_header;

    /// <summary>
    /// Gets the number of bytes in the file structure block size.
    /// </summary>
    public int BlockSize => m_blockSize;

    public string FileName => m_disk.FileName;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Occurs when rolling back a transaction. This will free up
    /// any temporary space allocated for the change. 
    /// </summary>
    public void RollbackChanges()
    {
        m_disk.RollbackChanges();
    }

    /// <summary>
    /// Occurs when committing the following data to the disk.
    /// This will copy any pending data to the disk in a manner that
    /// will protect against corruption.
    /// </summary>
    /// <param name="header"></param>
    public void CommitChanges(FileHeaderBlock header)
    {
        header.IsReadOnly = true;
        m_disk.CommitChanges(header);
        Thread.MemoryBarrier();
        m_header = header;
    }

    /// <summary>
    /// Creates a <see cref="BinaryStreamIoSessionBase"/> that can be used to read from this disk medium.
    /// </summary>
    /// <returns></returns>
    public BinaryStreamIoSessionBase CreateIoSession()
    {
        return m_disk.CreateIoSession();
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">the new extension</param>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_disk.ChangeExtension(extension, isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_disk.ChangeShareMode(isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public void Dispose()
    {
        if (!m_disposed)
        {
            GC.SuppressFinalize(this);
            m_disposed = true;
            m_disk.Dispose();
            m_disk = null;
        }
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a new in-memory disk medium with the specified settings.
    /// </summary>
    /// <param name="pool">The memory pool to use for storage.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    /// <param name="flags">An optional array of GUIDs representing flags to apply to the file header.</param>
    /// <returns>A <see cref="DiskMedium"/> instance representing the newly created in-memory disk medium.</returns>
    /// <remarks>
    /// This method creates a new in-memory disk medium using the provided <paramref name="pool"/> for storage.
    /// The <paramref name="fileStructureBlockSize"/> parameter specifies the block size for the file's structure.
    /// Additionally, optional flags can be passed as an array of <paramref name="flags"/> to apply to the file header.
    /// </remarks>
    /// <param name="pool">The memory pool to use for storage.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    /// <param name="flags">An optional array of GUIDs representing flags to apply to the file header.</param>
    public static DiskMedium CreateMemoryFile(MemoryPool pool, int fileStructureBlockSize, params Guid[] flags)
    {
        FileHeaderBlock header = FileHeaderBlock.CreateNew(fileStructureBlockSize, flags);
        MemoryPoolFile disk = new(pool);

        return new DiskMedium(disk, header);
    }

    /// <summary>
    /// Creates a new disk medium backed by a custom file stream with the specified settings.
    /// </summary>
    /// <param name="stream">The custom file stream to use as the underlying storage.</param>
    /// <param name="pool">The memory pool to use for caching.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    /// <param name="flags">An optional array of GUIDs representing flags to apply to the file header.</param>
    /// <returns>A <see cref="DiskMedium"/> instance representing the newly created disk medium.</returns>
    /// <remarks>
    /// This method creates a new disk medium that uses a custom file stream <paramref name="stream"/>
    /// as the underlying storage. The <paramref name="pool"/> parameter specifies the memory pool to use for caching.
    /// The <paramref name="fileStructureBlockSize"/> parameter specifies the block size for the file's structure.
    /// Additionally, optional flags can be passed as an array of <paramref name="flags"/> to apply to the file header.
    /// </remarks>
    /// <param name="stream">The custom file stream to use as the underlying storage.</param>
    /// <param name="pool">The memory pool to use for caching.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    /// <param name="flags">An optional array of GUIDs representing flags to apply to the file header.</param>
    public static DiskMedium CreateFile(CustomFileStream stream, MemoryPool pool, int fileStructureBlockSize, params Guid[] flags)
    {
        FileHeaderBlock header = FileHeaderBlock.CreateNew(fileStructureBlockSize, flags);
        BufferedFile disk = new(stream, pool, header, isNewFile: true);

        return new DiskMedium(disk, header);
    }

    /// <summary>
    /// Opens an existing disk medium using a custom file stream and specified settings.
    /// </summary>
    /// <param name="stream">The custom file stream representing the existing disk storage.</param>
    /// <param name="pool">The memory pool to use for caching.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    /// <returns>A <see cref="DiskMedium"/> instance representing the opened disk medium.</returns>
    /// <remarks>
    /// This method opens an existing disk medium by reading the file header from the provided
    /// custom file stream <paramref name="stream"/> and using it to initialize the disk medium.
    /// The <paramref name="pool"/> parameter specifies the memory pool to use for caching, and
    /// <paramref name="fileStructureBlockSize"/> specifies the block size for the file's structure.
    /// </remarks>
    /// <param name="stream">The custom file stream representing the existing disk storage.</param>
    /// <param name="pool">The memory pool to use for caching.</param>
    /// <param name="fileStructureBlockSize">The block size for the file's structure.</param>
    public static DiskMedium OpenFile(CustomFileStream stream, MemoryPool pool, int fileStructureBlockSize)
    {
        byte[] buffer = new byte[fileStructureBlockSize];
        stream.ReadRaw(0, buffer, fileStructureBlockSize);
        FileHeaderBlock header = FileHeaderBlock.Open(buffer);
        BufferedFile disk = new(stream, pool, header, isNewFile: false);

        return new DiskMedium(disk, header);
    }

    #endregion
}