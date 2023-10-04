//******************************************************************************************************
//  TimestampPointIDSeekFilter.cs - Gbtc
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
//  11/26/2014 - J. Ritchie Carroll
//       Generated original version of source code.
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;
using SnapDB.IO;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Filters;

/// <summary>
/// Represents a seek filter for a specific timestamp and point ID.
/// </summary>
public static class TimestampPointIdSeekFilter
{
    #region [ Members ]

    private class SeekToKey<TKey> : SeekFilterBase<TKey> where TKey : TimestampPointIdBase<TKey>, new()
    {
        #region [ Members ]

        private bool m_isEndReached;

        private readonly TKey m_keyToFind;

        #endregion

        #region [ Constructors ]

        private SeekToKey()
        {
            m_keyToFind = new TKey();
            StartOfFrame = new TKey();
            EndOfFrame = new TKey();
            StartOfRange = StartOfFrame;
            EndOfRange = EndOfFrame;
        }

        /// <summary>
        /// Creates a filter by reading from the stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public SeekToKey(BinaryStreamBase stream) : this()
        {
            m_keyToFind.Timestamp = stream.ReadUInt64();
            m_keyToFind.PointId = stream.ReadUInt64();
            m_keyToFind.CopyTo(StartOfRange);
            m_keyToFind.CopyTo(EndOfRange);
        }

        /// <summary>
        /// Creates a filter for the key.
        /// </summary>
        /// <param name="timestamp">The specific timestamp to find.</param>
        /// <param name="pointId">The specific point ID to find.</param>
        public SeekToKey(ulong timestamp, ulong pointId) : this()
        {
            m_keyToFind.Timestamp = timestamp;
            m_keyToFind.PointId = pointId;
            m_keyToFind.CopyTo(StartOfRange);
            m_keyToFind.CopyTo(EndOfRange);
        }

        #endregion

        #region [ Properties ]

        public override Guid FilterType => TimestampPointIdSeekFilterDefinition.FilterGuid;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Gets the next search window.
        /// </summary>
        /// <returns><c>true</c> if window exists, <c>false</c> if finished.</returns>
        public override bool NextWindow()
        {
            if (m_isEndReached)
                return false;

            m_isEndReached = true;

            return true;
        }

        /// <summary>
        /// Resets the iterative nature of the filter.
        /// </summary>
        public override void Reset()
        {
            m_isEndReached = false;
        }

        /// <summary>
        /// Serializes the filter to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public override void Save(BinaryStreamBase stream)
        {
            stream.Write(m_keyToFind.Timestamp);
            stream.Write(m_keyToFind.PointId);
        }

        #endregion
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a filter for the specified timestamp and point ID.
    /// </summary>
    /// <param name="timestamp">The specific timestamp to find.</param>
    /// <param name="pointId">The specific point ID to find.</param>
    /// <returns>Seek filter to find specific key.</returns>
    public static SeekFilterBase<TKey> FindKey<TKey>(ulong timestamp, ulong pointId) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new SeekToKey<TKey>(timestamp, pointId);
    }

    /// <summary>
    /// Loads a <see cref="SeekFilterBase{TKey}"/> from the provided <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream to load the filter from.</param>
    /// <returns>Seek filter to find specific key.</returns>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static SeekFilterBase<TKey> CreateFromStream<TKey>(BinaryStreamBase stream) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new SeekToKey<TKey>(stream);
    }

    #endregion
}