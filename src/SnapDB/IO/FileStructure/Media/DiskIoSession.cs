//******************************************************************************************************
//  DiskIoSession.cs - Gbtc
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
//  06/15/2012 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Data;
using Gemstone.Diagnostics;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Provides a data IO session with the disk subsystem to perform basic read and write operations.
/// </summary>
internal unsafe class DiskIoSession : IDisposable
{
    #region [ Members ]

    private readonly BlockArguments m_args;
    private readonly int m_blockSize;

    private DiskIo m_diskIo;

    private BinaryStreamIoSessionBase m_diskMediumIoSession;
    private readonly ushort m_fileIdNumber;

    private readonly bool m_isReadOnly;
    private readonly uint m_lastReadonlyBlock;
    private readonly uint m_snapshotSequenceNumber;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIoSession"/> class for disk I/O operations.
    /// </summary>
    /// <param name="diskIo">The parent <see cref="DiskIo"/> instance associated with this session.</param>
    /// <param name="ioSession">The underlying binary stream I/O session used for reading and writing data.</param>
    /// <param name="header">The file header block associated with this session.</param>
    /// <param name="file">The subfile header associated with this session.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diskIo"/>, <paramref name="ioSession"/>, or <paramref name="file"/> is <c>null</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the <paramref name="diskIo"/> instance is disposed.</exception>
    public DiskIoSession(DiskIo diskIo, BinaryStreamIoSessionBase ioSession, FileHeaderBlock header, SubFileHeader? file)
    {
        if (diskIo is null)
            throw new ArgumentNullException(nameof(diskIo));

        ObjectDisposedException.ThrowIf(diskIo.IsDisposed, diskIo);

        if (file is null)
            throw new ArgumentNullException(nameof(file));

        m_args = new BlockArguments();
        m_lastReadonlyBlock = diskIo.LastReadonlyBlock;
        m_diskMediumIoSession = ioSession ?? throw new ArgumentNullException(nameof(ioSession));
        m_snapshotSequenceNumber = header.SnapshotSequenceNumber;
        m_fileIdNumber = file.FileIdNumber;
        m_isReadOnly = file.IsReadOnly || diskIo.IsReadOnly;
        m_blockSize = diskIo.BlockSize;
        m_diskIo = diskIo;
        IsValid = false;
        IsDisposed = false;
    }

#if DEBUG
    ~DiskIoSession()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the indexed page of this block.
    /// </summary>
    public uint BlockIndex { get; private set; }

    public byte BlockType { get; private set; }

    public uint IndexValue { get; private set; }

    /// <summary>
    /// Returns <c>true</c> if this class is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets if the block in this I/O Session is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the number of bytes valid in this block.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets a pointer to the block.
    /// </summary>
    public byte* Pointer { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="DiskIoSession"/> object.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        try
        {
            if (m_diskMediumIoSession is not null)
            {
                m_diskMediumIoSession.Dispose();
                m_diskMediumIoSession = null!;
            }

            m_diskIo = null!;
        }

        finally
        {
            GC.SuppressFinalize(this);
            IsValid = false;
            IsDisposed = true; // Prevent duplicate dispose.
        }
    }

    /// <summary>
    /// Navigates to a block that will be written to.
    /// This class does not check if overwriting an existing block. So be careful not to corrupt the file.
    /// </summary>
    /// <param name="blockIndex">The index value of this block.</param>
    /// <param name="blockType">The type of this block.</param>
    /// <param name="indexValue">A value put in the footer of the block designating the index of this block.</param>
    /// <remarks>This function will increase the size of the file if the block exceeds the current size of the file.</remarks>
    public void WriteToNewBlock(uint blockIndex, BlockType blockType, uint indexValue)
    {
        BlockIndex = blockIndex;
        BlockType = (byte)blockType;
        IndexValue = indexValue;

        WriteCount++;
        ReadCount++;

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_diskIo.IsDisposed)
            throw new ObjectDisposedException(typeof(DiskIo).FullName);

        if (m_isReadOnly)
            throw new ReadOnlyException("The subfile used for this io session is read only.");

        if (blockIndex > 10 && blockIndex <= m_lastReadonlyBlock)
            throw new ArgumentOutOfRangeException(nameof(blockIndex), "Cannot write to committed blocks");

        IsValid = true;

        BlockIndex = blockIndex;
        ReadBlock(true);
        ClearFooterData();
        WriteFooterData();
    }

    /// <summary>
    /// Writes data to an existing block with the specified block index, block type, and index value.
    /// </summary>
    /// <param name="blockIndex">The index of the block to write data to.</param>
    /// <param name="blockType">The type of the block to write.</param>
    /// <param name="indexValue">The index value associated with the block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this <see cref="DiskIoSession"/> instance or its parent <see cref="DiskIo"/> instance is disposed.</exception>
    /// <exception cref="ReadOnlyException">Thrown if the subfile used for this I/O session is read-only.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockIndex"/> is greater than 10 and less than or equal to the last committed block.</exception>
    /// <exception cref="Exception">Thrown if there is a read error or the read state is not valid.</exception>
    public void WriteToExistingBlock(uint blockIndex, BlockType blockType, uint indexValue)
    {
        BlockIndex = blockIndex;
        BlockType = (byte)blockType;
        IndexValue = indexValue;

        WriteCount++;
        ReadCount++;

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_diskIo.IsDisposed)
            throw new ObjectDisposedException(typeof(DiskIo).FullName);

        if (m_isReadOnly)
            throw new ReadOnlyException("The subfile used for this io session is read only.");

        if (blockIndex > 10 && blockIndex <= m_lastReadonlyBlock)
            throw new ArgumentOutOfRangeException(nameof(blockIndex), "Cannot write to committed blocks");

        IsValid = true;

        ReadBlock(true);

        IoReadState readState = IsFooterCurrentSnapshotAndValid();

        if (readState == IoReadState.Valid)
            return;

        IsValid = false;
        throw new Exception("Read Error: " + readState);
    }

    /// <summary>
    /// Reads data from a block with the specified block index, block type, and index value.
    /// </summary>
    /// <param name="blockIndex">The index of the block to read data from.</param>
    /// <param name="blockType">The type of the block to read.</param>
    /// <param name="indexValue">The index value associated with the block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this <see cref="DiskIoSession"/> instance or its parent <see cref="DiskIo"/> instance is disposed.</exception>
    /// <exception cref="Exception">Thrown if there is a read error or the read state is not valid.</exception>
    public void Read(uint blockIndex, BlockType blockType, uint indexValue)
    {
        BlockIndex = blockIndex;
        BlockType = (byte)blockType;
        IndexValue = indexValue;

        ReadCount++;

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_diskIo.IsDisposed)
            throw new ObjectDisposedException(typeof(DiskIo).FullName);

        IsValid = true;

        ReadBlock(false);

        IoReadState readState = IsFooterValid();

        if (readState == IoReadState.Valid)
            return;

        IsValid = false;
        throw new Exception("Read Error: " + readState);
    }

    /// <summary>
    /// Reads data from an old block with the specified block index, block type, and index value.
    /// </summary>
    /// <param name="blockIndex">The index of the old block to read data from.</param>
    /// <param name="blockType">The type of the old block to read.</param>
    /// <param name="indexValue">The index value associated with the old block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if this <see cref="DiskIoSession"/> instance or its parent <see cref="DiskIo"/> instance is disposed.</exception>
    /// <exception cref="Exception">Thrown if there is a read error or the read state is not valid.</exception>
    public void ReadOld(uint blockIndex, BlockType blockType, uint indexValue)
    {
        BlockIndex = blockIndex;
        BlockType = (byte)blockType;
        IndexValue = indexValue;

        ReadCount++;

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_diskIo.IsDisposed)
            throw new ObjectDisposedException(typeof(DiskIo).FullName);

        IsValid = true;

        ReadBlock(false);

        IoReadState readState = IsFooterValidFromOldBlock();

        if (readState == IoReadState.Valid)
            return;

        IsValid = false;
        throw new Exception("Read Error: " + readState);
    }

    /// <summary>
    /// Clears the data in the current <see cref="DiskIoSession"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if this <see cref="DiskIoSession"/> instance, its parent <see cref="DiskIo"/> instance, or the underlying I/O session instance is disposed.</exception>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (m_diskIo.IsDisposed)
            throw new ObjectDisposedException(typeof(DiskIo).FullName);

        m_args.Length = 0;
        IsValid = false;
        m_diskMediumIoSession.Clear();
    }

    /// <summary>
    /// Tries to read data from the following file.
    /// </summary>
    /// <param name="requestWriteAccess"><c>true</c> if reading data from this block for the purpose of writing to it later.</param>
    private void ReadBlock(bool requestWriteAccess)
    {
        long position = BlockIndex * m_blockSize;

        if (position >= m_args.FirstPosition && position < m_args.FirstPosition + m_args.Length && (m_args.SupportsWriting || !requestWriteAccess))
        {
            Pointer = (byte*)m_args.FirstPointer;
            CachedLookups++;
        }
        else
        {
            m_args.Position = position;
            m_args.IsWriting = requestWriteAccess;
            m_diskMediumIoSession.GetBlock(m_args);
            Pointer = (byte*)m_args.FirstPointer;
            Lookups++;
        }

        int offsetOfPosition = (int)(position - m_args.FirstPosition);

        if (m_args.Length - offsetOfPosition < m_blockSize)
            throw new Exception("stream is not lining up on page boundaries");

        Pointer += offsetOfPosition;
        Length = m_blockSize - FileStructureConstants.BlockFooterLength;
    }

    private IoReadState IsFooterValidFromOldBlock()
    {
        byte* data = Pointer + m_blockSize - 32;
        int checksumState = data[28];

        if (checksumState == Footer.ChecksumIsNotValid)
            return IoReadState.ChecksumInvalid;

        if (checksumState is Footer.ChecksumIsValid or Footer.ChecksumMustBeRecomputed)
        {
            if (data[0] != BlockType)
                return IoReadState.BlockTypeMismatch;

            if (*(uint*)(data + 4) != IndexValue)
                return IoReadState.IndexNumberMismatch;

            if (*(uint*)(data + 8) >= m_snapshotSequenceNumber)
                return IoReadState.PageNewerThanSnapshotSequenceNumber;

            if (*(ushort*)(data + 2) != m_fileIdNumber)
                return IoReadState.FileIdNumberDidNotMatch;

            return IoReadState.Valid;
        }

        throw new Exception("Checksum was not computed properly.");
    }

    private IoReadState IsFooterValid()
    {
        byte* data = Pointer + m_blockSize - 32;
        int checksumState = data[28];

        if (checksumState == Footer.ChecksumIsNotValid)
            return IoReadState.ChecksumInvalid;

        if (checksumState is Footer.ChecksumIsValid or Footer.ChecksumMustBeRecomputed)
        {
            if (data[0] != BlockType)
                return IoReadState.BlockTypeMismatch;

            if (*(uint*)(data + 4) != IndexValue)
                return IoReadState.IndexNumberMismatch;

            if (*(uint*)(data + 8) > m_snapshotSequenceNumber)
                return IoReadState.PageNewerThanSnapshotSequenceNumber;

            if (*(ushort*)(data + 2) != m_fileIdNumber)
                return IoReadState.FileIdNumberDidNotMatch;

            return IoReadState.Valid;
        }

        throw new Exception("Checksum was not computed properly.");
    }

    private IoReadState IsFooterCurrentSnapshotAndValid()
    {
        byte* data = Pointer + m_blockSize - 32;
        int checksumState = data[28];

        if (checksumState == Footer.ChecksumIsNotValid)
            return IoReadState.ChecksumInvalid;

        if (checksumState is Footer.ChecksumIsValid or Footer.ChecksumMustBeRecomputed)
        {
            if (data[0] != BlockType)
                return IoReadState.BlockTypeMismatch;

            if (*(uint*)(data + 4) != IndexValue)
                return IoReadState.IndexNumberMismatch;

            if (*(uint*)(data + 8) != m_snapshotSequenceNumber)
                return IoReadState.PageNewerThanSnapshotSequenceNumber;

            if (*(ushort*)(data + 2) != m_fileIdNumber)
                return IoReadState.FileIdNumberDidNotMatch;

            return IoReadState.Valid;
        }

        throw new Exception("Checksum was not computed properly.");
    }

    private void WriteFooterData()
    {
        byte* data = Pointer + m_blockSize - 32;

        data[28] = Footer.ChecksumMustBeRecomputed;
        data[0] = BlockType;

        *(uint*)(data + 4) = IndexValue;
        *(ushort*)(data + 2) = m_fileIdNumber;
        *(uint*)(data + 8) = m_snapshotSequenceNumber;
    }

    private void ClearFooterData()
    {
        long* ptr = (long*)(Pointer + m_blockSize - 32);

        ptr[0] = 0;
        ptr[1] = 0;
        ptr[2] = 0;
        ptr[3] = 0;
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(DiskIoSession), MessageClass.Component);

    internal static long ReadCount;
    internal static long WriteCount;

    public static long CachedLookups;
    public static long Lookups;

    #endregion
}