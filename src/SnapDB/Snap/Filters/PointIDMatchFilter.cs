//******************************************************************************************************
//  PointIDMatchFilter.cs - Gbtc
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
//  11/25/2014 - J. Ritchie Carroll
//       Added single point ID matching filter.   
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************


using System.Data;
using System.Runtime.CompilerServices;
using SnapDB.IO;
using SnapDB.Snap.Types;
using static SnapDB.Snap.Filters.PointIDMatchFilterBitArray;

namespace SnapDB.Snap.Filters;

/// <summary>
/// Partial class for creating and managing match filters based on point IDs.
/// </summary>
public partial class PointIDMatchFilter
{
    #region [ Static ]

    /// <summary>
    /// Creates a filter from the provided <paramref name="pointId"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type of the match filter.</typeparam>
    /// <typeparam name="TValue">The value type of the match filter.</typeparam>
    /// <param name="pointId">The point ID to create the filter for.</param>
    /// <returns>A match filter based on the specified point ID.</returns>
    public static MatchFilterBase<TKey, TValue> CreateFromPointID<TKey, TValue>(ulong pointId) where TKey : TimestampPointIDBase<TKey>, new()
    {
        return pointId switch
        {
            // 64KB of space, 524288
            < 8 * 1024 * 64 => new BitArrayFilter<TKey, TValue>(new[] { pointId }, pointId),
            <= uint.MaxValue => new UIntHashSet<TKey, TValue>(new[] { pointId }, pointId),
            _ => new ULongHashSet<TKey, TValue>(new[] { pointId }, pointId)
        };
    }

    /// <summary>
    /// Creates a match filter that filters keys based on a list of point IDs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the match filter.</typeparam>
    /// <typeparam name="TValue">The type of values in the match filter.</typeparam>
    /// <param name="listOfPointIDs">An enumerable collection of point IDs to filter by.</param>
    /// <returns>A <see cref="MatchFilterBase{TKey, TValue}"/> that filters keys based on the specified list of point IDs.</returns>
    /// <remarks>
    /// The match filter includes keys whose point IDs match any of the point IDs in the <paramref name="listOfPointIDs"/> collection.
    /// The appropriate filter type is chosen based on the maximum point ID value in the list.
    /// </remarks>
    public static MatchFilterBase<TKey, TValue> CreateFromList<TKey, TValue>(IEnumerable<ulong> listOfPointIDs) where TKey : TimestampPointIDBase<TKey>, new()
    {
        ulong maxValue = 0;
        ulong[] pointIDs = listOfPointIDs as ulong[] ?? listOfPointIDs.ToArray();

        if (pointIDs.Any())
            maxValue = pointIDs.Max();

        return maxValue switch
        {
            // 64KB of space, 524288
            < 8 * 1024 * 64 => new BitArrayFilter<TKey, TValue>(pointIDs, maxValue),
            <= uint.MaxValue => new UIntHashSet<TKey, TValue>(pointIDs, maxValue),
            _ => new ULongHashSet<TKey, TValue>(pointIDs, maxValue)
        };
    }

    /// <summary>
    /// Creates a match filter from a binary stream.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the match filter.</typeparam>
    /// <typeparam name="TValue">The type of values in the match filter.</typeparam>
    /// <param name="stream">The binary stream containing match filter data.</param>
    /// <returns>
    /// A <see cref="MatchFilterBase{TKey, TValue}"/> created from the data in the specified <paramref name="stream"/>,
    /// or <c>null</c> if the stream contains no filter data.
    /// </returns>
    /// <exception cref="VersionNotFoundException">Thrown if the binary stream contains data with an unknown version.</exception>
    /// <remarks>
    /// The match filter is deserialized from the binary stream based on its version and data format.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static MatchFilterBase<TKey, TValue>? CreateFromStream<TKey, TValue>(BinaryStreamBase stream) where TKey : TimestampPointIDBase<TKey>, new()
    {
        MatchFilterBase<TKey, TValue> filter;
        byte version = stream.ReadUInt8();
        ulong maxValue;
        int count;

        switch (version)
        {
            case 0:
                return null;

            case 1:
                maxValue = stream.ReadUInt64();
                count = stream.ReadInt32();

                if (maxValue < 8 * 1024 * 64) // 64KB of space, 524288
                    filter = new BitArrayFilter<TKey, TValue>(stream, count, maxValue);
                else
                    filter = new UIntHashSet<TKey, TValue>(stream, count, maxValue);

                break;

            case 2:
                maxValue = stream.ReadUInt64();
                count = stream.ReadInt32();
                filter = new ULongHashSet<TKey, TValue>(stream, count, maxValue);

                break;

            default:
                throw new VersionNotFoundException("Unknown Version");
        }

        return filter;
    }

    #endregion
}