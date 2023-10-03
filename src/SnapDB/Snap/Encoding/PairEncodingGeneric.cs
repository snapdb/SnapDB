//******************************************************************************************************
//  CombinedEncodingGeneric`2.cs - Gbtc
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
//  02/22/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap;

namespace SnapDB.Snap.Encoding;

/// <summary>
/// Internal class for encoding and decoding pairs of generic key-value types.
/// </summary>
/// <typeparam name="TKey">The generic type of the keys.</typeparam>
/// <typeparam name="TValue">The generic type of the values.</typeparam>
/// <seealso cref="PairEncodingBase{TKey, TValue}" />
internal class PairEncodingGeneric<TKey, TValue>
    : PairEncodingBase<TKey, TValue>
    where TKey : SnapTypeBase<TKey>, new()
    where TValue : SnapTypeBase<TValue>, new()
{
    private readonly IndividualEncodingBase<TKey> m_keyEncoding;
    private readonly IndividualEncodingBase<TValue> m_valueEncoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairEncodingGeneric{TKey, TValue}"/> class with the specified encoding method.
    /// </summary>
    /// <param name="encodingMethod">The encoding method definition used for initialization.</param>
    public PairEncodingGeneric(EncodingDefinition encodingMethod)
    {
        EncodingMethod = encodingMethod;
        m_keyEncoding = Library.Encodings.GetEncodingMethod<TKey>(encodingMethod.KeyEncodingMethod);
        m_valueEncoding = Library.Encodings.GetEncodingMethod<TValue>(encodingMethod.ValueEncodingMethod);
    }

    public override EncodingDefinition EncodingMethod { get; }

    /// <summary>
    /// Gets if the previous key will need to be presented to the encoding algorithms to
    /// property encode the next sample. Returning <c>false</c> will cause nulls to be passed
    /// in a parameters to the encoding.
    /// </summary>
    public override bool UsesPreviousKey => m_keyEncoding.UsesPreviousValue;

    /// <summary>
    /// Gets if the previous value will need to be presented to the encoding algorithms to
    /// property encode the next sample. Returning <c>false</c> will cause nulls to be passed
    /// in a parameters to the encoding.
    /// </summary>
    public override bool UsesPreviousValue => m_valueEncoding.UsesPreviousValue;

    /// <summary>
    /// Gets the maximum amount of space that is required for the compression algorithm. This
    /// prevents lower levels from having overflows on the underlying streams. It is critical
    /// that this value be correct. Error on the side of too large of a value as a value
    /// too small will corrupt data and be next to impossible to track down the point of corruption
    /// </summary>
    public override int MaxCompressionSize => m_keyEncoding.MaxCompressionSize + m_valueEncoding.MaxCompressionSize;

    /// <summary>
    /// Gets if the stream supports a symbol that 
    /// represents that the end of the stream has been encountered.
    /// </summary>
    /// <remarks>
    /// An example of a symbol would be the byte code 0xFF.
    /// In this case, if the first byte of the
    /// word is 0xFF, the encoding has specifically
    /// designated this as the end of the stream. Therefore, calls to
    /// Decompress will result in an end of stream exception.
    /// 
    /// Failing to reserve a code as the end of stream will mean that
    /// streaming points will include its own symbol to represent the end of the
    /// stream, taking 1 extra byte per point encoded.
    /// </remarks>
    public override bool ContainsEndOfStreamSymbol => m_keyEncoding.ContainsEndOfStreamSymbol;

    /// <summary>
    /// The byte code to use as the end of stream symbol.
    /// May throw NotSupportedException if <see cref="PairEncodingBase{TKey,TValue}.ContainsEndOfStreamSymbol"/> is <c>false</c>.
    /// </summary>
    public override byte EndOfStreamSymbol => m_keyEncoding.EndOfStreamSymbol;

    /// <summary>
    /// Encodes key and value and writes them to a binary stream using the specified encoding methods.
    /// </summary>
    /// <param name="stream">The binary stream to which to write the encoded key and value.</param>
    /// <param name="prevKey">The previously encoded key.</param>
    /// <param name="prevValue">The previously encoded value.</param>
    /// <param name="key">The key to be encoded and written.</param>
    /// <param name="value">The value to be encoded and written.</param>
    public override void Encode(BinaryStreamBase stream, TKey prevKey, TValue prevValue, TKey key, TValue value)
    {
        m_keyEncoding.Encode(stream, prevKey, key);
        m_valueEncoding.Encode(stream, prevValue, value);
    }

    /// <summary>
    /// Decodes key and value from a binary stream using the specified encoding methods and indicates whether it's the end of the stream.
    /// </summary>
    /// <param name="stream">The binary stream from which to decode the key and value.</param>
    /// <param name="prevKey">The previously decoded key.</param>
    /// <param name="prevValue">The previously decoded value.</param>
    /// <param name="key">The decoded key.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">A boolean indicating whether the end of the stream has been reached.</param>
    public override void Decode(BinaryStreamBase stream, TKey prevKey, TValue prevValue, TKey key, TValue value, out bool isEndOfStream)
    {
        m_keyEncoding.Decode(stream, prevKey, key, out isEndOfStream);
        if (isEndOfStream)
            return;

        m_valueEncoding.Decode(stream, prevValue, value, out isEndOfStream);
    }

    /// <summary>
    /// Decodes key and value from a byte pointer using the specified encoding methods and indicates whether it's the end of the stream.
    /// </summary>
    /// <param name="stream">The byte pointer from which to decode the key and value.</param>
    /// <param name="prevKey">The previously decoded key.</param>
    /// <param name="prevValue">The previously decoded value.</param>
    /// <param name="key">The decoded key.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">A boolean indicating whether the end of the stream has been reached.</param>
    /// <returns>The number of bytes consumed from the byte pointer during decoding.</returns>
    public override unsafe int Decode(byte* stream, TKey prevKey, TValue prevValue, TKey key, TValue value, out bool isEndOfStream)
    {
        int length = m_keyEncoding.Decode(stream, prevKey, key, out isEndOfStream);
        if (isEndOfStream)
            return length;

        length += m_valueEncoding.Decode(stream + length, prevValue, value, out isEndOfStream);
        return length;
    }

    /// <summary>
    /// Encodes key and value and writes them to a byte pointer using the specified encoding methods.
    /// </summary>
    /// <param name="stream">The byte pointer to which to write the encoded key and value.</param>
    /// <param name="prevKey">The previously encoded key.</param>
    /// <param name="prevValue">The previously encoded value.</param>
    /// <param name="key">The key to be encoded and written.</param>
    /// <param name="value">The value to be encoded and written.</param>
    /// <returns>The number of bytes written to the byte pointer during encoding.</returns>
    public override unsafe int Encode(byte* stream, TKey prevKey, TValue prevValue, TKey key, TValue value)
    {
        int length = m_keyEncoding.Encode(stream, prevKey, key);
        length += m_valueEncoding.Encode(stream + length, prevValue, value);
        return length;
    }

    /// <summary>
    /// Clones this encoding method.
    /// </summary>
    /// <returns>A clone.</returns>
    public override PairEncodingBase<TKey, TValue> Clone()
    {
        return new PairEncodingGeneric<TKey, TValue>(EncodingMethod);
    }
}
