﻿//******************************************************************************************************
//  BinaryStreamPointerBase.cs - Gbtc
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
//  08/17/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;
using SnapDB.IO.Unmanaged;
using System.Runtime.InteropServices;

namespace SnapDB.IO;

/// <summary>
/// An implementation of <see cref="BinaryStreamBase"/> that is pointer based.
/// </summary>
public unsafe abstract class BinaryStreamPointerBase
    : BinaryStreamBase
{
    /// <summary>
    /// Creates a <see cref="BinaryStreamPointerBase"/>.
    /// </summary>
    protected BinaryStreamPointerBase()
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("Only designed to run on a little endian processor.");
    }

    /// <summary>
    /// Gets the pointer version number, assuming that this binary stream has an unmanaged buffer backing this stream. 
    /// If the pointer version is the same, then any pointer acquired is still valid.
    /// </summary>
    public long PointerVersion { get; protected set; }

    /// <summary>
    /// The position that corresponds to the first byte in the buffer.
    /// </summary>
    protected long FirstPosition;

    /// <summary>
    /// Contains the position for the last position.
    /// </summary>
    protected long LastPosition;

    /// <summary>
    /// The current position data.
    /// </summary>
    protected byte* Current;

    /// <summary>
    /// The first position of the block.
    /// </summary>
    protected byte* First;

    /// <summary>
    /// One past the last address for reading.
    /// </summary>
    protected byte* LastRead;

    /// <summary>
    /// One past the last address for writing.
    /// </summary>
    protected byte* LastWrite;


    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override bool CanRead => true;

    public override bool CanSeek => true;

    /// <summary>
    /// Gets or sets the current position for the stream.
    /// </summary>
    /// <remarks>
    /// It is important to use this to Get or Set the position from the underlying stream since 
    /// this class buffers the results of the query. Setting this field does not guarantee that
    /// the underlying stream will get set. Call FlushToUnderlyingStream to accomplish this.
    /// </remarks>
    public override long Position
    {
        get => FirstPosition + (Current - First);
        set
        {
            if (FirstPosition <= value && value < LastPosition)
            {
                Current = First + (value - FirstPosition);
            }
            else
            {
                FirstPosition = value;
                LastPosition = value;
                Current = null;
                First = null;
                LastRead = null;
                LastWrite = null;
            }
        }
    }

    /// <summary>
    /// Returns the number of bytes available at the end of the stream.
    /// </summary>
    protected long RemainingReadLength => LastRead - Current;

    /// <summary>
    /// Returns the number of bytes available at the end of the stream for writing purposes.
    /// </summary>
    protected long RemainingWriteLength => LastWrite - Current;

    /// <summary>
    /// Gets a pointer from the current position that can be used for writing up the the provided length.
    /// The current position is not advanced after calling this function.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="length">The number of bytes valid for the writing.</param>
    /// <remarks>This method will throw an exeption if the provided length cannot be provided.</remarks>
    public byte* GetWritePointer(long position, int length)
    {
        Position = position;
        if (RemainingWriteLength <= 0)
            UpdateLocalBuffer(true);

        if (RemainingWriteLength < length)
            throw new Exception("Cannot get the provided length.");

        return Current;
    }

    /// <summary>
    /// Gets a pointer from the current position that can be used for writing up the the provided length.
    /// The current position is not advanced after calling this function.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="length">The number of bytes valid for the writing.</param>
    /// <remarks>This method will throw an exception if the provided length cannot be provided.</remarks>
    public byte* GetReadPointer(long position, int length)
    {
        Position = position;

        if (RemainingReadLength <= 0)
            UpdateLocalBuffer(false);
        if (RemainingReadLength < length)
            throw new Exception("Cannot get the provided length.");

        return Current;
    }

    /// <summary>
    /// Gets a pointer from the current position that can be used for writing up the the provided length.
    /// The current position is not advanced after calling this function.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="length">The number of bytes valid for the writing.</param>
    /// <param name="supportsWriting">An output parameter detailing if writing to this block is supported.</param>
    /// <remarks>This method will throw an exception if the provided length cannot be provided.</remarks>
    public byte* GetReadPointer(long position, int length, out bool supportsWriting)
    {
        Position = position;
        if (RemainingReadLength <= 0)
            UpdateLocalBuffer(false);
        if (RemainingReadLength < length)
            throw new Exception("Cannot get the provided length.");

        supportsWriting = RemainingWriteLength >= length;
        return Current;
    }

    /// <summary>
    /// Copies a specified number of bytes to a new location.
    /// </summary>
    /// <param name="source">The source position to start copying from.</param>
    /// <param name="destination">The destination position to start copying to.</param>
    /// <param name="length">The number of bytes to copy.</param>
    public override void Copy(long source, long destination, int length)
    {
        if (source < 0)
            throw new ArgumentException("value cannot be less than zero", nameof(source));
        if (destination < 0)
            throw new ArgumentException("value cannot be less than zero", nameof(destination));
        if (length < 0)
            throw new ArgumentException("value cannot be less than zero", nameof(length));

        if (length == 0 || source == destination)
            return;

        Position = source;
        UpdateLocalBuffer(false);

        bool containsSource = length <= LastRead - Current; // RemainingReadLength = (m_lastRead - m_current)
        bool containsDestination = FirstPosition <= destination && destination + length < LastPosition;

        if (containsSource && containsDestination)
        {
            UpdateLocalBuffer(true);

            byte* src = Current;
            byte* dst = Current + (destination - source);

            Memory.Copy(src, dst, length);
            return;
        }

        // Manually perform the copy.
        byte[] data = new byte[length];
        Position = source;
        ReadAll(data, 0, data.Length);
        Position = destination;
        Write(data, 0, data.Length);
    }

    public override void Write(byte value)
    {
        const int size = sizeof(byte);
        byte* cur = Current;
        if (cur < LastWrite)
        {
            *cur = value;
            Current = cur + size;

            return;
        }
        base.Write(value);
    }

    public override void Write(short value)
    {
        const int size = sizeof(short);
        byte* cur = Current;
        if (cur + size <= LastWrite)
        {
            *(short*)cur = value;
            Current = cur + size;

            return;
        }
        base.Write(value);
    }

    public override void Write(int value)
    {
        const int size = sizeof(int);
        byte* cur = Current;
        if (cur + size <= LastWrite)
        {
            *(int*)cur = value;
            Current = cur + size;

            return;
        }
        base.Write(value);
    }

    public override void Write(long value)
    {
        const int size = sizeof(long);
        byte* cur = Current;
        if (cur + size <= LastWrite)
        {
            *(long*)cur = value;
            Current = cur + size;

            return;
        }
        base.Write(value);
    }

    public override void Write7Bit(uint value)
    {
        const int size = 5;
        if (Current + size <= LastWrite)
        {
            Current += Encoding7Bit.Write(Current, value);
            return;
        }
        base.Write7Bit(value);
    }

    public override void Write7Bit(ulong value)
    {
        const int size = 9;
        byte* stream = Current;

        if (stream + size <= LastWrite)
        {
            if (value < 128)
            {
                stream[0] = (byte)value;
                Current += 1;

                return;
            }
            stream[0] = (byte)(value | 128);

            if (value < 128 * 128)
            {
                stream[1] = (byte)(value >> 7);
                Current += 2;

                return;
            }
            stream[1] = (byte)((value >> 7) | 128);

            if (value < 128 * 128 * 128)
            {
                stream[2] = (byte)(value >> 14);
                Current += 3;

                return;
            }
            stream[2] = (byte)((value >> 14) | 128);

            if (value < 128 * 128 * 128 * 128)
            {
                stream[3] = (byte)(value >> 21);
                Current += 4;

                return;
            }
            stream[3] = (byte)((value >> (7 + 7 + 7)) | 128);

            if (value < 128L * 128 * 128 * 128 * 128)
            {
                stream[4] = (byte)(value >> (7 + 7 + 7 + 7));
                Current += 5;

                return;
            }
            stream[4] = (byte)((value >> (7 + 7 + 7 + 7)) | 128);

            if (value < 128L * 128 * 128 * 128 * 128 * 128)
            {
                stream[5] = (byte)(value >> (7 + 7 + 7 + 7 + 7));
                Current += 6;

                return;
            }
            stream[5] = (byte)((value >> (7 + 7 + 7 + 7 + 7)) | 128);

            if (value < 128L * 128 * 128 * 128 * 128 * 128 * 128)
            {
                stream[6] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7));
                Current += 7;

                return;
            }
            stream[6] = (byte)((value >> (7 + 7 + 7 + 7 + 7 + 7)) | 128);

            if (value < 128L * 128 * 128 * 128 * 128 * 128 * 128 * 128)
            {
                stream[7] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7 + 7));
                Current += 8;

                return;
            }

            stream[7] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7 + 7) | 128);
            stream[8] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7 + 7 + 7));
            Current += 9;

            return;
        }
        base.Write7Bit(value);
    }

    public override void Write(byte* buffer, int length)
    {
        if (RemainingWriteLength <= 0)
            UpdateLocalBuffer(true);
        if (RemainingWriteLength < length)
        {
            base.Write(buffer, length);

            return;
        }

        int pos = 0;

        while (pos + 8 <= length)
        {
            *(long*)(Current + pos) = *(long*)(buffer + pos);
            pos += 8;
        }
        if (pos + 4 <= length)
        {
            *(int*)(Current + pos) = *(int*)(buffer + pos);
            pos += 4;
        }
        if (pos + 2 <= length)
        {
            *(short*)(Current + pos) = *(short*)(buffer + pos);
            pos += 2;
        }
        if (pos + 1 <= length)
        {
            *(Current + pos) = *(buffer + pos);
        }
        Current += length;
    }

    public override void Write(byte[] value, int offset, int count)
    {
        if (Current + count <= LastWrite)
        {
            Marshal.Copy(value, offset, (IntPtr)Current, count);
            Current += count;

            return;
        }
        Write2(value, offset, count);
    }

    private void Write2(byte[] value, int offset, int count)
    {
        while (count > 0)
        {
            if (RemainingWriteLength <= 0)
                UpdateLocalBuffer(true);
            int availableLength = Math.Min((int)RemainingWriteLength, count);

            Marshal.Copy(value, offset, (IntPtr)Current, availableLength);
            Current += availableLength;

            count -= availableLength;
            offset += availableLength;
        }
    }

    public override byte ReadUInt8()
    {
        const int size = sizeof(byte);
        if (Current < LastRead)
        {
            byte value = *Current;
            Current += size;

            return value;
        }
        return base.ReadUInt8();
    }

    public override short ReadInt16()
    {
        const int size = sizeof(short);
        if (Current + size <= LastRead)
        {
            short value = *(short*)Current;
            Current += size;

            return value;
        }
        return base.ReadInt16();
    }

    public override int ReadInt32()
    {
        const int size = sizeof(int);
        if (Current + size <= LastRead)
        {
            int value = *(int*)Current;
            Current += size;

            return value;
        }
        return base.ReadInt32();
    }

    public override long ReadInt64()
    {
        const int size = sizeof(long);
        if (Current + size <= LastRead)
        {
            long value = *(long*)Current;
            Current += size;

            return value;
        }
        return base.ReadInt64();
    }

    public override uint Read7BitUInt32()
    {
        const int size = 5;
        byte* stream = Current;

        if (stream + size <= LastRead)
        {
            uint value11 = stream[0];
            if (value11 < 128)
            {
                Current += 1;
                return value11;
            }
            value11 ^= (uint)stream[1] << 7;
            if (value11 < 128 * 128)
            {
                Current += 2;
                return value11 ^ 0x80;
            }
            value11 ^= (uint)stream[2] << 14;
            if (value11 < 128 * 128 * 128)
            {
                Current += 3;
                return value11 ^ 0x4080;
            }
            value11 ^= (uint)stream[3] << 21;
            if (value11 < 128 * 128 * 128 * 128)
            {
                Current += 4;
                return value11 ^ 0x204080;
            }
            value11 ^= ((uint)stream[4] << 28) ^ 0x10204080;
            Current += 5;
            return value11;
        }
        return base.Read7BitUInt32();
    }

    public override ulong Read7BitUInt64()
    {
        const int size = 9;
        byte* stream = Current;

        if (stream + size <= LastRead)
        {
            ulong value11 = stream[0];
            if (value11 < 128)
            {
                Current += 1;
                return value11;
            }
            value11 ^= (ulong)stream[1] << 7;
            if (value11 < 128 * 128)
            {
                Current += 2;
                return value11 ^ 0x80;
            }
            value11 ^= (ulong)stream[2] << (7 + 7);
            if (value11 < 128 * 128 * 128)
            {
                Current += 3;
                return value11 ^ 0x4080;
            }
            value11 ^= (ulong)stream[3] << (7 + 7 + 7);
            if (value11 < 128 * 128 * 128 * 128)
            {
                Current += 4;
                return value11 ^ 0x204080;
            }
            value11 ^= (ulong)stream[4] << (7 + 7 + 7 + 7);
            if (value11 < 128L * 128 * 128 * 128 * 128)
            {
                Current += 5;
                return value11 ^ 0x10204080L;
            }
            value11 ^= (ulong)stream[5] << (7 + 7 + 7 + 7 + 7);
            if (value11 < 128L * 128 * 128 * 128 * 128 * 128)
            {
                Current += 6;
                return value11 ^ 0x810204080L;
            }
            value11 ^= (ulong)stream[6] << (7 + 7 + 7 + 7 + 7 + 7);
            if (value11 < 128L * 128 * 128 * 128 * 128 * 128 * 128)
            {
                Current += 7;
                return value11 ^ 0x40810204080L;
            }
            value11 ^= (ulong)stream[7] << (7 + 7 + 7 + 7 + 7 + 7 + 7);
            if (value11 < 128L * 128 * 128 * 128 * 128 * 128 * 128 * 128)
            {
                Current += 8;
                return value11 ^ 0x2040810204080L;
            }
            value11 ^= (ulong)stream[8] << (7 + 7 + 7 + 7 + 7 + 7 + 7 + 7);
            Current += 9;

            return value11 ^ 0x102040810204080L;
        }
        return base.Read7BitUInt64();
    }



    public override int Read(byte[] value, int offset, int count)
    {
        if (RemainingReadLength >= count)
        {
            Marshal.Copy((IntPtr)Current, value, offset, count);
            Current += count;

            return count;
        }
        return Read2(value, offset, count);
    }

    private int Read2(byte[] value, int offset, int count)
    {
        int origionalCount = count;
        while (count > 0)
        {
            if (RemainingReadLength <= 0)
                UpdateLocalBuffer(false);

            int availableLength = Math.Min((int)RemainingReadLength, count);
            Marshal.Copy((IntPtr)Current, value, offset, availableLength);

            Current += availableLength;
            count -= availableLength;
            offset += availableLength;
        }
        return origionalCount;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        // Do Nothing
    }
}