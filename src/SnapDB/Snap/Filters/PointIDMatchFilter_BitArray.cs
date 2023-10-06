//******************************************************************************************************
//  PointIDMatchFilter_BitArray.cs - Gbtc
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
//  11/09/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//   
//******************************************************************************************************

using SnapDB.Collections;
using SnapDB.IO;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Filters;

/// <summary>
/// A class for implementing a match filter using a <see cref="BitArray"/>.
/// </summary>
public class PointIdMatchFilterBitArray
{
    #region [ Members ]

    /// <summary>
    /// A filter that uses a <see cref="BitArray"/> to set <c>true</c> and <c>false</c> values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the filter.</typeparam>
    /// <typeparam name="TValue">The type of values in the filter.</typeparam>
    public class BitArrayFilter<TKey, TValue> : MatchFilterBase<TKey, TValue> where TKey : TimestampPointIdBase<TKey>, new()
    {
        #region [ Members ]

        private readonly BitArray m_points;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new filter backed by a <see cref="BitArray"/>.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <param name="pointCount">The number of points in the stream.</param>
        /// <param name="maxValue">The maximum value stored in the bit array. Cannot be larger than <c>int.MaxValue-1</c>.</param>
        public BitArrayFilter(BinaryStreamBase stream, int pointCount, ulong maxValue)
        {
            if (maxValue >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "Cannot be larger than int.MaxValue-1");

            MaxValue = maxValue;
            m_points = new BitArray(false, (int)maxValue + 1);

            while (pointCount > 0)
            {
                // Since a bitarray cannot have more than 32bit.
                m_points.SetBit((int)stream.ReadUInt32());
                pointCount--;
            }

            foreach (int point in m_points.GetAllSetBits())
            {
                MinValue = (ulong)point;

                break;
            }
        }

        /// <summary>
        /// Creates a bit array filter from <paramref name="points"/>.
        /// </summary>
        /// <param name="points">The points to use.</param>
        /// <param name="maxValue">The maximum value stored in the bit array. Cannot be larger than <c>int.MaxValue-1</c>.</param>
        public BitArrayFilter(IEnumerable<ulong> points, ulong maxValue)
        {
            MaxValue = maxValue;
            m_points = new BitArray(false, (int)maxValue + 1);

            foreach (ulong pt in points)
                m_points.SetBit((int)pt);

            foreach (int point in m_points.GetAllSetBits())
            {
                MinValue = (ulong)point;

                break;
            }
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the unique identifier for the filter type associated with the <see cref="BitArrayFilter{TKey, TValue}"/>.
        /// </summary>
        public override Guid FilterType => PointIdMatchFilterDefinition.FilterGuid;

        /// <summary>
        /// Gets or sets the maximum value used in the bit array. Cannot be larger than <see cref="int.MaxValue"/> - 1.
        /// </summary>
        public ulong MaxValue { get; set; } = ulong.MaxValue;

        /// <summary>
        /// Gets or sets the minimum value used in the bit array.
        /// </summary>
        public ulong MinValue { get; set; } = ulong.MinValue;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Saves the filter data to the specified <see cref="BinaryStreamBase"/>.
        /// </summary>
        /// <param name="stream">The stream to which the filter data will be saved.</param>
        public override void Save(BinaryStreamBase stream)
        {
            stream.Write((byte)1); // Stored as array of uint[]
            stream.Write(MaxValue);
            stream.Write(m_points.SetCount);

            foreach (int x in m_points.GetAllSetBits())
                stream.Write((uint)x);
        }

        /// <summary>
        /// Determines whether the filter contains the specified key and value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the filter contains the specified key and value; otherwise, <c>false</c>.</returns>
        public override bool Contains(TKey key, TValue value)
        {
            int point = (int)key.PointId;

            return key.PointId <= MaxValue && m_points.GetBitUnchecked(point);
        }

        #endregion
    }

    #endregion
}