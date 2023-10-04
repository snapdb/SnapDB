//******************************************************************************************************
//  IndividualEncodingBase.cs - Gbtc
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
/// Represents the base class for individual value encoding strategies that allow compressing of a single value.
/// </summary>
/// <typeparam name="T">The type of values to encode.</typeparam>
public abstract class IndividualEncodingBase<T>
{
    #region [ Properties ]

    /// <summary>
    /// Gets a value indicating whether this encoding contains an end-of-stream symbol.
    /// </summary>
    public abstract bool ContainsEndOfStreamSymbol { get; }

    /// <summary>
    /// Gets the byte value representing the end-of-stream symbol if applicable.
    /// May throw NotSupportedException if <see cref="ContainsEndOfStreamSymbol"/> is <c>false</c>.
    /// </summary>
    public abstract byte EndOfStreamSymbol { get; }

    /// <summary>
    /// Gets a value indicating whether this encoding strategy uses the previous value for encoding.
    /// </summary>
    public abstract bool UsesPreviousValue { get; }

    /// <summary>
    /// Gets the maximum amount of space required by the compression algorithm.
    /// </summary>
    public abstract int MaxCompressionSize { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Encodes <paramref name="value"/> and writes it to the specified binary <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The binary stream to write to.</param>
    /// <param name="prevValue">The previous value for encoding reference, if required by <see cref="UsesPreviousValue"/>; otherwise, <c>null</c>.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The number of bytes necessary to encode this key-value pair.</returns>
    public abstract void Encode(BinaryStreamBase stream, T prevValue, T value);

    /// <summary>
    /// Decodes <paramref name="value"/> from the specified binary <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The binary stream to read from.</param>
    /// <param name="prevValue">The previous value used for decoding reference, if required by <see cref="UsesPreviousValue"/>; otherwise, <c>null</c>.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">Indicates whether the end of the stream has been reached by returning <c>true</c>. If there is no end-of-stream symbol, always returns <c>false</c>.</param>
    /// <returns>The number of bytes necessary to decode the next key-value pair.</returns>
    public abstract void Decode(BinaryStreamBase stream, T prevValue, T value, out bool isEndOfStream);

    /// <summary>
    /// Encodes <paramref name="value"/> and writes it to the specified memory <paramref name="stream"/>, returning the encoded data length.
    /// </summary>
    /// <param name="stream">A pointer to the memory stream.</param>
    /// <param name="prevValue">The previous value for encoding reference, if required by <see cref="UsesPreviousValue"/>; otherwise, <c>null</c>.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The length required to encode the key-value pair in bytes.</returns>
    public virtual unsafe int Encode(byte* stream, T prevValue, T value)
    {
        BinaryStreamPointerWrapper bs = new(stream, MaxCompressionSize);
        Encode(bs, prevValue, value);
        return (int)bs.Position;
    }

    /// <summary>
    /// Decodes <paramref name="value"/> from the specified memory <paramref name="stream"/>, returning the decoded data length.
    /// </summary>
    /// <param name="stream">A pointer to the memory stream.</param>
    /// <param name="prevValue">The previous value used for decoding reference, if required by <see cref="UsesPreviousValue"/>; otherwise, <c>null</c>.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="isEndOfStream">Indicates whether the end of the stream has been reached by returning <c>true</c>. If there is not end-of-stream symbol, always returns <c>false</c>.</param>
    public virtual unsafe int Decode(byte* stream, T prevValue, T value, out bool isEndOfStream)
    {
        BinaryStreamPointerWrapper bs = new(stream, MaxCompressionSize);
        Decode(bs, prevValue, value, out isEndOfStream);

        return (int)bs.Position;
    }

    /// <summary>
    /// Clones this encoding method.
    /// </summary>
    /// <returns>A clone.</returns>
    public abstract IndividualEncodingBase<T> Clone();

    #endregion
}