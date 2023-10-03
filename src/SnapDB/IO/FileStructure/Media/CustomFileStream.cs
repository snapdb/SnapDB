//******************************************************************************************************
//  CustomFileStream.cs - Gbtc
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
//  09/25/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.InteropServices;
using Gemstone.Diagnostics;
using Gemstone;
using SnapDB.Collections;
using SnapDB.IO.Unmanaged;
using SnapDB.Threading;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// A functional wrapper around a <see cref="FileStream"/>
/// specific to how the <see cref="TransactionalFileStructure"/> uses the <see cref="FileStream"/>.
/// </summary>
internal sealed class CustomFileStream
    : IDisposable
{
    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(CustomFileStream), MessageClass.Component);

    // Needed since this class computes footer check-sums.
    private bool m_disposed;

    private FileStream? m_stream;
    private int m_streamUsers;
    private readonly ResourceQueue<byte[]> m_bufferQueue;

    /// <summary>
    /// Lock this first. Allows the <see cref="m_stream"/> item to be replaced in 
    /// a synchronized fashion. 
    /// </summary>
    private readonly ReaderWriterLockEasy m_isUsingStream = new();

    /// <summary>
    /// Needed to properly synchronize read and write operations.
    /// </summary>
    private readonly object m_syncRoot;
    private readonly AtomicInt64 m_length = new();

    /// <summary>
    /// Creates a new CustomFileStream
    /// </summary>
    /// <param name="ioSize">The size of a buffer pool entry.</param>
    /// <param name="fileStructureBlockSize">The size of an individual block.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="isReadOnly">If the file is read-only.</param>
    /// <param name="isSharingEnabled">If the file is exclusively opened.</param>
    private CustomFileStream(int ioSize, int fileStructureBlockSize, string fileName, bool isReadOnly, bool isSharingEnabled)
    {
        if (ioSize < 4096)
            throw new ArgumentOutOfRangeException(nameof(ioSize), "Cannot be less than 4096");

        if (fileStructureBlockSize > ioSize)
            throw new ArgumentOutOfRangeException(nameof(fileStructureBlockSize), "Must not be greater than BufferPoolSize");

        if (!BitMath.IsPowerOfTwo(ioSize))
            throw new ArgumentException("Must be a power of 2", nameof(ioSize));

        if (!BitMath.IsPowerOfTwo(fileStructureBlockSize))
            throw new ArgumentException("Must be a power of 2", nameof(fileStructureBlockSize));

        IoSize = ioSize;
        FileName = fileName;
        IsReadOnly = isReadOnly;
        IsSharingEnabled = isSharingEnabled;
        FileStructureBlockSize = fileStructureBlockSize;
        m_bufferQueue = s_resourceList.GetResourceQueue(ioSize);
        m_syncRoot = new object();

        FileInfo fileInfo = new(fileName);
        m_length.Value = fileInfo.Length;
    }

#if DEBUG
    ~CustomFileStream()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #region [ Properties ]

    /// <summary>
    /// Gets if the file was opened read-only.
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets if the file was opened allowing shared read access.
    /// </summary>
    public bool IsSharingEnabled { get; private set; }

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the number of bytes in a file structure block.
    /// </summary>
    public int FileStructureBlockSize { get; }

    /// <summary>
    /// Gets the number of bytes in each I/O operation.
    /// </summary>
    public int IoSize { get; }

    /// <summary>
    /// Gets the length of the stream.
    /// </summary>
    public long Length => m_length;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Opens the underlying file stream.
    /// </summary>
    public void Open()
    {
        using (m_isUsingStream.EnterWriteLock())
        {
            if (m_streamUsers == 0)
                m_stream = new FileStream(FileName, FileMode.Open, IsReadOnly ? FileAccess.Read : FileAccess.ReadWrite, IsSharingEnabled ? FileShare.Read : FileShare.None, 2048, true);

            m_streamUsers++;
        }
    }

    /// <summary>
    /// Closes the underlying file stream.
    /// </summary>
    public void Close()
    {
        using (m_isUsingStream.EnterWriteLock())
        {
            m_streamUsers--;

            if (m_streamUsers == 0)
            {
                m_stream?.Dispose();
                m_stream = null;
            }
        }
    }

    /// <summary>
    /// Reads data from the disk.
    /// </summary>
    /// <param name="position">The starting position.</param>
    /// <param name="buffer">The byte buffer of data to read.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    public int ReadRaw(long position, byte[] buffer, int length)
    {
        bool needsOpen = m_stream is null;

        try
        {
            if (needsOpen)
                Open();

            int totalLengthRead = 0;

            while (length > 0)
            {
                int len;

                using (m_isUsingStream.EnterReadLock())
                {
                    Task<int> results;

                    lock (m_syncRoot)
                    {
                        m_stream!.Position = position;
                        results = m_stream.ReadAsync(buffer, 0, length);
                    }

                    len = results.Result;
                }
                
                totalLengthRead += len;

                if (len == length)
                    return totalLengthRead;

                if (len == 0 && position >= m_length)
                    return totalLengthRead; // End of the stream has occurred

                if (len != 0)
                {
                    position += len;
                    length -= len; // Keep Reading
                }
                else
                {
                    s_log.Publish(MessageLevel.Warning, "File Read Error", $"The OS has closed the following file {m_stream.Name}. Attempting to reopen.");
                    ReopenFile();
                }
            }

            return length;
        }
        finally
        {
            if (needsOpen)
                Close();
        }
    }

    /// <summary>
    /// Writes data to the disk.
    /// </summary>
    /// <param name="position">The starting position.</param>
    /// <param name="buffer">The byte buffer of data to write.</param>
    /// <param name="length">The number of bytes to write.</param>
    public void WriteRaw(long position, byte[] buffer, int length)
    {
        bool needsOpen = m_stream is null;

        try
        {
            if (needsOpen)
                Open();

            using (m_isUsingStream.EnterReadLock())
            {
                Task results;

                lock (m_syncRoot)
                {
                    m_stream!.Position = position;
                    results = m_stream.WriteAsync(buffer, 0, length);
                }

                results.Wait();
                m_length.Value = m_stream.Length;
            }
        }
        finally
        {
            if (needsOpen)
                Close();
        }
    }

    /// <summary>
    /// Reads an entire page at the provided location. Also computes the checksum information.
    /// </summary>
    /// <param name="position">The stream position. May be any position inside the desired block.</param>
    /// <param name="locationToCopyData">The place where to write the data to.</param>
    public void Read(long position, nint locationToCopyData)
    {
        byte[] buffer = m_bufferQueue.Dequeue();
        int bytesRead = ReadRaw(position, buffer, buffer.Length);

        if (bytesRead < buffer.Length)
            Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);

        Marshal.Copy(buffer, 0, locationToCopyData, buffer.Length);

        m_bufferQueue.Enqueue(buffer);

        Footer.WriteChecksumResultsToFooter(locationToCopyData, FileStructureBlockSize, buffer.Length);
    }


    /// <summary>
    /// Writes all of the dirty blocks passed onto the disk subsystem. Also computes the checksum for the data.
    /// </summary>
    /// <param name="currentEndOfCommitPosition">The last valid byte of the file system where this data will be appended to.</param>
    /// <param name="stream">The source of the data to dump to the disk.</param>
    /// <param name="length">The number by bytes to write to the file system.</param>
    /// <param name="waitForWriteToDisk">True to wait for a complete commit to disk before returning from this function.</param>
    public void Write(long currentEndOfCommitPosition, MemoryPoolStreamCore stream, long length, bool waitForWriteToDisk)
    {
        bool needsOpen = m_stream is null;

        try
        {
            if (needsOpen)
                Open(); // If the stream needs to be opened, perform the open operation.

            byte[] buffer = m_bufferQueue.Dequeue(); // Dequeue a buffer from the buffer queue.
            long endPosition = currentEndOfCommitPosition + length;
            long currentPosition = currentEndOfCommitPosition;

            // Loop to read and write data in blocks until reaching the end position.
            while (currentPosition < endPosition)
            {
                stream.ReadBlock(currentPosition, out nint ptr, out int streamLength);
                int subLength = (int)Math.Min(streamLength, endPosition - currentPosition);
                Footer.ComputeChecksumAndClearFooter(ptr, FileStructureBlockSize, subLength);
                Marshal.Copy(ptr, buffer, 0, subLength);
                WriteRaw(currentPosition, buffer, subLength);

                currentPosition += subLength;
            }

            m_bufferQueue.Enqueue(buffer);

            if (waitForWriteToDisk)
            {
                FlushFileBuffers();
            }
            else
            {
                using (m_isUsingStream.EnterReadLock())
                {
                    m_stream!.Flush(false);
                }
            }
        }
        finally
        {
            if (needsOpen)
                Close();
        }
    }


    /// <summary>
    /// Flushes any temporary data to the disk.
    /// </summary>
    public void FlushFileBuffers()
    {
        using (m_isUsingStream.EnterReadLock())
        {
            m_stream?.Flush(true);
        }
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">The new extension.</param>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        using (m_isUsingStream.EnterWriteLock())
        {
            string oldFileName = FileName;
            string newFileName = Path.ChangeExtension(oldFileName, extension);

            if (File.Exists(newFileName))
                throw new Exception("New file already exists with this extension");

            bool openStream = m_stream is null;
            m_stream?.Dispose();
            m_stream = null;

            File.Move(oldFileName, newFileName);

            if (openStream)
                m_stream = new FileStream(newFileName, FileMode.Open, isReadOnly ? FileAccess.Read : FileAccess.ReadWrite, isSharingEnabled ? FileShare.Read : FileShare.None, 2048, true);

            FileName = newFileName;
            IsSharingEnabled = isSharingEnabled;
            IsReadOnly = isReadOnly;
        }
    }

    private void ReopenFile()
    {
        using (m_isUsingStream.EnterWriteLock())
        {
            string fileName = m_stream!.Name;

            try
            {
                m_stream.Dispose();
                m_stream = null;
            }
            catch (Exception ex)
            {
                s_log.Publish(MessageLevel.Info, "Error when disposing stream", null, null, ex);
            }

            m_stream = new FileStream(fileName, FileMode.Open, IsReadOnly ? FileAccess.Read : FileAccess.ReadWrite, IsSharingEnabled ? FileShare.Read : FileShare.None, 2048, true);
        }
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as read-only.</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        using (m_isUsingStream.EnterWriteLock())
        {
            if (m_stream is not null)
            {
                m_stream.Dispose();
                m_stream = new FileStream(FileName, FileMode.Open, isReadOnly ? FileAccess.Read : FileAccess.ReadWrite, isSharingEnabled ? FileShare.Read : FileShare.None, 2048, true);
            }

            IsSharingEnabled = isSharingEnabled;
            IsReadOnly = isReadOnly;
        }
    }

    /// <summary>
    /// Releases all the resources used by the <see cref="CustomFileStream"/> object.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (m_disposed)
            return;
        
        try
        {
            using (m_isUsingStream.EnterWriteLock())
            {
                m_stream?.Dispose();
                m_stream = null;
            }
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
        }
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a file with the supplied name.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="ioBlockSize">The number of bytes to do all io with.</param>
    /// <param name="fileStructureBlockSize">The number of bytes in the file structure so check-sums can be properly computed.</param>
    /// <returns>A new <see cref="CustomFileStream"/> instance representing the specified file.</returns>
    public static CustomFileStream CreateFile(string fileName, int ioBlockSize, int fileStructureBlockSize)
    {
        return new CustomFileStream(ioBlockSize, fileStructureBlockSize, fileName, false, false);
    }

    /// <summary>
    /// Opens a file.
    /// </summary>
    /// <param name="fileName">The name of the file to open or create.</param>
    /// <param name="ioBlockSize">The I/O block size to use for the file.</param>
    /// <param name="fileStructureBlockSize">The file structure block size found in the file header.</param>
    /// <param name="isReadOnly">A boolean indicating whether the file is opened in read-only mode.</param>
    /// <param name="isSharingEnabled">A boolean indicating whether file sharing is enabled.</param>
    /// <returns>A new or existing <see cref="CustomFileStream"/> instance representing the specified file.</returns>
    public static CustomFileStream OpenFile(string fileName, int ioBlockSize, out int fileStructureBlockSize, bool isReadOnly, bool isSharingEnabled)
    {
        using (FileStream fileStream = new(fileName, FileMode.Open, isReadOnly ? FileAccess.Read : FileAccess.ReadWrite, isSharingEnabled ? FileShare.Read : FileShare.None, 2048, true))
        {
            fileStructureBlockSize = FileHeaderBlock.SearchForBlockSize(fileStream);
        }

        return new CustomFileStream(ioBlockSize, fileStructureBlockSize, fileName, isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Queues byte[] blocks.
    /// </summary>
    private static readonly ResourceQueueCollection<int, byte[]> s_resourceList;

    /// <summary>
    /// Creates a resource list that everyone shares.
    /// </summary>
    static CustomFileStream()
    {
        s_resourceList = new ResourceQueueCollection<int, byte[]>(blockSize => () => new byte[blockSize], 10, 20);
    }

    #endregion
}
