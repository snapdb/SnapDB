//******************************************************************************************************
//  BufferedTreeStream'2.cs - Gbtc
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
//  09/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

public partial class UnionTreeStream<TKey, TValue>
{
    /// <summary>
    /// A wrapper around a <see cref="TreeStream{TKey,TValue}"/> that primarily supports peeking
    /// a value from a stream.
    /// </summary>
    private class BufferedTreeStream
        : IDisposable
    {
        public TreeStream<TKey, TValue> Stream;
        public bool IsValid;
        public readonly TKey CacheKey = new TKey();
        public readonly TValue CacheValue = new TValue();

        /// <summary>
        /// Creates a new instance of the <see cref="BufferedTreeStream"/> class.
        /// </summary>
        /// <param name="stream">The underlying tree stream.</param>
        public BufferedTreeStream(TreeStream<TKey, TValue> stream)
        {
            if (!stream.IsAlwaysSequential)
                throw new ArgumentException("Stream must gaurentee sequential data access");
            if (!stream.NeverContainsDuplicates)
                stream = new DistinctTreeStream<TKey, TValue>(stream);

            Stream = stream;
            EnsureCache();
        }

        /// <summary>
        /// Ensures that the cache value is valid.
        /// </summary>
        public void EnsureCache()
        {
            if (!IsValid)
                ReadToCache();
        }

        /// <summary>
        /// Reads the next value of the stream and updates the cache.
        /// </summary>
        public void ReadToCache()
        {
            IsValid = Stream.Read(CacheKey, CacheValue);
        }

        /// <summary>
        /// Reads the next available value into the provided key and value.
        /// </summary>
        /// <param name="key">The key to store the read key.</param>
        /// <param name="value">The value to store the read value.</param>
        public void Read(TKey key, TValue value)
        {
            if (IsValid)
            {
                IsValid = false;
                CacheKey.CopyTo(key);
                CacheValue.CopyTo(value);
                return;
            }
            throw new Exception("Cache is not valid. Programming Error.");
        }

        /// <summary>
        /// Writes the provided key and value to the cache.
        /// </summary>
        /// <param name="key">The key to write to the cache.</param>
        /// <param name="value">The value to write to the cache.</param>
        public void WriteToCache(TKey key, TValue value)
        {
            IsValid = true;
            key.CopyTo(CacheKey);
            value.CopyTo(CacheValue);
        }

        /// <summary>
        /// Disposes of the <see cref="BufferedTreeStream"/> and its underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

        }
    }

}
