//******************************************************************************************************
//  PointIDMatchFilter_UlongHashSet.cs - Gbtc
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

using SnapDB.IO;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Filters;

public partial class PointIdMatchFilter
{
    /// <summary>
    /// A filter that uses a <see cref="BitArray"/> to set <c>true</c> and <c>false</c> values.
    /// </summary>
    private class ULongHashSet<TKey, TValue>
        : MatchFilterBase<TKey, TValue>
        where TKey : TimestampPointIDBase<TKey>, new()
    {
        private readonly ulong m_maxValue;
        private readonly HashSet<ulong> m_points;


        /// <summary>
        /// Creates a new filter backed by a <see cref="BitArray"/>.
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <param name="pointCount">The number of points in the stream.</param>
        /// <param name="maxValue">The maximum value stored in the bit array. Cannot be larger than <c>int.MaxValue-1</c>.</param>
        public ULongHashSet(BinaryStreamBase stream, int pointCount, ulong maxValue)
        {
            m_maxValue = maxValue;
            m_points = new HashSet<ulong>();

            while (pointCount > 0)
            {
                m_points.Add(stream.ReadUInt64());
                pointCount--;
            }
        }

        /// <summary>
        /// Creates a bit array filter from <see cref="points"/>.
        /// </summary>
        /// <param name="points">The points to use.</param>
        /// <param name="maxValue">The maximum value stored in the bit array. Cannot be larger than <c>int.MaxValue-1</c>.</param>
        public ULongHashSet(IEnumerable<ulong> points, ulong maxValue)
        {
            m_maxValue = maxValue;
            m_points = new HashSet<ulong>(points);
        }

        public override Guid FilterType => PointIdMatchFilterDefinition.FilterGuid;

        public override void Save(BinaryStreamBase stream)
        {
            stream.Write((byte)2); // Stored as array of ULong[]
            stream.Write(m_maxValue);
            stream.Write(m_points.Count);

            foreach (ulong x in m_points)
                stream.Write(x);
        }

        public override bool Contains(TKey key, TValue value)
        {
            return m_points.Contains(key.PointID);
        }

    }
}
