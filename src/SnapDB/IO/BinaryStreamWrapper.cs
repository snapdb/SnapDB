//******************************************************************************************************
//  BinaryStreamWrapper.cs - Gbtc
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

namespace SnapDB.IO;

/// <summary>
/// A simple wrapper of a <see cref="Stream"/>. Provides no caching functionality.
/// </summary>
public class BinaryStreamWrapper : BinaryStreamBase
{
    #region [ Members ]

    private readonly bool m_ownsStream;
    private readonly Stream m_stream;

    #endregion

    #region [ Constructors ]

    public BinaryStreamWrapper(Stream stream, bool ownsStream)
    {
        m_ownsStream = ownsStream;
        m_stream = stream;
    }

    #endregion

    #region [ Properties ]

    public override bool CanWrite => m_stream.CanWrite;

    public override long Length => m_stream.Length;

    public override bool CanRead => m_stream.CanRead;

    public override bool CanSeek => m_stream.CanSeek;

    /// <summary>
    /// Gets or sets the current position for the stream.
    /// </summary>
    /// <remarks>
    /// It is important to use this to get or set the position from the underlying stream since
    /// this class buffers the results of the query. Setting this field does not guarantee that
    /// the underlying stream will get set. Call FlushToUnderlyingStream to accomplish this.
    /// </remarks>
    public override long Position
    {
        get => m_stream.Position;
        set => m_stream.Position = value;
    }

    #endregion

    #region [ Methods ]

    public override void Write(byte value)
    {
        m_stream.WriteByte(value);
    }

    public override void Write(byte[] value, int offset, int count)
    {
        m_stream.Write(value, offset, count);
    }


    public override byte ReadUInt8()
    {
        int value = m_stream.ReadByte();
        if (value < 0)
            throw new EndOfStreamException();

        return (byte)value;
    }

    public override void Flush()
    {
        m_stream.Flush();
    }

    public override void SetLength(long value)
    {
        m_stream.SetLength(value);
    }

    public override int Read(byte[] value, int offset, int count)
    {
        return m_stream.Read(value, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && m_ownsStream)
            m_stream.Dispose();
        base.Dispose(disposing);
    }

    #endregion
}