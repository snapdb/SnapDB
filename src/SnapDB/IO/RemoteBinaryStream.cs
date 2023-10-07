//******************************************************************************************************
//  RemoteBinaryStream.cs - Gbtc
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
//  12/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Threading;

namespace SnapDB.IO;

/// <summary>
/// Represents a remote binary stream for reading and writing data over a network connection.
/// </summary>
public class RemoteBinaryStream : BinaryStreamBase
{
    #region [ Members ]

    private const int BufferSize = 1420;
    private readonly byte[] m_receiveBuffer;
    private int m_receiveLength;
    private int m_receivePosition;
    private readonly byte[] m_sendBuffer;
    private int m_sendLength;

    private readonly Stream m_stream;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the RemoteBinaryStream class with the specified stream for communication.
    /// </summary>
    /// <param name="stream">The underlying stream used for communication.</param>
    /// <param name="workerThreadSynchronization">
    /// An optional instance of WorkerThreadSynchronization for synchronization. If not provided, a new instance will be created.
    /// </param>
    /// <exception cref="Exception">Thrown if the processor is not little-endian (not supported).</exception>
    public RemoteBinaryStream(Stream stream, WorkerThreadSynchronization? workerThreadSynchronization = null)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("BigEndian processors are not supported");

        workerThreadSynchronization ??= new WorkerThreadSynchronization();

        WorkerThreadSynchronization = workerThreadSynchronization;
        m_receiveBuffer = new byte[BufferSize];
        m_sendBuffer = new byte[BufferSize];
        m_sendLength = 0;
        m_receiveLength = 0;
        m_receivePosition = 0;
        m_stream = stream;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets a value indicating whether the stream supports reading.
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// Gets a value indicating whether the stream supports seeking (positioning).
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// Gets a value indicating whether the stream allows writing.
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> since getting the length of this stream is not supported.
    /// </summary>
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> since setting or getting the position of this stream is not supported.
    /// </summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the WorkerThreadSynchronization instance used for synchronization in this stream.
    /// </summary>
    public WorkerThreadSynchronization WorkerThreadSynchronization { get; }

    /// <summary>
    /// Gets the number of bytes available in the receive buffer for reading.
    /// </summary>
    protected int ReceiveBufferAvailable => m_receiveLength - m_receivePosition;

    /// <summary>
    /// Gets the amount of free space available in the send buffer.
    /// </summary>
    private int SendBufferFreeSpace => BufferSize - m_sendLength;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Disposes of the RemoteBinaryStream, releasing any resources associated with it.
    /// </summary>
    /// <param name="disposing">
    /// A flag indicating whether the method is called from the finalizer or directly by user code.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            WorkerThreadSynchronization.BeginSafeToCallbackRegion();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Flushes any buffered data in the send buffer to the underlying stream.
    /// </summary>
    public override void Flush()
    {
        if (m_sendLength <= 0)
            return;

        WorkerThreadSynchronization.BeginSafeToCallbackRegion();

        try
        {
            m_stream.Write(m_sendBuffer, 0, m_sendLength);
            m_stream.Flush();
        }
        finally
        {
            WorkerThreadSynchronization.EndSafeToCallbackRegion();
        }

        m_sendLength = 0;
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> since setting the length of this stream is not supported.
    /// </summary>
    /// <param name="value">The new length of the stream, which is not supported.</param>
    /// <exception cref="NotSupportedException">Thrown to indicate that setting the stream length is not supported.</exception>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Reads a specified number of bytes from the stream into a byte array, starting at the specified offset.
    /// </summary>
    /// <param name="buffer">The byte array where the read data will be stored.</param>
    /// <param name="offset">The offset in the byte array where reading will start.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count <= 0)
            return 0;

        int receiveBufferLength = ReceiveBufferAvailable;

        // If there is enough in the receive buffer to fulfill this request.
        if (count <= receiveBufferLength)
        {
            Array.Copy(m_receiveBuffer, m_receivePosition, buffer, offset, count);
            m_receivePosition += count;
            return count;
        }

        int originalCount = count;

        // First, empty the receive buffer.
        if (receiveBufferLength > 0)
        {
            Array.Copy(m_receiveBuffer, m_receivePosition, buffer, offset, receiveBufferLength);
            m_receivePosition = 0;
            m_receiveLength = 0;
            offset += receiveBufferLength;
            count -= receiveBufferLength;
        }

        // If still asking for more than 100 bytes, skip the receive buffer
        // and copy directly to the destination.
        if (count > 100)
        {
            // Loop, since ReceiveFromSocket can return partial results.
            while (count > 0)
            {
                WorkerThreadSynchronization.BeginSafeToCallbackRegion();
                try
                {
                    receiveBufferLength = m_stream.Read(buffer, offset, count);
                }
                finally
                {
                    WorkerThreadSynchronization.EndSafeToCallbackRegion();
                }

                if (receiveBufferLength == 0)
                    throw new EndOfStreamException();

                offset += receiveBufferLength;
                count -= receiveBufferLength;
            }

            return originalCount;
        }

        // With fewer than 100 bytes requested, 
        // first fill up the receive buffer, 
        // then copy this to the destination.
        int prebufferLength = m_receiveBuffer.Length;
        m_receiveLength = 0;

        while (m_receiveLength < count)
        {
            WorkerThreadSynchronization.BeginSafeToCallbackRegion();

            try
            {
                receiveBufferLength = m_stream.Read(m_receiveBuffer, m_receiveLength, prebufferLength);
            }
            finally
            {
                WorkerThreadSynchronization.EndSafeToCallbackRegion();
            }

            if (receiveBufferLength == 0)
                throw new EndOfStreamException();

            m_receiveLength += receiveBufferLength;
            prebufferLength -= receiveBufferLength;
        }

        Array.Copy(m_receiveBuffer, 0, buffer, offset, count);
        m_receivePosition = count;

        return originalCount;
    }

    /// <summary>
    /// Writes a single byte to the stream.
    /// </summary>
    /// <param name="value">The byte value to write to the stream.</param>
    public override void Write(byte value)
    {
        if (m_sendLength < BufferSize)
        {
            m_sendBuffer[m_sendLength] = value;
            m_sendLength++;
            return;
        }

        base.Write(value);
    }

    /// <summary>
    /// Writes an 8-byte (64-bit) signed integer to the stream.
    /// </summary>
    /// <param name="value">The long value to write to the stream.</param>
    public override unsafe void Write(long value)
    {
        if (m_sendLength <= BufferSize - 8)
        {
            fixed (byte* ptr = m_sendBuffer)
            {
                *(long*)(ptr + m_sendLength) = value;
                m_sendLength += 8;
                return;
            }
        }

        base.Write(value);
    }

    /// <summary>
    /// Writes a 4-byte (32-bit) signed integer to the stream.
    /// </summary>
    /// <param name="value">The integer value to write to the stream.</param>
    public override unsafe void Write(int value)
    {
        if (m_sendLength <= BufferSize - 4)
        {
            fixed (byte* ptr = m_sendBuffer)
            {
                *(int*)(ptr + m_sendLength) = value;
                m_sendLength += 4;
                return;
            }
        }

        base.Write(value);
    }

    /// <summary>
    /// Writes a 7-bit encoded unsigned 64-bit integer (UInt64) to the stream.
    /// </summary>
    /// <param name="value">The 7-bit encoded unsigned 64-bit integer (UInt64) value to write to the stream.</param>
    public override unsafe void Write7Bit(ulong value)
    {
        if (m_sendLength <= BufferSize - 9)
        {
            fixed (byte* ptr = m_sendBuffer)
            {
                byte* stream = ptr + m_sendLength;

                if (value < 128)
                {
                    stream[0] = (byte)value;
                    m_sendLength += 1;
                    return;
                }

                stream[0] = (byte)(value | 128);

                if (value < 128 * 128)
                {
                    stream[1] = (byte)(value >> 7);
                    m_sendLength += 2;
                    return;
                }

                stream[1] = (byte)((value >> 7) | 128);

                if (value < 128 * 128 * 128)
                {
                    stream[2] = (byte)(value >> 14);
                    m_sendLength += 3;
                    return;
                }

                stream[2] = (byte)((value >> 14) | 128);

                if (value < 128 * 128 * 128 * 128)
                {
                    stream[3] = (byte)(value >> 21);
                    m_sendLength += 4;
                    return;
                }

                stream[3] = (byte)((value >> (7 + 7 + 7)) | 128);

                if (value < 128L * 128 * 128 * 128 * 128)
                {
                    stream[4] = (byte)(value >> (7 + 7 + 7 + 7));
                    m_sendLength += 5;
                    return;
                }

                stream[4] = (byte)((value >> (7 + 7 + 7 + 7)) | 128);

                if (value < 128L * 128 * 128 * 128 * 128 * 128)
                {
                    stream[5] = (byte)(value >> (7 + 7 + 7 + 7 + 7));
                    m_sendLength += 6;
                    return;
                }

                stream[5] = (byte)((value >> (7 + 7 + 7 + 7 + 7)) | 128);

                if (value < 128L * 128 * 128 * 128 * 128 * 128 * 128)
                {
                    stream[6] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7));
                    m_sendLength += 7;
                    return;
                }

                stream[6] = (byte)((value >> (7 + 7 + 7 + 7 + 7 + 7)) | 128);

                if (value < 128L * 128 * 128 * 128 * 128 * 128 * 128 * 128)
                {
                    stream[7] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7 + 7));
                    m_sendLength += 8;
                    return;
                }

                stream[7] = (byte)((value >> (7 + 7 + 7 + 7 + 7 + 7 + 7)) | 128);
                stream[8] = (byte)(value >> (7 + 7 + 7 + 7 + 7 + 7 + 7 + 7));
                m_sendLength += 9;
                return;
            }
        }

        base.Write7Bit(value);
    }

    /// <summary>
    /// Reads a single unsigned byte (UInt8) from the stream.
    /// </summary>
    /// <returns>The unsigned byte (UInt8) read from the stream.</returns>
    public override byte ReadUInt8()
    {
        if (m_receivePosition >= m_receiveLength)
            return base.ReadUInt8();

        byte value = m_receiveBuffer[m_receivePosition];
        m_receivePosition++;

        return value;
    }

    /// <summary>
    /// Reads a 4-byte (32-bit) signed integer from the stream.
    /// </summary>
    /// <returns>The 4-byte (32-bit) signed integer read from the stream.</returns>
    public override unsafe int ReadInt32()
    {
        if (m_receivePosition <= m_receiveLength - 4)
        {
            fixed (byte* ptr = m_receiveBuffer)
            {
                int value = *(int*)(ptr + m_receivePosition);
                m_receivePosition += 4;
                return value;
            }
        }

        return base.ReadInt32();
    }

    /// <summary>
    /// Reads an 8-byte (64-bit) signed integer from the stream.
    /// </summary>
    /// <returns>The 8-byte (64-bit) signed integer read from the stream.</returns>
    public override unsafe long ReadInt64()
    {
        if (m_receivePosition <= m_receiveLength - 8)
        {
            fixed (byte* ptr = m_receiveBuffer)
            {
                long value = *(long*)(ptr + m_receivePosition);
                m_receivePosition += 8;
                return value;
            }
        }

        return base.ReadInt64();
    }

    /// <summary>
    /// Reads a 7-bit encoded unsigned 64-bit integer (UInt64) from the stream.
    /// </summary>
    /// <returns>The 7-bit encoded unsigned 64-bit integer (UInt64) read from the stream.</returns>
    public override unsafe ulong Read7BitUInt64()
    {
        if (m_receivePosition <= m_receiveLength - 9)
        {
            fixed (byte* ptr = m_receiveBuffer)
            {
                byte* stream = ptr + m_receivePosition;
                ulong value11 = stream[0];

                if (value11 < 128)
                {
                    m_receivePosition += 1;
                    return value11;
                }

                value11 ^= (ulong)stream[1] << 7;

                if (value11 < 128 * 128)
                {
                    m_receivePosition += 2;
                    return value11 ^ 0x80;
                }

                value11 ^= (ulong)stream[2] << (7 + 7);

                if (value11 < 128 * 128 * 128)
                {
                    m_receivePosition += 3;
                    return value11 ^ 0x4080;
                }

                value11 ^= (ulong)stream[3] << (7 + 7 + 7);

                if (value11 < 128 * 128 * 128 * 128)
                {
                    m_receivePosition += 4;
                    return value11 ^ 0x204080;
                }

                value11 ^= (ulong)stream[4] << (7 + 7 + 7 + 7);

                if (value11 < 128L * 128 * 128 * 128 * 128)
                {
                    m_receivePosition += 5;
                    return value11 ^ 0x10204080L;
                }

                value11 ^= (ulong)stream[5] << (7 + 7 + 7 + 7 + 7);

                if (value11 < 128L * 128 * 128 * 128 * 128 * 128)
                {
                    m_receivePosition += 6;
                    return value11 ^ 0x810204080L;
                }

                value11 ^= (ulong)stream[6] << (7 + 7 + 7 + 7 + 7 + 7);

                if (value11 < 128L * 128 * 128 * 128 * 128 * 128 * 128)
                {
                    m_receivePosition += 7;
                    return value11 ^ 0x40810204080L;
                }

                value11 ^= (ulong)stream[7] << (7 + 7 + 7 + 7 + 7 + 7 + 7);

                if (value11 < 128L * 128 * 128 * 128 * 128 * 128 * 128 * 128)
                {
                    m_receivePosition += 8;
                    return value11 ^ 0x2040810204080L;
                }

                value11 ^= (ulong)stream[8] << (7 + 7 + 7 + 7 + 7 + 7 + 7 + 7);
                m_receivePosition += 9;
                return value11 ^ 0x102040810204080L;
            }
        }

        return base.Read7BitUInt64();
    }

    /// <summary>
    /// Writes a specified number of bytes from a byte array to the stream.
    /// </summary>
    /// <param name="buffer">The byte array containing the data to be written to the stream.</param>
    /// <param name="offset">The offset in the byte array where writing will start.</param>
    /// <param name="count">The number of bytes to write from the byte array.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (SendBufferFreeSpace < count)
            Flush();

        if (count > 100)
        {
            Flush();
            WorkerThreadSynchronization.BeginSafeToCallbackRegion();

            try
            {
                m_stream.Write(buffer, offset, count);
            }
            finally
            {
                WorkerThreadSynchronization.EndSafeToCallbackRegion();
            }
        }
        else
        {
            Array.Copy(buffer, offset, m_sendBuffer, m_sendLength, count);
            m_sendLength += count;
        }
    }

    #endregion
}