﻿//******************************************************************************************************
//  SimplifiedSubFileStreamIoSession.cs - Gbtc
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
//  10/17/2014 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.InteropServices;
using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// An IoSession for a Simplified Sub File Stream.
/// </summary>
internal unsafe class SimplifiedSubFileStreamIoSession : BinaryStreamIoSessionBase
{
    #region [ Members ]

    private readonly int m_blockDataLength;
    private readonly int m_blockSize;

    private readonly byte[] m_buffer;
    private uint m_currentBlockIndex;
    private long m_currentPhysicalBlock;
    private readonly FileHeaderBlock m_header;
    private readonly Memory m_memory;

    private readonly FileStream m_stream;
    private readonly SubFileHeader m_subFile;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplifiedSubFileStreamIoSession"/> class.
    /// </summary>
    /// <param name="stream">The underlying <see cref="FileStream"/> for I/O operations.</param>
    /// <param name="subFile">The sub-file header associated with this I/O session.</param>
    /// <param name="header">The file header block for the overall file.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/>, <paramref name="subFile"/>, or <paramref name="header"/> is null.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown if the <paramref name="subFile"/> does not have a valid direct block assignment.
    /// </exception>
    public SimplifiedSubFileStreamIoSession(FileStream stream, SubFileHeader? subFile, FileHeaderBlock header)
    {
        // Check for null arguments.
        if (subFile is null)
            throw new ArgumentNullException(nameof(subFile));

        // Check for a valid direct block assignment in the subFile.
        if (subFile.DirectBlock == 0)
            throw new Exception("Must assign subFile.DirectBlock");

        // Initialize the instance fields with provided values.
        m_stream = stream ?? throw new ArgumentNullException(nameof(stream));
        m_header = header ?? throw new ArgumentNullException(nameof(header));
        m_blockSize = header.BlockSize;
        m_subFile = subFile;
        m_memory = new Memory(m_blockSize);
        m_buffer = new byte[m_blockSize];
        m_currentPhysicalBlock = -1;
        m_blockDataLength = m_blockSize - FileStructureConstants.BlockFooterLength;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SimplifiedSubFileStreamIoSession"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        try
        {
            if (!disposing)
                return;

            Flush();
            m_memory.Dispose();
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
            base.Dispose(disposing); // Call base class Dispose().
        }
    }

    /// <summary>
    /// Sets the current usage of the <see cref="BinaryStreamIoSessionBase"/> to null.
    /// </summary>
    public override void Clear()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        Flush();
    }

    public override void GetBlock(BlockArguments args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        long pos = args.Position;

        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (pos < 0)
            throw new ArgumentOutOfRangeException(nameof(args), "position cannot be negative");

        if (pos >= m_blockDataLength * (uint.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(args), "position reaches past the end of the file.");

        uint indexPosition;

        if (pos <= uint.MaxValue) // 64-bit divide is 2 times slower
            indexPosition = (uint)pos / (uint)m_blockDataLength;
        else
            indexPosition = (uint)((ulong)pos / (ulong)m_blockDataLength); // 64-bit signed divide is twice as slow as 64-bit unsigned.

        args.FirstPosition = indexPosition * m_blockDataLength;
        args.Length = m_blockDataLength;

        uint physicalBlockIndex = m_subFile.DirectBlock + indexPosition;

        Read(physicalBlockIndex, indexPosition);

        args.FirstPointer = m_memory.Address;
        args.SupportsWriting = true;
    }

    private void Flush()
    {
        if (m_currentPhysicalBlock < 0)
            return;

        WriteBlockCount++;

        byte* data = (byte*)m_memory.Address + m_blockSize - 32;

        data[0] = (byte)BlockType.DataBlock;
        data[1] = 0;

        *(ushort*)(data + 2) = m_subFile.FileIdNumber;
        *(uint*)(data + 4) = m_currentBlockIndex;
        *(uint*)(data + 8) = m_header.SnapshotSequenceNumber;

        data[32 - 4] = Footer.ChecksumMustBeRecomputed;

        Footer.ComputeChecksumAndClearFooter(m_memory.Address, m_blockSize, m_blockSize);
        Marshal.Copy(m_memory.Address, m_buffer, 0, m_blockSize);

        m_stream.Position = m_currentPhysicalBlock * m_blockSize;
        m_stream.Write(m_buffer, 0, m_blockSize);
        m_currentPhysicalBlock = -1;
    }

    private void Read(uint physicalBlockIndex, uint blockIndex)
    {
        if (m_currentPhysicalBlock == physicalBlockIndex)
            return;

        Flush();

        if (blockIndex >= m_subFile.DataBlockCount)
        {
            uint additionalBlocks = blockIndex - m_subFile.DataBlockCount + 1;
            Memory.Clear(m_memory.Address, m_blockSize);

            m_header.AllocateFreeBlocks(additionalBlocks);
            m_subFile.DataBlockCount += additionalBlocks;
        }
        else
        {
            ReadBlockCount++;
            m_stream.Position = physicalBlockIndex * m_blockSize;
            int bytesRead = m_stream.Read(m_buffer, 0, m_blockSize);

            if (bytesRead < m_buffer.Length)
                Array.Clear(m_buffer, bytesRead, m_buffer.Length - bytesRead);

            Marshal.Copy(m_buffer, 0, m_memory.Address, m_buffer.Length);
        }

        m_currentPhysicalBlock = physicalBlockIndex;
        m_currentBlockIndex = blockIndex;
    }

    #endregion

    #region [ Static ]

    public static int ReadBlockCount;
    public static int WriteBlockCount;

    #endregion
}