//******************************************************************************************************
//  FileHeaderBlock.cs - Gbtc
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
//  12/03/2011 - Steven E. Chisholm
//       Generated original version of source code. That is capable of reading/writing header version 0
//
//  10/11/2014 - Steven E. Chisholm
//       Added header version 2
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone;
using Gemstone.GuidExtensions;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Contains the information that is in the header page of an archive file.
/// </summary>
public class FileHeaderBlock : ImmutableObjectBase<FileHeaderBlock>
{
    #region [ Members ]

    private const short FileAllocationHeaderVersion = 2;

    private const short FileAllocationReadTableVersion = 2;
    private const short FileAllocationWriteTableVersion = 2;

    // The GUID for this archive file system.
    private Guid m_archiveId;

    // The GUID to represent the type of this archive file.
    private Guid m_archiveType;

    // The version of the header.
    private short m_headerVersion;

    // The size of the block.

    // The version number required to read the file system.
    private short m_minimumReadVersion;

    // The version number required to write to the file system.
    private short m_minimumWriteVersion;

    // Since files are allocated sequentially, this value is the next file id that is not used.
    private ushort m_nextFileId;

    private Dictionary<short, byte[]>? m_unknownAttributes;

    private Dictionary<Guid, byte[]>? m_userAttributes;

    #endregion

    #region [ Constructors ]

    private FileHeaderBlock()
    {
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The GUID number for this archive.
    /// </summary>
    public Guid ArchiveId => m_archiveId;

    /// <summary>
    /// The GUID number for this archive.
    /// </summary>
    public Guid ArchiveType
    {
        get => m_archiveType;
        set
        {
            TestForEditable();
            m_archiveType = value;
        }
    }

    /// <summary>
    /// The number of bytes per block for the file structure.
    /// </summary>
    public int BlockSize { get; private set; }

    /// <summary>
    /// Determines if the archive file can be read
    /// </summary>
    public bool CanRead => m_minimumReadVersion <= FileAllocationWriteTableVersion;

    /// <summary>
    /// Determines if the file can be written to because enough features are recognized by this current version to do it without corrupting the file system.
    /// </summary>
    // TODO: Support changing files that are from an older version.
    public bool CanWrite => m_minimumWriteVersion == FileAllocationWriteTableVersion;

    /// <summary>
    /// Gets the size of each data block (block size - overhead)
    /// </summary>
    public int DataBlockSize => BlockSize - FileStructureConstants.BlockFooterLength;

    /// <summary>
    /// Returns the number of files that are in this file system.
    /// </summary>
    /// <returns>Number of files that are in this file system. </returns>
    public int FileCount => Files.Count;

    /// <summary>
    /// A list of all of the files in this collection.
    /// </summary>
    public ImmutableList<SubFileHeader?> Files { get; private set; } = default!;

    /// <summary>
    /// User definable flags to associate with archive files.
    /// </summary>
    public ImmutableList<Guid> Flags { get; private set; } = default!;

    /// <summary>
    /// Gets the number of times the file header exists in the archive file.
    /// </summary>
    public byte HeaderBlockCount { get; private set; }

    /// <summary>
    /// Gets if this file uses the simplified file format.
    /// </summary>
    public bool IsSimplifiedFileFormat { get; private set; }

    /// <summary>
    /// Represents the last block that has been allocated.
    /// </summary>
    public uint LastAllocatedBlock { get; private set; }

    /// <summary>
    /// Maintains a sequential number that represents the version of the file.
    /// </summary>
    /// <remarks>
    /// This will be updated every time the file system has been modified. Initially, it will be one.
    /// </remarks>
    public uint SnapshotSequenceNumber { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Clones the object, while incrementing the sequence number.
    /// </summary>
    /// <returns>The clone of the object.</returns>
    public override FileHeaderBlock CloneEditable()
    {
        FileHeaderBlock clone = base.CloneEditable();
        clone.SnapshotSequenceNumber++;
        return clone;
    }

    /// <summary>
    /// Allocates a sequential number of blocks at the end of the file and returns the starting address of the allocation
    /// </summary>
    /// <param name="count">the number of blocks to allocate</param>
    /// <returns>the address of the first block of the allocation </returns>
    public uint AllocateFreeBlocks(uint count)
    {
        TestForEditable();

        uint blockAddress = LastAllocatedBlock + 1;
        LastAllocatedBlock += count;

        return blockAddress;
    }

    /// <summary>
    /// Creates a new file on the file system and returns the <see cref="SubFileHeader"/> associated with the new file.
    /// </summary>
    /// <param name="fileName">Represents the nature of the data that will be stored in this file.</param>
    /// <returns>The newly created <see cref="SubFileHeader"/> instance.</returns>
    /// <remarks>A file system only supports 64 files. This is a fundamental limitation and cannot be changed easily.</remarks>
    public SubFileHeader CreateNewFile(SubFileName fileName)
    {
        TestForEditable();

        if (!CanWrite)
            throw new InvalidOperationException("Writing to this file type is not supported");

        if (IsReadOnly)
            throw new InvalidOperationException("File is read only");

        if (Files.Count >= 64)
            throw new OverflowException("Only 64 files per file system is supported");

        if (ContainsSubFile(fileName))
            throw new DuplicateNameException("Name already exists");

        SubFileHeader node = new(m_nextFileId, fileName, false, IsSimplifiedFileFormat);
        m_nextFileId++;
        Files.Add(node);

        return node;
    }

    /// <summary>
    /// Determines if the file contains the subfile
    /// </summary>
    /// <param name="fileName">the subfile to look for</param>
    /// <returns>true if contained, false otherwise</returns>
    public bool ContainsSubFile(SubFileName fileName)
    {
        return Files.Any(file => file?.FileName == fileName);
    }

    /// <summary>
    /// This will return a byte array of data that can be written to an archive file.
    /// </summary>
    public byte[] GetBytes()
    {
        if (!IsFileAllocationTableValid())
            throw new InvalidOperationException("File Allocation Table is invalid");

        byte[] dataBytes = new byte[BlockSize];
        MemoryStream stream = new(dataBytes);
        BinaryWriter dataWriter = new(stream);

        dataWriter.Write(s_fileAllocationTableHeaderBytes);
        dataWriter.Write(BitConverter.IsLittleEndian ? 'L' : 'B');
        dataWriter.Write((byte)BitMath.CountBitsSet((uint)(BlockSize - 1)));
        dataWriter.Write(FileAllocationReadTableVersion);
        dataWriter.Write(FileAllocationWriteTableVersion);
        dataWriter.Write(FileAllocationHeaderVersion);
        dataWriter.Write((byte)(IsSimplifiedFileFormat ? 2 : 1));
        dataWriter.Write(HeaderBlockCount);
        dataWriter.Write(LastAllocatedBlock);
        dataWriter.Write(SnapshotSequenceNumber);
        dataWriter.Write(m_nextFileId);
        dataWriter.Write(m_archiveId.ToByteArray());
        dataWriter.Write(m_archiveType.ToByteArray());
        dataWriter.Write((short)Files.Count);

        foreach (SubFileHeader? node in Files)
            node?.Save(dataWriter);

        // Metadata Flags
        if (Flags.Count > 0)
        {
            Encoding7Bit.WriteInt15(dataWriter.Write, (short)FileHeaderAttributes.FileFlags);
            Encoding7Bit.WriteInt15(dataWriter.Write, (short)(Flags.Count * 16));

            foreach (Guid flag in Flags)
                dataWriter.Write(flag.ToLittleEndianBytes());
        }

        if (m_unknownAttributes is not null)
            foreach (KeyValuePair<short, byte[]> md in m_unknownAttributes)
            {
                Encoding7Bit.WriteInt15(dataWriter.Write, md.Key);
                Encoding7Bit.WriteInt15(dataWriter.Write, (short)md.Value.Length);
                dataWriter.Write(md.Value);
            }

        if (m_userAttributes is not null)
            foreach (KeyValuePair<Guid, byte[]> md in m_userAttributes)
            {
                Encoding7Bit.WriteInt15(dataWriter.Write, (short)FileHeaderAttributes.UserAttributes);
                dataWriter.Write(md.Key.ToLittleEndianBytes());
                Encoding7Bit.WriteInt15(dataWriter.Write, (short)md.Value.Length);
                dataWriter.Write(md.Value);
            }

        Encoding7Bit.WriteInt15(dataWriter.Write, (short)FileHeaderAttributes.EndOfAttributes);
        Encoding7Bit.WriteInt15(dataWriter.Write, 0);

        if (stream.Position + 32 > dataBytes.Length)
            throw new Exception("the file size exceeds the allowable size.");

        WriteFooterData(dataBytes);

        return dataBytes;
    }

    /// <summary>
    /// Requests that member fields be set to readonly.
    /// </summary>
    protected override void SetMembersAsReadOnly()
    {
        Files.IsReadOnly = true;
        Flags.IsReadOnly = true;
    }

    /// <summary>
    /// Request that member fields be cloned and marked as editable.
    /// </summary>
    protected override void CloneMembersAsEditable()
    {
        if (!CanWrite)
            throw new InvalidOperationException("This file cannot be modified because the file system version is not recognized");

        Flags = Flags.CloneEditable();
        Files = Files.CloneEditable();

        if (m_userAttributes is not null)
            m_userAttributes = new Dictionary<Guid, byte[]>(m_userAttributes);

        if (m_unknownAttributes is not null)
            m_unknownAttributes = new Dictionary<short, byte[]>(m_unknownAttributes);
    }

    /// <summary>
    /// Checks all of the information in the header file
    /// to verify if it is valid.
    /// </summary>
    private bool IsFileAllocationTableValid()
    {
        if (ArchiveId == Guid.Empty)
            return false;

        if (m_headerVersion < 0)
            return false;

        if (m_minimumReadVersion < 0)
            return false;

        if (m_minimumWriteVersion < 0)
            return false;

        return true;
    }

    /// <summary>
    /// This procedure will attempt to read all of the data out of the file allocation table
    /// If the file allocation table is corrupt, an error will be generated.
    /// </summary>
    /// <param name="buffer">the block that contains the buffer data.</param>
    private void LoadFromBuffer(byte[] buffer)
    {
        ValidateBlock(buffer);

        BlockSize = buffer.Length;
        MemoryStream stream = new(buffer);
        BinaryReader dataReader = new(stream);

        if (!dataReader.ReadBytes(26).SequenceEqual(s_fileAllocationTableHeaderBytes))
            throw new Exception("This file is not an archive file system, or the file is corrupt, or this file system major version is not recognized by this version of the historian");

        char endian = (char)dataReader.ReadByte();

        if (BitConverter.IsLittleEndian)
        {
            if (endian != 'L')
                throw new Exception("This archive file was not written with a little endian processor");
        }
        else
        {
            if (endian != 'B')
                throw new Exception("This archive file was not written with a big endian processor");
        }

        byte blockSizePower = dataReader.ReadByte();

        if (blockSizePower is > 30 or < 5)
            throw new Exception("Block size of this file is not supported");

        int blockSize = 1 << blockSizePower;

        if (BlockSize != blockSize)
            throw new Exception("Block size is unexpected");

        m_minimumReadVersion = dataReader.ReadInt16();
        m_minimumWriteVersion = dataReader.ReadInt16();

        if (!CanRead)
            throw new Exception("The version of this file system is not recognized");

        m_headerVersion = m_minimumWriteVersion;

        if (m_headerVersion < 0)
            throw new Exception("Header version not supported");

        if (m_headerVersion is 0 or 1)
        {
            IsSimplifiedFileFormat = false;
            HeaderBlockCount = 10;
            LoadHeaderV0V1(dataReader);
            return;
        }

        m_headerVersion = dataReader.ReadInt16();
        byte fileMode = dataReader.ReadByte();

        IsSimplifiedFileFormat = fileMode switch
        {
            1 => false,
            2 => true,
            _ => throw new Exception("Unknown File Mode")
        };

        HeaderBlockCount = dataReader.ReadByte();
        LastAllocatedBlock = dataReader.ReadUInt32();
        SnapshotSequenceNumber = dataReader.ReadUInt32();
        m_nextFileId = dataReader.ReadUInt16();
        m_archiveId = new Guid(dataReader.ReadBytes(16));
        m_archiveType = new Guid(dataReader.ReadBytes(16));

        int fileCount = dataReader.ReadInt16();

        // TODO: check based on block length
        if (fileCount > 64)
            throw new Exception("Only 64 features are supported per archive");

        Files = new ImmutableList<SubFileHeader?>(fileCount);

        for (int x = 0; x < fileCount; x++)
            Files.Add(new SubFileHeader(dataReader, true, IsSimplifiedFileFormat));

        Flags = new ImmutableList<Guid>();

        FileHeaderAttributes tag = (FileHeaderAttributes)Encoding7Bit.ReadInt15(dataReader.ReadByte);

        while (tag != FileHeaderAttributes.EndOfAttributes)
        {
            short dataLen;

            switch (tag)
            {
                case FileHeaderAttributes.FileFlags:
                    dataLen = Encoding7Bit.ReadInt15(dataReader.ReadByte);
                    while (dataLen > 0)
                    {
                        dataLen -= 16;
                        Flags.Add(dataReader.ReadBytes(16).ToLittleEndianGuid());
                    }

                    break;
                case FileHeaderAttributes.UserAttributes:
                    Guid flag = dataReader.ReadBytes(16).ToLittleEndianGuid();
                    dataLen = Encoding7Bit.ReadInt15(dataReader.ReadByte);
                    AddUserAttribute(flag, dataReader.ReadBytes(dataLen));
                    break;
                default:
                    dataLen = Encoding7Bit.ReadInt15(dataReader.ReadByte);
                    AddUnknownAttribute((byte)tag, dataReader.ReadBytes(dataLen));
                    break;
            }

            tag = (FileHeaderAttributes)dataReader.ReadByte();
        }

        if (!IsFileAllocationTableValid())
            throw new Exception("File System is invalid");

        IsReadOnly = true;
    }

    private void AddUserAttribute(Guid id, byte[] data)
    {
        m_userAttributes ??= new Dictionary<Guid, byte[]>();
        m_userAttributes[id] = data;
    }

    private void AddUnknownAttribute(byte id, byte[] data)
    {
        m_unknownAttributes ??= new Dictionary<short, byte[]>();
        m_unknownAttributes[id] = data;
    }

    private void LoadHeaderV0V1(BinaryReader dataReader)
    {
        m_archiveId = new Guid(dataReader.ReadBytes(16));
        m_archiveType = new Guid(dataReader.ReadBytes(16));

        SnapshotSequenceNumber = dataReader.ReadUInt32();
        LastAllocatedBlock = dataReader.ReadUInt32();
        m_nextFileId = dataReader.ReadUInt16();
        int fileCount = dataReader.ReadInt32();

        // TODO: check based on block length
        if (fileCount > 64)
            throw new Exception("Only 64 features are supported per archive");

        Files = new ImmutableList<SubFileHeader?>(fileCount);

        for (int x = 0; x < fileCount; x++)
            Files.Add(new SubFileHeader(dataReader, true, false));

        // TODO: check based on block length
        int userSpaceLength = dataReader.ReadInt32();
        dataReader.ReadBytes(userSpaceLength);

        if (m_minimumWriteVersion == 1)
        {
            _ = new DateTime(dataReader.ReadInt64());
            _ = new DateTime(dataReader.ReadInt64());
            int flagCount = dataReader.ReadInt32();
            Flags = new ImmutableList<Guid>(flagCount);

            while (flagCount > 0)
            {
                flagCount--;
                Flags.Add(new Guid(dataReader.ReadBytes(16)));
            }
        }
        else
        {
            Flags = new ImmutableList<Guid>();
        }

        if (!IsFileAllocationTableValid())
            throw new Exception("File System is invalid");

        IsReadOnly = true;
    }

    #endregion

    #region [ Static ]

    // The file header bytes which equals: "openHistorian 2.0 Archive\00"
    private static readonly byte[] s_fileAllocationTableHeaderBytes = "openHistorian 2.0 Archive\0"u8.ToArray();

    /// <summary>
    /// Looks in the contents of a file for the block size of the file.
    /// </summary>
    /// <param name="stream">the stream to look</param>
    /// <returns>the number of bytes in a block. Always a power of 2.</returns>
    public static int SearchForBlockSize(Stream stream)
    {
        long oldPosition = stream.Position;
        stream.Position = 0;

        if (!stream.ReadBytes(26).SequenceEqual(s_fileAllocationTableHeaderBytes))
            throw new Exception("This file is not an archive file system, or the file is corrupt, or this file system major version is not recognized by this version of the historian");

        char endian = (char)stream.ReadNextByte();

        if (BitConverter.IsLittleEndian)
        {
            if (endian != 'L')
                throw new Exception("This archive file was not written with a little endian processor");
        }
        else
        {
            if (endian != 'B')
                throw new Exception("This archive file was not written with a big endian processor");
        }

        byte blockSizePower = stream.ReadNextByte();

        if (blockSizePower is > 30 or < 5) // Stored as 2^n power. 
            throw new Exception("Block size of this file is not supported");

        int blockSize = 1 << blockSizePower;
        stream.Position = oldPosition;

        return blockSize;
    }

    /// <summary>
    /// Creates a new file header.
    /// </summary>
    /// <param name="blockSize">The block size to make the header.</param>
    /// <param name="flags">Flags to write to the file.</param>
    /// <returns>The newly created <see cref="SubFileHeader"/> instance.</returns>
    public static FileHeaderBlock CreateNew(int blockSize, params Guid[] flags)
    {
        FileHeaderBlock header = new()
        {
            BlockSize = blockSize,
            m_minimumReadVersion = FileAllocationReadTableVersion,
            m_minimumWriteVersion = FileAllocationWriteTableVersion,
            m_headerVersion = FileAllocationHeaderVersion,
            HeaderBlockCount = 10,
            IsSimplifiedFileFormat = false,
            m_archiveId = Guid.NewGuid(),
            SnapshotSequenceNumber = 1,
            m_nextFileId = 0,
            LastAllocatedBlock = 9,
            Files = new ImmutableList<SubFileHeader?>(),
            Flags = new ImmutableList<Guid>(),
            m_archiveType = Guid.Empty
        };

        foreach (Guid f in flags)
            header.Flags.Add(f);

        header.IsReadOnly = true;

        return header;
    }

    /// <summary>
    /// Creates a new simplified <see cref="FileHeaderBlock"/> with the specified <paramref name="blockSize"/> and optional <paramref name="flags"/>.
    /// </summary>
    /// <param name="blockSize">The block size of the header.</param>
    /// <param name="flags">Optional flags to set in the header.</param>
    /// <returns>The newly created simplified <see cref="FileHeaderBlock"/> instance.</returns>
    public static FileHeaderBlock CreateNewSimplified(int blockSize, params Guid[] flags)
    {
        FileHeaderBlock header = new()
        {
            BlockSize = blockSize,
            m_minimumReadVersion = FileAllocationReadTableVersion,
            m_minimumWriteVersion = FileAllocationWriteTableVersion,
            m_headerVersion = FileAllocationHeaderVersion,
            HeaderBlockCount = 1,
            IsSimplifiedFileFormat = true,
            m_archiveId = Guid.NewGuid(),
            SnapshotSequenceNumber = 1,
            m_nextFileId = 0,
            LastAllocatedBlock = 0,
            Files = new ImmutableList<SubFileHeader?>(),
            Flags = new ImmutableList<Guid>(),
            m_archiveType = Guid.Empty
        };

        foreach (Guid f in flags)
            header.Flags.Add(f);

        header.IsReadOnly = true;

        return header;
    }

    /// <summary>
    /// Opens an existing <see cref="FileHeaderBlock"/> from the provided binary <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The binary data representing the file header.</param>
    /// <returns>The opened <see cref="FileHeaderBlock"/> instance.</returns>
    public static FileHeaderBlock Open(byte[] data)
    {
        FileHeaderBlock header = new();

        header.LoadFromBuffer(data);
        header.IsReadOnly = true;

        return header;
    }

    private static unsafe void ValidateBlock(byte[] buffer)
    {
        fixed (byte* data = buffer)
        {
            Footer.ComputeChecksum((nint)data, out long checksum1, out int checksum2, buffer.Length - 16);

            long checksumInData1 = *(long*)(data + buffer.Length - 16);
            int checksumInData2 = *(int*)(data + buffer.Length - 8);

            if (checksum1 != checksumInData1 || checksum2 != checksumInData2)
                throw new Exception("Checksum on header file is invalid");

            if (data[buffer.Length - 32] != (byte)BlockType.FileAllocationTable)
                throw new Exception("IoReadState.BlockTypeMismatch");

            if (*(int*)(data + buffer.Length - 28) != 0)
                throw new Exception("IoReadState.IndexNumberMismatch");

            if (*(int*)(data + buffer.Length - 24) != 0)
                throw new Exception("IoReadState.FileIdNumberDidNotMatch");
        }
    }

    private static unsafe void WriteFooterData(byte[] buffer)
    {
        fixed (byte* data = buffer)
        {
            data[buffer.Length - 32] = (byte)BlockType.FileAllocationTable;
            *(int*)(data + buffer.Length - 28) = 0;
            *(int*)(data + buffer.Length - 24) = 0;
            *(int*)(data + buffer.Length - 20) = 0;

            Footer.ComputeChecksum((nint)data, out long checksum1, out int checksum2, buffer.Length - 16);
            *(long*)(data + buffer.Length - 16) = checksum1;
            *(int*)(data + buffer.Length - 8) = checksum2;
            *(int*)(data + buffer.Length - 4) = 0;
        }
    }

    #endregion
}