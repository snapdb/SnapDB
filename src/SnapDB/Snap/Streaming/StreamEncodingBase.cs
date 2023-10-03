//******************************************************************************************************
//  StreamEncodingBase.cs - Gbtc
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
//  08/10/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//******************************************************************************************************

using SnapDB.Snap.Encoding;
using SnapDB.IO;

namespace SnapDB.Snap.Streaming;

/// <summary>
/// An abstract base class for defining stream encoding methods used to encode and decode
/// key-value pairs of types <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TKey">The type of the keys to be encoded and decoded.</typeparam>
/// <typeparam name="TValue">The type of the values to be encoded and decoded.</typeparam>
public abstract class StreamEncodingBase<TKey, TValue>
        where TKey : SnapTypeBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {

        /// <summary>
        /// Gets the definition of the encoding used.
        /// </summary>
        public abstract EncodingDefinition EncodingMethod { get; }

        /// <summary>
        /// Writes the end of the stream symbol to the <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public abstract void WriteEndOfStream(BinaryStreamBase stream);

        /// <summary>
        /// Encodes the current key-value to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="currentKey">The key to write.</param>
        /// <param name="currentValue">The value to write.</param>
        public abstract void Encode(BinaryStreamBase stream, TKey currentKey, TValue currentValue);

        /// <summary>
        /// Attempts to read the next point from the stream. 
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="key">The key to store the value to.</param>
        /// <param name="value">The value to store to.</param>
        /// <returns><c>true</c> if successful; <c>false</c> if end of the stream has been reached.</returns>
        public abstract bool TryDecode(BinaryStreamBase stream, TKey key, TValue value);

        /// <summary>
        /// Resets the encoder. Some encoders maintain streaming state data that should
        /// be reset when reading from a new stream.
        /// </summary>
        public abstract void ResetEncoder();

    }

