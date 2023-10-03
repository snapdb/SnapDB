//******************************************************************************************************
//  Murmur3.cs - Gbtc
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
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

// Checksum is based on code found at the following websites
// http://blog.teamleadnet.com/2012/08/murmurhash3-ultra-fast-hash-algorithm.html
// http://en.wikipedia.org/wiki/MurmurHash
// http://code.google.com/p/smhasher/wiki/MurmurHash3

namespace SnapDB.IO.FileStructure;

/// <summary>
/// A specialized implementation of Murmur3 that requires the data be aligned 
/// to 16-byte boundaries.
/// </summary>
internal static unsafe class Murmur3
{
    /// <summary>
    /// Computes the MurmurHash3 checksum for the given byte data.
    /// </summary>
    /// <param name="bb">A pointer to the byte data.</param>
    /// <param name="length">The length of the byte data (must be a multiple of 16).</param>
    /// <param name="checksum1">The first computed checksum.</param>
    /// <param name="checksum2">The second computed checksum.</param>
    public static void ComputeHash(byte* bb, int length, out ulong checksum1, out ulong checksum2)
    {
        // Ensure the length is a multiple of 16, as required by the MurmurHash3 algorithm.
        if ((length & 15) != 0)
            throw new Exception("Checksum only valid for a length multiple of 16");

        // Constants used in the MurmurHash3 algorithm.
        const uint seed = 0;
        const ulong c1 = 0x87c37b91114253d5L;
        const ulong c2 = 0x4cf5ad432745937fL;

        // Cast the byte data to ulong blocks for processing.
        ulong* blocks = (ulong*)bb;
        int nblocks = length >> 4;
        ulong h1 = seed;
        ulong h2 = seed;

        for (int i = 0; i < nblocks; i++)
        {
            ulong k1 = blocks[2 * i + 0];
            ulong k2 = blocks[2 * i + 1];

            // Mix and combine hash values using bitwise operations and constants.
            k1 *= c1;
            k1 = (k1 << 31) | (k1 >> (64 - 31));
            k1 *= c2;
            h1 ^= k1;

            h1 = (h1 << 27) | (h1 >> (64 - 27));
            h1 += h2;
            h1 = h1 * 5 + 0x52dce729;

            k2 *= c2;
            k2 = (k2 << 33) | (k2 >> (64 - 33));
            k2 *= c1;
            h2 ^= k2;


            h2 = (h2 << 31) | (h2 >> (64 - 31));
            h2 += h1;
            h2 = h2 * 5 + 0x38495ab5;
        }

        // Process the tail section of the data.

        h1 ^= (ulong)length;
        h2 ^= (ulong)length;

        h1 += h2;
        h2 += h1;

        h1 ^= h1 >> 33;
        h1 *= 0xff51afd7ed558ccdL;
        h1 ^= h1 >> 33;
        h1 *= 0xc4ceb9fe1a85ec53L;
        h1 ^= h1 >> 33;

        h2 ^= h2 >> 33;
        h2 *= 0xff51afd7ed558ccdL;
        h2 ^= h2 >> 33;
        h2 *= 0xc4ceb9fe1a85ec53L;
        h2 ^= h2 >> 33;

        h1 += h2;
        h2 += h1;

        // Set the computed check-sums.
        checksum1 = h1;
        checksum2 = h2;
    }
}

[Obsolete("For testing only")]
/// <summary>
/// Provides helper methods for performing operations on 64-bit integers (ulong).
/// </summary>
internal static class IntHelpers
{
    /// <summary>
    /// Rotates the bits of a 64-bit integer left by the specified number of bits.
    /// </summary>
    /// <param name="original">The original 64-bit integer.</param>
    /// <param name="bits">The number of bits to rotate left.</param>
    /// <returns>The result of the left rotation operation.</returns>
    public static ulong RotateLeft(this ulong original, int bits)
    {
        return (original << bits) | (original >> (64 - bits));
    }

    /// <summary>
    /// Rotates the bits of a 64-bit integer right by the specified number of bits.
    /// </summary>
    /// <param name="original">The original 64-bit integer.</param>
    /// <param name="bits">The number of bits to rotate right.</param>
    /// <returns>The result of the right rotation operation.</returns>
    public static ulong RotateRight(this ulong original, int bits)
    {
        return (original >> bits) | (original << (64 - bits));
    }

    /// <summary>
    /// Reads an unsigned 64-bit integer (ulong) from a byte array at the specified position.
    /// </summary>
    /// <param name="bb">The byte array containing the data.</param>
    /// <param name="pos">The position in the byte array from which to read.</param>
    /// <returns>The extracted 64-bit integer.</returns>
        public static unsafe ulong GetUInt64(this byte[] bb, int pos)
    {
        // We only read aligned longs, so a simple casting is enough
        fixed (byte* pbyte = &bb[pos])
        {
            return *(ulong*)pbyte;
        }
    }
}

[Obsolete("For testing only")]
internal class Murmur3Orig
{
    // 128 bit output, 64 bit platform version

    public static ulong ReadSize = 16;
    private static readonly ulong s_c1 = 0x87c37b91114253d5L;
    private static readonly ulong s_c2 = 0x4cf5ad432745937fL;

    private ulong m_length;
    private uint m_seed; // If want to start with a seed, create a constructor.
    private ulong m_h1;
    private ulong m_h2;

    private void MixBody(ulong k1, ulong k2)
    {
        m_h1 ^= MixKey1(k1);

        m_h1 = m_h1.RotateLeft(27);
        m_h1 += m_h2;
        m_h1 = m_h1 * 5 + 0x52dce729;

        m_h2 ^= MixKey2(k2);

        m_h2 = m_h2.RotateLeft(31);
        m_h2 += m_h1;
        m_h2 = m_h2 * 5 + 0x38495ab5;
    }

    private static ulong MixKey1(ulong k1)
    {
        k1 *= s_c1;
        k1 = k1.RotateLeft(31);
        k1 *= s_c2;
        return k1;
    }

    private static ulong MixKey2(ulong k2)
    {
        k2 *= s_c2;
        k2 = k2.RotateLeft(33);
        k2 *= s_c1;
        return k2;
    }

    private static ulong MixFinal(ulong k)
    {
        // Avalanche bits.

        k ^= k >> 33;
        k *= 0xff51afd7ed558ccdL;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53L;
        k ^= k >> 33;
        return k;
    }

    public byte[] ComputeHash(byte[] bb)
    {
        ProcessBytes(bb);
        return Hash;
    }

    private void ProcessBytes(byte[] bb)
    {
        m_h1 = m_seed;
        m_h2 = m_seed;
        m_length = 0L;

        int pos = 0;
        ulong remaining = (ulong)bb.Length;

        // Read 128 bits, 16 bytes, 2 longs in eacy cycle.
        while (remaining >= ReadSize)
        {
            ulong k1 = bb.GetUInt64(pos);
            pos += 8;

            ulong k2 = bb.GetUInt64(pos);
            pos += 8;

            m_length += ReadSize;
            remaining -= ReadSize;

            MixBody(k1, k2);
        }

        // If the input MOD 16 != 0
        if (remaining > 0)
            ProcessBytesRemaining(bb, remaining, pos);
    }

    private void ProcessBytesRemaining(byte[] bb, ulong remaining, int pos)
    {
        ulong k1 = 0;
        ulong k2 = 0;
        m_length += remaining;

        // Little endian (x86) processing.
        switch (remaining)
        {
            case 15:
                k2 ^= (ulong)bb[pos + 14] << 48; // Fall through.
                goto case 14;
            case 14:
                k2 ^= (ulong)bb[pos + 13] << 40; // Fall through.
                goto case 13;
            case 13:
                k2 ^= (ulong)bb[pos + 12] << 32; // Fall through.
                goto case 12;
            case 12:
                k2 ^= (ulong)bb[pos + 11] << 24; // Fall through.
                goto case 11;
            case 11:
                k2 ^= (ulong)bb[pos + 10] << 16; // Fall through.
                goto case 10;
            case 10:
                k2 ^= (ulong)bb[pos + 9] << 8; // Fall through.
                goto case 9;
            case 9:
                k2 ^= bb[pos + 8]; // Fall through.
                goto case 8;
            case 8:
                k1 ^= bb.GetUInt64(pos);
                break;
            case 7:
                k1 ^= (ulong)bb[pos + 6] << 48; // Fall through.
                goto case 6;
            case 6:
                k1 ^= (ulong)bb[pos + 5] << 40; // Fall through.
                goto case 5;
            case 5:
                k1 ^= (ulong)bb[pos + 4] << 32; // Fall through.
                goto case 4;
            case 4:
                k1 ^= (ulong)bb[pos + 3] << 24; // Fall through.
                goto case 3;
            case 3:
                k1 ^= (ulong)bb[pos + 2] << 16; // Fall through.
                goto case 2;
            case 2:
                k1 ^= (ulong)bb[pos + 1] << 8; // Fall through.
                goto case 1;
            case 1:
                k1 ^= bb[pos]; // Fall through.
                break;
            default:
                throw new Exception("Something went wrong with remaining bytes calculation.");
        }

        m_h1 ^= MixKey1(k1);
        m_h2 ^= MixKey2(k2);
    }

    /// <summary>
    /// Gets the computed hash value as a byte array.
    /// </summary>
    public byte[] Hash
    {
        get
        {
            // Finalize the hash values by XORing and mixing.
            m_h1 ^= m_length;
            m_h2 ^= m_length;

            m_h1 += m_h2;
            m_h2 += m_h1;

            m_h1 = MixFinal(m_h1);
            m_h2 = MixFinal(m_h2);

            m_h1 += m_h2;
            m_h2 += m_h1;

            // Create a byte array to store the hash result.
            byte[] hash = new byte[ReadSize];

            // Copy the 64-bit hash values into the byte array.
            Array.Copy(BitConverter.GetBytes(m_h1), 0, hash, 0, 8);
            Array.Copy(BitConverter.GetBytes(m_h2), 0, hash, 8, 8);

            return hash;
        }
    }
}