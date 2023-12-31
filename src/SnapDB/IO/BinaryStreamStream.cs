﻿//******************************************************************************************************
//  BinaryStreamBase.cs - Gbtc
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
//  08/16/2014 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.IO;

/// <summary>
/// A <see cref="Stream"/> wrapper around a <see cref="BinaryStreamBase"/>.
/// </summary>
/// <remarks>
/// A <see cref="Stream"/> inherits from <see cref="MarshalByRefObject"/>
/// which prevents any methods from inlining. Therefore, a <see cref="BinaryStreamBase"/>
/// will not inherit from <see cref="Stream"/>.
/// </remarks>
public class BinaryStreamStream : Stream
{
    #region [ Members ]

    /// <summary>
    /// A new <see cref="BaseStream"/> that reads and writes to a little-endian stream.
    /// </summary>
    public readonly BinaryStreamBase BaseStream;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="Stream"/> wrapper around a <see cref="BinaryStreamBase"/>.
    /// </summary>
    /// <param name="baseStream"></param>
    internal BinaryStreamStream(BinaryStreamBase baseStream)
    {
        BaseStream = baseStream;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the stream supports reading; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanRead => BaseStream.CanRead;

    /// <summary>
    /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the stream supports seeking; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanSeek => BaseStream.CanSeek;

    /// <summary>
    /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the stream supports seeking; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanWrite => BaseStream.CanWrite;

    /// <summary>
    /// When overridden in a derived class, gets the length in bytes of the stream.
    /// </summary>
    /// <returns>
    /// A long value representing the length of the stream in bytes.
    /// </returns>
    /// <exception cref="NotSupportedException">A class derived from Stream does not support seeking.</exception>
    /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
    public override long Length => BaseStream.Length;

    /// <summary>
    /// When overridden in a derived class, gets or sets the position within the current stream.
    /// </summary>
    /// <returns>
    /// The current position within the stream.
    /// </returns>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
    /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
    public override long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// </summary>
    /// <exception cref="IOException">An I/O error occurs. </exception>
    public override void Flush()
    {
        BaseStream.Flush();
    }

    /// <summary>
    /// When overridden in a derived class, sets the position within the current stream.
    /// </summary>
    /// <returns>
    /// The new position within the current stream.
    /// </returns>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output.</exception>
    /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return BaseStream.Seek(offset, origin);
    }

    /// <summary>
    /// When overridden in a derived class, sets the length of the current stream.
    /// </summary>
    /// <param name="value">The desired length of the current stream in bytes.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output.</exception>
    /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
    public override void SetLength(long value)
    {
        BaseStream.SetLength(value);
    }

    /// <summary>
    /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <returns>
    /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
    /// </returns>
    /// <param name="buffer">
    /// An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/>
    /// and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.
    /// </param>
    /// <param name="offset">
    /// The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.
    /// </param>
    /// <param name="count">
    /// The maximum number of bytes to be read from the current stream.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
    /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return BaseStream.Read(buffer, offset, count);
    }

    /// <summary>
    /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
    /// </summary>
    /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param>
    /// <param name="count">The number of bytes to be written to the current stream. </param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        BaseStream.Write(buffer, offset, count);
    }

    #endregion
}