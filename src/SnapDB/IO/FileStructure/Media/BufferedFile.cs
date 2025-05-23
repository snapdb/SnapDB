﻿//******************************************************************************************************
//  BufferedFile.cs - Gbtc
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
//  02/01/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

using Gemstone.Diagnostics;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// A buffered file stream utilizes the <see cref="MemoryPool"/> to intellectually cache
/// the contents of files.
/// </summary>
/// <remarks>
/// This class is a special purpose class that can only be used for the <see cref="TransactionalFileStructure"/>
/// and can not buffer general purpose file.
/// The cache algorithm is a least recently used algorithm.
/// This is accomplished by incrementing a counter every time a page is accessed
/// and dividing by 2 every time a collection occurs from the <see cref="MemoryPool"/>.
/// </remarks>
// FUTURE: Consider allowing this class to scale horizontally like how the concurrent dictionary scales.
// FUTURE: this will reduce the concurrent contention on the class at the cost of more memory required.
internal partial class BufferedFile : IDiskMediumCoreFunctions
{
    #region [ Members ]

    /// <summary>
    /// All I/O to the disk is done at this maximum block size. Usually 64KB
    /// This value is equal to the MemoryPool's Page Size.
    /// </summary>
    private readonly int m_diskBlockSize;

    /// <summary>
    /// The size of an individual block of the FileStructure. Usually 4KB.
    /// </summary>
    private readonly int m_fileStructureBlockSize;

    /// <summary>
    /// The number of bytes contained in the committed area of the file.
    /// </summary>
    private long m_lengthOfCommittedData;

    /// <summary>
    /// The length of the 10 header pages.
    /// </summary>
    private readonly long m_lengthOfHeader;

    /// <summary>
    /// Location to store cached memory pages.
    /// This class is thread-safe.
    /// </summary>
    private PageReplacementAlgorithm m_pageReplacementAlgorithm;

    /// <summary>
    /// The <see cref="MemoryPool"/> where the memory pages come from.
    /// </summary>
    private readonly MemoryPool m_pool;

    /// <summary>
    /// Manages all I/O done to the physical file.
    /// </summary>
    private CustomFileStream m_queue;

    /// <summary>
    /// Synchronize all calls to this class.
    /// </summary>
    private readonly Lock m_syncRoot;

    /// <summary>
    /// Any uncommitted data resides in this location.
    /// </summary>
    private MemoryPoolStreamCore m_writeBuffer;

    /// <summary>
    /// Gets if the class has been disposed.
    /// </summary>
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a file backed memory stream.
    /// </summary>
    /// <param name="stream">The <see cref="CustomFileStream"/> to buffer.</param>
    /// <param name="pool">The <see cref="MemoryPool"/> to allocate memory from.</param>
    /// <param name="header">The <see cref="FileHeaderBlock"/> to be managed when modifications occur.</param>
    /// <param name="isNewFile">
    /// Tells if this is a newly created file. This will make sure that the
    /// first 10 pages have the header data copied to it.
    /// </param>
    public BufferedFile(CustomFileStream stream, MemoryPool pool, FileHeaderBlock header, bool isNewFile)
    {
        m_fileStructureBlockSize = header.BlockSize;
        m_diskBlockSize = pool.PageSize;
        m_lengthOfHeader = header.BlockSize * header.HeaderBlockCount;
        m_writeBuffer = new MemoryPoolStreamCore(pool);
        m_pool = pool;
        m_queue = stream;
        m_syncRoot = new Lock();
        m_pageReplacementAlgorithm = new PageReplacementAlgorithm(pool);
        pool.RequestCollection += m_pool_RequestCollection;

        if (isNewFile)
            try
            {
                // Open the file queue for writing.
                m_queue.Open();

                // Serialize the header into bytes.
                byte[] headerBytes = header.GetBytes();

                // Write the header bytes to the file queue for each header block.
                for (int x = 0; x < header.HeaderBlockCount; x++)
                    // Ensure that the file queue is closed even if an exception is thrown
                    m_queue.WriteRaw(0, headerBytes, headerBytes.Length);
            }

            finally
            {
                m_queue.Close();
            }

        m_lengthOfCommittedData = (header.LastAllocatedBlock + 1) * header.BlockSize;
        m_writeBuffer.ConfigureAlignment(m_lengthOfCommittedData, pool.PageSize);
    }

#if DEBUG
    ~BufferedFile()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the file name associated with the medium. Returns an empty string if a memory file.
    /// </summary>
    public string FileName => m_queue.FileName;

    /// <summary>
    /// Gets the current number of bytes used by the file system.
    /// This is only intended to be an approximate figure.
    /// </summary>
    public long Length => m_queue.Length;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
            return;

        try
        {
            m_disposed = true;

            // Unregistering from this event guarantees that a collection will no longer
            // be called since this class utilizes custom code to guarantee this.
            m_pool.RequestCollection -= m_pool_RequestCollection;

            lock (m_syncRoot)
            {
                m_pageReplacementAlgorithm?.Dispose();
                m_queue?.Dispose();
                m_writeBuffer?.Dispose();
            }
        }
        finally
        {
            m_queue = null!;
            m_disposed = true;
            m_pageReplacementAlgorithm = null!;
            m_writeBuffer = null!;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Populates the pointer data inside <paramref name="args"/> for the desired block as specified in <paramref name="args"/>.
    /// This function will block if needing to retrieve data from the disk.
    /// </summary>
    /// <param name="pageLock">The reusable lock information about what this block is currently using.</param>
    /// <param name="args">
    /// Contains what block needs to be read and when this function returns,
    /// it will contain the proper pointer information for this block.
    /// </param>
    private void GetBlock(PageReplacementAlgorithm.PageLock pageLock, BlockArguments args)
    {
        pageLock.Clear();

        // Determines where the block is located.
        if (args.Position >= m_lengthOfCommittedData)
        {
            // If the block is in the uncommitted space, it is stored in the 
            // MemoryPoolStreamCore.
            args.SupportsWriting = true;
            m_writeBuffer.GetBlock(args);
        }
        else if (args.Position < m_lengthOfHeader)
        {
            // If the block is in the header, error because this area of the file is not designed to be accessed.
            throw new ArgumentOutOfRangeException(nameof(args), "Cannot use this function to modify the file header.");
        }
        else
        {
            // If it is between the file header and uncommitted space, 
            // it is in the committed space, which this space by design is never to be modified. 
            if (args.IsWriting)
                throw new ArgumentException("Cannot write to committed data space", nameof(args));

            args.SupportsWriting = false;
            args.Length = m_diskBlockSize;

            // Rounds to the beginning of the block to be looked up.
            args.FirstPosition = args.Position & ~m_pool.PageMask;

            GetBlockFromCommittedSpace(pageLock, args.FirstPosition, out args.FirstPointer);

            // Make sure the block does not go beyond the end of the uncommitted space.
            if (args.FirstPosition + args.Length > m_lengthOfCommittedData)
                args.Length = (int)(m_lengthOfCommittedData - args.FirstPosition);
        }
    }

    /// <summary>
    /// Processes the GetBlock from the committed area.
    /// </summary>
    /// <param name="pageLock">The page lock used for page management.</param>
    /// <param name="position">The position of the block.</param>
    /// <param name="pointer">an output parameter that contains the pointer for the provided position.</param>
    /// <remarks>The valid length is at least the size of the buffer pools page size.</remarks>
    private void GetBlockFromCommittedSpace(PageReplacementAlgorithm.PageLock pageLock, long position, out nint pointer)
    {
        // If the page is in the buffer, we can return and don't have to read it.
        if (pageLock.TryGetSubPage(position, out pointer))
            return;

        // If the address doesn't exist in the current list. Read it from the disk.
        m_pool.AllocatePage(out int poolPageIndex, out nint poolAddress);
        m_queue.Read(position, poolAddress);

        // Since a race condition exists, I need to check the buffer to make sure that 
        // the most recently read page already exists in the PageReplacementAlgorithm.
        pointer = pageLock.GetOrAddPage(position, poolAddress, poolPageIndex, out bool wasPageAdded);

        // If I lost on the race condition, I need to re-release this page.
        if (!wasPageAdded)
            m_pool.ReleasePage(poolPageIndex);
    }

    /// <summary>
    /// Handles the <see cref="MemoryPool.RequestCollection"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The collection event arguments.</param>
    private void m_pool_RequestCollection(object? sender, CollectionEventArgs e)
    {
        if (m_disposed)
            return;

        m_pageReplacementAlgorithm.DoCollection(e);

        if (e.CollectionMode == MemoryPoolCollectionMode.Critical)
            m_pageReplacementAlgorithm.DoCollection(e);
    }

    /// <summary>
    /// Releases the buffered data contained in the buffer pool.
    /// This is accomplished by disposing of the writer and recreating it.
    /// </summary>
    private void ReleaseWriteBufferSpace()
    {
        m_writeBuffer.Dispose();
        m_writeBuffer = new MemoryPoolStreamCore(m_pool);
        m_writeBuffer.ConfigureAlignment(m_lengthOfCommittedData, m_pool.PageSize);
    }

    /// <summary>
    /// Executes a commit of data. This will flush the data to the disk use the provided header data to properly
    /// execute this function.
    /// </summary>
    /// <param name="header">File header block.</param>
    public void CommitChanges(FileHeaderBlock header)
    {
        using (IoSession pageLock = new(this, m_pageReplacementAlgorithm))
        {
            // Determine how much committed data to write
            long lengthOfAllData = (header.LastAllocatedBlock + 1) * m_fileStructureBlockSize;
            long copyLength = lengthOfAllData - m_lengthOfCommittedData;

            // Write the uncommitted data.
            m_queue.Write(m_lengthOfCommittedData, m_writeBuffer, copyLength, true);

            byte[] bytes = header.GetBytes();

            if (header.HeaderBlockCount == 10)
            {
                // Update the new header to position 0, position 1, and one of position 2-9.
                m_queue.WriteRaw(0, bytes, m_fileStructureBlockSize);
                m_queue.WriteRaw(m_fileStructureBlockSize, bytes, m_fileStructureBlockSize);
                m_queue.WriteRaw(m_fileStructureBlockSize * ((header.SnapshotSequenceNumber & 7) + 2), bytes, m_fileStructureBlockSize);
            }
            else
            {
                for (int x = 0; x < header.HeaderBlockCount; x++)
                    m_queue.WriteRaw(x * m_fileStructureBlockSize, bytes, m_fileStructureBlockSize);
            }

            m_queue.FlushFileBuffers();

            long startPos;

            // Copy recently committed data to the buffer pool
            if ((m_lengthOfCommittedData & (m_diskBlockSize - 1)) != 0) // Only if there is a split page.
            {
                startPos = m_lengthOfCommittedData & ~(m_diskBlockSize - 1);

                // Finish filling up the split page in the buffer.
                if (pageLock.TryGetSubPage(startPos, out nint ptrDest))
                {
                    m_writeBuffer.ReadBlock(m_lengthOfCommittedData, out nint ptrSrc, out int length);
                    Footer.WriteChecksumResultsToFooter(ptrSrc, m_fileStructureBlockSize, length);
                    ptrDest += m_diskBlockSize - length;
                    Memory.Copy(ptrSrc, ptrDest, length);
                }

                startPos += m_diskBlockSize;
            }
            else
            {
                startPos = m_lengthOfCommittedData;
            }

            while (startPos < lengthOfAllData)
            {
                // If the address doesn't exist in the current list. Read it from the disk.
                m_pool.AllocatePage(out int poolPageIndex, out nint poolAddress);
                m_writeBuffer.CopyTo(startPos, poolAddress, m_diskBlockSize);

                Footer.WriteChecksumResultsToFooter(poolAddress, m_fileStructureBlockSize, m_diskBlockSize);

                if (!m_pageReplacementAlgorithm.TryAddPage(startPos, poolAddress, poolPageIndex))
                    m_pool.ReleasePage(poolPageIndex);

                startPos += m_diskBlockSize;
            }

            m_lengthOfCommittedData = lengthOfAllData;
        }

        ReleaseWriteBufferSpace();
    }

    /// <summary>
    /// Creates a <see cref="BinaryStreamIoSessionBase"/> that can be used to read from this disk medium.
    /// </summary>
    /// <returns>A new BinaryStreamIoSessionBase instance.</returns>
    public BinaryStreamIoSessionBase CreateIoSession()
    {
        return new IoSession(this, m_pageReplacementAlgorithm);
    }

    /// <summary>
    /// Rolls back all edits to the DiskMedium
    /// </summary>
    public void RollbackChanges()
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().ToString());

        ReleaseWriteBufferSpace();
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">The new extension.</param>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_queue.ChangeExtension(extension, isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_queue.ChangeShareMode(isReadOnly, isSharingEnabled);
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(BufferedFile), MessageClass.Component);

    #endregion
}