//******************************************************************************************************
//  StreamEncodingGeneric`2.cs - Gbtc
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
//  02/24/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
// 09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Encoding;

namespace SnapDB.Snap.Streaming;

/// <summary>
/// Allows any generic encoding definition to be wrapped to support stream encoding.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
internal class StreamEncodingGeneric<TKey, TValue> : StreamEncodingBase<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly PairEncodingBase<TKey, TValue> m_encoding;
    private readonly TKey m_prevKey;
    private readonly TValue m_prevValue;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="StreamEncodingGeneric{TKey,TValue}"/> based on the supplied <paramref name="encodingMethod"/>
    /// </summary>
    /// <param name="encodingMethod">the encoding method to use for the streaming</param>
    public StreamEncodingGeneric(EncodingDefinition encodingMethod)
    {
        m_encoding = Library.Encodings.GetEncodingMethod<TKey, TValue>(encodingMethod);
        m_prevKey = new TKey();
        m_prevValue = new TValue();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the definition of the encoding used.
    /// </summary>
    public override EncodingDefinition EncodingMethod => m_encoding.EncodingMethod;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Writes the end of the stream symbol to the <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">the stream to write to</param>
    public override void WriteEndOfStream(BinaryStreamBase stream)
    {
        if (m_encoding.ContainsEndOfStreamSymbol)
            stream.Write(m_encoding.EndOfStreamSymbol);

        else
            stream.Write((byte)0);
    }

    /// <summary>
    /// Encodes the current key-value pair to the stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="currentKey">The key to write.</param>
    /// <param name="currentValue">The value to write.</param>
    public override void Encode(BinaryStreamBase stream, TKey currentKey, TValue currentValue)
    {
        if (!m_encoding.ContainsEndOfStreamSymbol)
            stream.Write((byte)1);

        m_encoding.Encode(stream, m_prevKey, m_prevValue, currentKey, currentValue);
        currentKey.CopyTo(m_prevKey);
        currentValue.CopyTo(m_prevValue);
    }

    /// <summary>
    /// Attempts to read the next point from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="key">The key to store the value to.</param>
    /// <param name="value">The value to store to.</param>
    /// <returns><c>true</c> if successful; <c>false</c> if end of the stream has been reached.</returns>
    public override bool TryDecode(BinaryStreamBase stream, TKey key, TValue value)
    {
        if (!m_encoding.ContainsEndOfStreamSymbol)
            if (stream.ReadUInt8() == 0)
                return false;

        m_encoding.Decode(stream, m_prevKey, m_prevValue, key, value, out bool endOfStream);
        key.CopyTo(m_prevKey);
        value.CopyTo(m_prevValue);

        return !endOfStream;
    }

    /// <summary>
    /// Resets the encoder. Some encoders maintain streaming state data that should
    /// be reset when reading from a new stream.
    /// </summary>
    public override void ResetEncoder()
    {
        m_prevKey.Clear();
        m_prevValue.Clear();
    }

    #endregion
}