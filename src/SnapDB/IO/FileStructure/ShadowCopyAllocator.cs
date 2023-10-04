//******************************************************************************************************
//  ShadowCopyAllocator.cs - Gbtc
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
//  01/04/2012 - Steven E. Chisholm
//       Generated original version of source code.
//       
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// This class will make shadow copies of blocks or, if the block has never been written to, prepare the block to be written to.
/// </summary>
internal unsafe class ShadowCopyAllocator : IndexParser
{
    #region [ Members ]

    // The FileAllocationTable that can be used to allocate space.
    private readonly FileHeaderBlock m_fileHeaderBlock;

    private readonly SubFileDiskIoSessionPool m_ioSessions;

    // This address is used to determine if the block being referenced is an old block or a new one.
    // Any addresses greater than or equal to this are new blocks for this transaction. Values before this are old.
    private readonly uint m_lastReadOnlyBlock;

    // The file being read.
    private readonly SubFileHeader m_subFileHeader;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="ShadowCopyAllocator"/> that is used make shadow copies of blocks.
    /// </summary>
    /// <param name="ioSessions"></param>
    public ShadowCopyAllocator(SubFileDiskIoSessionPool ioSessions) : base(ioSessions)
    {
        if (ioSessions is null)
            throw new ArgumentNullException(nameof(ioSessions));

        if (ioSessions.IsReadOnly)
            throw new ArgumentException("DataReader is read only", nameof(ioSessions));

        m_lastReadOnlyBlock = ioSessions.LastReadonlyBlock;
        m_fileHeaderBlock = ioSessions.Header;
        m_subFileHeader = ioSessions.File ?? throw new NullReferenceException("Cannot create shadow copy allocator: IO session file is null");
        m_ioSessions = ioSessions;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// This will make a shadow copy of the block that contains the position provided.
    /// If the block does not exist, space is allocated and the indexes are
    /// set up to allow the block to be written to.
    /// </summary>
    /// <param name="positionIndex">The position the application intents to write to.</param>
    /// <param name="wasShadowed"></param>
    /// <remarks>Calling this function automatically updates the underlying parser.</remarks>
    public uint VirtualToShadowPagePhysical(uint positionIndex, out bool wasShadowed)
    {
        SetPositionAndLookup(positionIndex);

        // Make a copy of the data page referenced
        if (TryShadowCopyDataBlock())
        {
            ShadowCopyIndexIndirectBlocks();
            wasShadowed = true;
            return DataClusterAddress;
        }

        wasShadowed = false;
        return DataClusterAddress;
    }

    private void ShadowCopyIndexIndirectBlocks()
    {
        if (FirstIndirectOffset != 0) // Quadruple Indirect
        {
            if (!ShadowCopyIndexIndirect(ref FourthIndirectBlockAddress, FourthIndirectBaseIndex, BlockType.IndexIndirect4, FourthIndirectOffset, DataClusterAddress))
                return;

            if (!ShadowCopyIndexIndirect(ref ThirdIndirectBlockAddress, ThirdIndirectBaseIndex, BlockType.IndexIndirect3, ThirdIndirectOffset, FourthIndirectBlockAddress))
                return;

            if (!ShadowCopyIndexIndirect(ref SecondIndirectBlockAddress, SecondIndirectBaseIndex, BlockType.IndexIndirect2, SecondIndirectOffset, ThirdIndirectBlockAddress))
                return;

            if (ShadowCopyIndexIndirect(ref FirstIndirectBlockAddress, FirstIndirectBaseIndex, BlockType.IndexIndirect1, FirstIndirectOffset, SecondIndirectBlockAddress))
                m_subFileHeader.QuadrupleIndirectBlock = FirstIndirectBlockAddress;
        }
        else if (SecondIndirectOffset != 0) // Triple Indirect
        {
            if (!ShadowCopyIndexIndirect(ref FourthIndirectBlockAddress, FourthIndirectBaseIndex, BlockType.IndexIndirect4, FourthIndirectOffset, DataClusterAddress))
                return;

            if (!ShadowCopyIndexIndirect(ref ThirdIndirectBlockAddress, ThirdIndirectBaseIndex, BlockType.IndexIndirect3, ThirdIndirectOffset, FourthIndirectBlockAddress))
                return;

            if (ShadowCopyIndexIndirect(ref SecondIndirectBlockAddress, SecondIndirectBaseIndex, BlockType.IndexIndirect2, SecondIndirectOffset, ThirdIndirectBlockAddress))
                m_subFileHeader.TripleIndirectBlock = SecondIndirectBlockAddress;
        }
        else if (ThirdIndirectOffset != 0) // Double Indirect
        {
            if (!ShadowCopyIndexIndirect(ref FourthIndirectBlockAddress, FourthIndirectBaseIndex, BlockType.IndexIndirect4, FourthIndirectOffset, DataClusterAddress))
                return;

            if (ShadowCopyIndexIndirect(ref ThirdIndirectBlockAddress, ThirdIndirectBaseIndex, BlockType.IndexIndirect3, ThirdIndirectOffset, FourthIndirectBlockAddress))
                m_subFileHeader.DoubleIndirectBlock = ThirdIndirectBlockAddress;
        }
        else if (FourthIndirectOffset != 0) // Single Indirect
        {
            if (ShadowCopyIndexIndirect(ref FourthIndirectBlockAddress, FourthIndirectBaseIndex, BlockType.IndexIndirect4, FourthIndirectOffset, DataClusterAddress))
                m_subFileHeader.SingleIndirectBlock = FourthIndirectBlockAddress;
        }
        else // Immediate
        {
            m_subFileHeader.DirectBlock = DataClusterAddress;
        }
    }

    /// <summary>
    /// Makes a shadow copy of the indirect index passed to this function. If the block does not exists, it creates it.
    /// </summary>
    /// <param name="sourceBlockAddress">The block to be copied</param>
    /// <param name="indexValue">the index value that goes in the footer of the file.</param>
    /// <param name="blockType">Gets the expected block type</param>
    /// <param name="remoteAddressOffset">the offset of the remote address that needs to be updated.</param>
    /// <param name="remoteBlockAddress">the value of the remote address.</param>
    /// <returns>Returns true if the block had to be shadowed, false if it did not change</returns>
    private bool ShadowCopyIndexIndirect(ref uint sourceBlockAddress, uint indexValue, BlockType blockType, int remoteAddressOffset, uint remoteBlockAddress)
    {
        uint indexIndirectBlock;

        // Make a copy of the index block referenced

        // If the block does not exist, create it.
        if (sourceBlockAddress == 0)
        {
            DiskIoSession buffer = m_ioSessions.SourceIndex;
            indexIndirectBlock = m_fileHeaderBlock.AllocateFreeBlocks(1);
            m_subFileHeader.TotalBlockCount++;

            buffer.WriteToNewBlock(indexIndirectBlock, blockType, indexValue);
            Memory.Clear(buffer.Pointer, buffer.Length);
            WriteIndexIndirectBlock(buffer.Pointer, remoteAddressOffset, remoteBlockAddress);

            sourceBlockAddress = indexIndirectBlock;
            return true;
        }

        // If the data page is an old page, allocate space to create a new copy.
        if (sourceBlockAddress <= m_lastReadOnlyBlock)
        {
            indexIndirectBlock = m_fileHeaderBlock.AllocateFreeBlocks(1);
            m_subFileHeader.TotalBlockCount++;

            ReadThenWriteIndexIndirectBlock(sourceBlockAddress, indexIndirectBlock, indexValue, blockType, remoteAddressOffset, remoteBlockAddress);
            sourceBlockAddress = indexIndirectBlock;
            return true;
        }

        // The page has already been copied. Use the existing address.
        ReadThenWriteIndexIndirectBlock(sourceBlockAddress, sourceBlockAddress, indexValue, blockType, remoteAddressOffset, remoteBlockAddress);
        return false;
    }

    /// <summary>
    /// Makes a shadow copy of an index indirect block and updates a remote address.
    /// </summary>
    /// <param name="sourceBlockAddress">the address of the source.</param>
    /// <param name="destinationBlockAddress">the address of the destination. This can be the same as the source.</param>
    /// <param name="indexValue">the index value that goes in the footer of the file.</param>
    /// <param name="blockType">Gets the expected block type</param>
    /// <param name="remoteAddressOffset">the offset of the remote address that needs to be updated.</param>
    /// <param name="remoteBlockAddress">the value of the remote address.</param>
    private void ReadThenWriteIndexIndirectBlock(uint sourceBlockAddress, uint destinationBlockAddress, uint indexValue, BlockType blockType, int remoteAddressOffset, uint remoteBlockAddress)
    {
        DiskIoSession bufferSource = m_ioSessions.SourceIndex;

        if (sourceBlockAddress == destinationBlockAddress)
        {
            if (*(int*)(bufferSource.Pointer + (remoteAddressOffset << 2)) == remoteBlockAddress)
                return;

            bufferSource.WriteToExistingBlock(destinationBlockAddress, blockType, indexValue);
            WriteIndexIndirectBlock(bufferSource.Pointer, remoteAddressOffset, remoteBlockAddress);
        }
        else
        {
            bufferSource.ReadOld(sourceBlockAddress, blockType, indexValue);

            DiskIoSession destination = m_ioSessions.DestinationIndex;
            destination.WriteToNewBlock(destinationBlockAddress, blockType, indexValue);
            Memory.Copy(bufferSource.Pointer, destination.Pointer, destination.Length);
            WriteIndexIndirectBlock(destination.Pointer, remoteAddressOffset, remoteBlockAddress);

            m_ioSessions.SwapIndex();
        }
    }

    /// <summary>
    /// Writes an Indirect Block to the drive. This sets the indexIndirectNumber and updates one of the addresses within this index.
    /// </summary>
    /// <param name="pointer"> </param>
    /// <param name="remoteAddressOffset">the offset of the remote address that needs to be updated</param>
    /// <param name="remoteBlockAddress">the value of the remote address</param>
    private void WriteIndexIndirectBlock(byte* pointer, int remoteAddressOffset, uint remoteBlockAddress)
    {
        *(uint*)(pointer + (remoteAddressOffset << 2)) = remoteBlockAddress;
    }

    /// <summary>
    /// Makes a copy of the data block. Returns <c>true</c> if a copy was made, otherwise <c>false</c> if no copy was made.
    /// </summary>
    /// <returns>True if the block's address was changed.</returns>
    private bool TryShadowCopyDataBlock()
    {
        // If the page does not exist -or-
        // If the data page is an old page, allocate space to create a new copy.
        if (DataClusterAddress != 0 && DataClusterAddress > m_lastReadOnlyBlock)
            return false; // The page has already been copied, use the existing address.

        uint dataBlockAddress = m_fileHeaderBlock.AllocateFreeBlocks(1);

        if (DataClusterAddress == 0)
            m_subFileHeader.DataBlockCount++;

        m_subFileHeader.TotalBlockCount++;
        ShadowCopyDataCluster(DataClusterAddress, BaseVirtualAddressIndexValue, dataBlockAddress);
        DataClusterAddress = dataBlockAddress;

        return true;
    }

    /// <summary>
    /// Makes a shadow copy of a data cluster.
    /// </summary>
    /// <param name="sourceClusterAddress">
    /// the address of the first block in the cluster.
    /// If address is zero, it simply creates an empty cluster.
    /// </param>
    /// <param name="indexValue">the index value of this first block.</param>
    /// <param name="destinationClusterAddress">the first block of the destination cluster.</param>
    private void ShadowCopyDataCluster(uint sourceClusterAddress, uint indexValue, uint destinationClusterAddress)
    {
        // If source exists
        if (sourceClusterAddress != 0)
        {
            DiskIoSession sourceData = m_ioSessions.SourceData;
            DiskIoSession destinationData = m_ioSessions.DestinationData;
            sourceData.ReadOld(sourceClusterAddress, BlockType.DataBlock, indexValue);
            destinationData.WriteToNewBlock(destinationClusterAddress, BlockType.DataBlock, indexValue);
            Memory.Copy(sourceData.Pointer, destinationData.Pointer, sourceData.Length);
            m_ioSessions.SwapData();
        }
        // If source cluster does not exist.
        else
        {
            m_ioSessions.SourceData.WriteToNewBlock(destinationClusterAddress, BlockType.DataBlock, indexValue);
            Memory.Clear(m_ioSessions.SourceData.Pointer, m_ioSessions.SourceData.Length);
        }
    }

    #endregion
}