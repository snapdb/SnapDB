//******************************************************************************************************
//  CombinedEncodingBase`2.cs - Gbtc
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
//  02/21/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.IO.Unmanaged;

namespace SnapDB.Snap.Encoding;

/// <summary>
/// Represents an encoding method that takes both a key and a value to encode.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public abstract class PairEncodingBase<TKey, TValue>
{
    #region [ Properties ]

    /// <summary>
    /// Gets the encoding method that this class implements.
    /// </summary>
    public abstract EncodingDefinition EncodingMethod { get; }

    /// <summary>
    /// Gets if the previous key will need to be presented to the encoding algorithms to
    /// property encode the next sample. Returning <c>false</c> will cause nulls to be passed
    /// in a parameters to the encoding.
    /// </summary>
    public abstract bool UsesPreviousKey { get; }

    /// <summary>
    /// Gets if the previous value will need to be presented to the encoding algorithms to
    /// property encode the next sample. Returning <c>false</c> will cause nulls to be passed
    /// in a parameters to the encoding.
    /// </summary>
    public abstract bool UsesPreviousValue { get; }

    /// <summary>
    /// Gets the maximum amount of space that is required for the compression algorithm. This
    /// prevents lower levels from having overflows on the underlying streams. It is critical
    /// that this value be correct. Error on the side of too large of a value as a value
    /// too small will corrupt data and be next to impossible to track down the point of corruption
    /// </summary>
    public abstract int MaxCompressionSize { get; }

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
    /// Failing to reserve a code as the end of stream will mean that
    /// streaming points will include its own symbol to represent the end of the
    /// stream, taking 1 extra byte per point encoded.
    /// </remarks>
    public abstract bool ContainsEndOfStreamSymbol { get; }

    /// <summary>
    /// The byte code to use as the end of stream symbol.
    /// May throw NotSupportedException if <see cref="ContainsEndOfStreamSymbol"/> is <c>false</c>.
    /// </summary>
    public abstract byte EndOfStreamSymbol { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Encodes key-value pairs and writes them to a byte pointer.
    /// </summary>
    /// <param name="stream">The byte pointer to which to write encoded data.</param>
    /// <param name="prevKey">The previously encoded key.</param>
    /// <param name="prevValue">The previously encoded value.</param>
    /// <param name="key">The key to be encoded and written.</param>
    /// <param name="value">The value to be encoded and written.</param>
    /// <returns>The position of the byte pointer after encoding.</returns>
    public virtual unsafe int Encode(byte* stream, TKey prevKey, TValue prevValue, TKey key, TValue value)
    {
        BinaryStreamPointerWrapper bs = new(stream, MaxCompressionSize);
        Encode(bs, prevKey, prevValue, key, value);
        return (int)bs.Position;
    }

    /// <summary>
    /// Decodes data from a byte pointer, producing key-value pairs and indicating whether it's the end of the stream.
    /// </summary>
    /// <param name="stream">The byte pointer from which to decode data.</param>
    /// <param name="prevKey">The previously decoded key.</param>
    /// <param name="prevValue">The previously decoded value.</param>
    /// <param name="key">The decoded key.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">A boolean indicating whether the end of the stream has been reached.</param>
    /// <returns>The position of the byte pointer after decoding.</returns>
    public virtual unsafe int Decode(byte* stream, TKey prevKey, TValue prevValue, TKey key, TValue value, out bool isEndOfStream)
    {
        BinaryStreamPointerWrapper bs = new(stream, MaxCompressionSize);
        Decode(bs, prevKey, prevValue, key, value, out isEndOfStream);

        return (int)bs.Position;
    }

    /// <summary>
    /// Encodes key-value pairs and writes them to a binary stream.
    /// </summary>
    /// <param name="stream">The binary stream to which to write encoded data.</param>
    /// <param name="prevKey">The previously encoded key.</param>
    /// <param name="prevValue">The previously encoded value.</param>
    /// <param name="key">The key to be encoded and written.</param>
    /// <param name="value">The value to be encoded and written.</param>
    public abstract void Encode(BinaryStreamBase stream, TKey prevKey, TValue prevValue, TKey key, TValue value);

    /// <summary>
    /// Decodes data from a binary stream, producing key-value pairs and indicating whether it's the end of the stream.
    /// </summary>
    /// <param name="stream">The binary stream from which to decode data.</param>
    /// <param name="prevKey">The previously decoded key.</param>
    /// <param name="prevValue">The previously decoded value.</param>
    /// <param name="key">The decoded key.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">A boolean indicating whether the end of the stream has been reached.</param>
    public abstract void Decode(BinaryStreamBase stream, TKey prevKey, TValue prevValue, TKey key, TValue value, out bool isEndOfStream);

    /// <summary>
    /// Clones this encoding method.
    /// </summary>
    /// <returns>A clone.</returns>
    public abstract PairEncodingBase<TKey, TValue> Clone();

    #endregion
}