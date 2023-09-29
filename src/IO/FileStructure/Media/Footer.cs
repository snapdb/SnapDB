//******************************************************************************************************
//  Footer.cs - Gbtc
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
//  02/09/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;
using SnapDB.Snap;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Since exceptions are very expensive, this enum will be returned for basic
/// I/O operations to let the reader know what to do with the data.  
/// </summary>
/// <remarks>
/// There two overarching conditions.  Valid or not Valid.  
/// If not valid, the reason why the page failed will be given.
/// If a page is returned as valid, this does not mean that the 
/// page being referenced is the correct page, it is up to the class
/// to check the footer of the page to verify that the page being read
/// is the correct page.
/// </remarks>
internal enum IoReadState
{
    /// <summary>
    /// Indicates that the read completed sucessfully.
    /// </summary>
    Valid,

    /// <summary>
    /// The checksum failed to compute.
    /// </summary>
    ChecksumInvalid,

    /// <summary>
    /// The page that was requested came from a newer version of the file.
    /// </summary>
    PageNewerThanSnapshotSequenceNumber,

    /// <summary>
    /// The page came from a different file.
    /// </summary>
    FileIdNumberDidNotMatch,

    /// <summary>
    /// The index value did not match that of the file.
    /// </summary>
    IndexNumberMissmatch,

    /// <summary>
    /// The page type requested did not match what was received.
    /// </summary>
    BlockTypeMismatch
}

internal static unsafe class Footer
{
    public const int ChecksumIsNotComputed = 0;
    public const int ChecksumIsValid = 1;
    public const int ChecksumIsNotValid = 2;
    public const int ChecksumMustBeRecomputed = 3;

    /// <summary>
    /// Computes checksum values for the specified data stored in an unmanaged memory block.
    /// </summary>
    /// <param name="data">A pointer to the start of the data to compute checksum for.</param>
    /// <param name="checksum1">Output: A long checksum value (64 bits).</param>
    /// <param name="checksum2">Output: An integer checksum value (32 bits).</param>
    /// <param name="length">The length of the data to be used for checksum computation, in bytes.</param>
    /// <remarks>
    /// This method computes checksum values for the data stored in an unmanaged memory block
    /// specified by the <paramref name="data"/> pointer. It uses the Murmur3 hashing algorithm
    /// to calculate the checksums. The computed checksums are returned as <paramref name="checksum1"/>
    /// (64 bits) and <paramref name="checksum2"/> (32 bits).
    /// </remarks>
    /// <param name="data">A pointer to the start of the data to compute checksum for.</param>
    /// <param name="checksum1">Output: A long checksum value (64 bits).</param>
    /// <param name="checksum2">Output: An integer checksum value (32 bits).</param>
    /// <param name="length">The length of the data to be used for checksum computation, in bytes.</param>
    public static void ComputeChecksum(IntPtr data, out long checksum1, out int checksum2, int length)
    {
        Stats.ChecksumCount++;
        Murmur3.ComputeHash((byte*)data, length, out ulong a, out ulong b);
        checksum1 = (long)a;
        checksum2 = (int)b ^ (int)(b >> 32);
    }

    /// <summary>
    /// Writes checksum results to the footer of data blocks.
    /// </summary>
    /// <param name="data">A pointer to the start of the data blocks.</param>
    /// <param name="blockSize">The size of each data block, in bytes (must be a power of two).</param>
    /// <param name="length">The total length of the data, including all blocks.</param>
    /// <remarks>
    /// This method is used to write checksum results to the footer of data blocks. It ensures that
    /// the <paramref name="blockSize"/> is a power of two and that it evenly divides the specified
    /// <paramref name="length"/>. Then, it iterates through the data blocks and writes checksum
    /// results to their respective footers.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="blockSize"/> is not a power of two.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="blockSize"/> is greater than the <paramref name="length"/>
    /// or when the <paramref name="length"/> is not a multiple of the <paramref name="blockSize"/>.
    /// </exception>
    /// <param name="data">A pointer to the start of the data blocks.</param>
    /// <param name="blockSize">The size of each data block, in bytes (must be a power of two).</param>
    /// <param name="length">The total length of the data, including all blocks.</param>
    public static void WriteChecksumResultsToFooter(IntPtr data, int blockSize, int length)
    {
        if (!BitMath.IsPowerOfTwo(blockSize))
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Must be a power of two.");

        if (blockSize > length)
            throw new ArgumentException("Must be greater than blockSize", nameof(length));

        if ((length & (blockSize - 1)) != 0)
            throw new ArgumentException("Length is not a multiple of the block size", nameof(length));

        for (int offset = 0; offset < length; offset += blockSize)
            WriteChecksumResultsToFooter(data + offset, blockSize);
    }

    /// <summary>
    /// Writes computed checksum results to the footer of a data block.
    /// </summary>
    /// <param name="data">A pointer to the data block.</param>
    /// <param name="blockSize">The size of the data block, in bytes.</param>
    /// <remarks>
    /// This method computes the checksum for the data block pointed to by <paramref name="data"/> and compares it
    /// to the checksum stored in the data block's footer. If the computed checksum matches the stored checksum,
    /// it marks the checksum as valid in the footer. Otherwise, it marks the checksum as not valid in the footer.
    /// The method also ensures that all other fields in the footer are set to zeroes.
    /// </remarks>
    /// <param name="data">A pointer to the data block.</param>
    /// <param name="blockSize">The size of the data block, in bytes.</param>
    public static void WriteChecksumResultsToFooter(IntPtr data, int blockSize)
    {
        byte* lpData = (byte*)data;
        ComputeChecksum(data, out long checksum1, out int checksum2, blockSize - 16);
        long checksumInData1 = *(long*)(lpData + blockSize - 16);
        int checksumInData2 = *(int*)(lpData + blockSize - 8);

        if (checksum1 == checksumInData1 && checksum2 == checksumInData2)
        {
            // Record checksum is valid and put zeroes in all other fields.
            *(int*)(lpData + blockSize - 4) = ChecksumIsValid;
        }

        else
        {
            // Record checksum is not valid and put zeroes in all other fields.
            *(int*)(lpData + blockSize - 4) = ChecksumIsNotValid;
        }
    }

    /// <summary>
    /// Computes the checksum for data, updates the footer, and clears the checksum status.
    /// </summary>
    /// <param name="data">A pointer to the data block.</param>
    /// <param name="blockSize">The size of the data block, in bytes.</param>
    /// <remarks>
    /// This method computes the checksum for the data block pointed to by <paramref name="data"/>.
    /// It first checks if the checksum needs to be recomputed based on the checksum status in the footer.
    /// If a recomputation is required, it calculates the checksum and updates it in the footer.
    /// Finally, it clears the checksum status in the footer.
    /// </remarks>
    /// <param name="data">A pointer to the data block.</param>
    public static void ComputeChecksumAndClearFooter(IntPtr data, int blockSize)
    {
        byte* lpData = (byte*)data;

        // Determine if the checksum needs to be recomputed.
        if (lpData[blockSize - 4] == ChecksumMustBeRecomputed)
        {
            ComputeChecksum(data, out long checksum1, out int checksum2, blockSize - 16);
            *(long*)(lpData + blockSize - 16) = checksum1;
            *(int*)(lpData + blockSize - 8) = checksum2;
        }

        *(int*)(lpData + blockSize - 4) = ChecksumIsNotComputed;
    }


    /// <summary>
    /// Computes the checksum for a data block and clears the footer.
    /// </summary>
    /// <param name="data">A pointer to the data block.</param>
    /// <param name="blockSize">The size of the data block, in bytes.</param>
    /// <param name="length">The total length of data to process, in bytes.</param>
    /// <remarks>
    /// This method computes the checksum for the data block pointed to by <paramref name="data"/> and clears
    /// the footer of the data block. It is designed to work with blocks of data within a larger data structure.
    /// The method iterates over the specified data blocks, computes checksums for each, and clears their footers.
    /// </remarks>
    /// <param name="data">A pointer to the data block.</param>
    /// <param name="blockSize">The size of the data block, in bytes.</param>
    /// <param name="length">The total length of data to process, in bytes.</param>
    public static void ComputeChecksumAndClearFooter(IntPtr data, int blockSize, int length)
    {
        if (!BitMath.IsPowerOfTwo(blockSize))
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Must be a power of two.");

        if (blockSize > length)
            throw new ArgumentException("Must be greater than blockSize", nameof(length));

        if ((length & (blockSize - 1)) != 0)
            throw new ArgumentException("Length is not a multiple of the block size", nameof(length));

        for (int offset = 0; offset < length; offset += blockSize)
        {
            ComputeChecksumAndClearFooter(data + offset, blockSize);
        }
    }
}