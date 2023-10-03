﻿//******************************************************************************************************
//  SortedTreeEngineReaderBaseExtensionMethods.cs - Gbtc
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
//  12/29/2012 - Steven E. Chisholm
//       Generated original version of source code.
//
//  11/25/2014 - J. Ritchie Carroll
//       Added single value read extension.
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Types;
using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Services.Reader;

/// <summary>
/// Provides extension methods for <see cref="IDatabaseReader{TKey, TValue}"/> to simplify reading from a sorted tree.
/// </summary>
public static class SortedTreeEngineReaderBaseExtensionMethods
{
    private static readonly SortedTreeEngineReaderOptions? s_singleValueOptions = new(maxReturnedCount: 1);

    /// <summary>
    /// Reads a single value from the sorted tree based on the provided timestamp and point ID.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the sorted tree.</typeparam>
    /// <typeparam name="TValue">The type of values in the sorted tree.</typeparam>
    /// <param name="reader">The database reader.</param>
    /// <param name="timestamp">The timestamp for the desired value.</param>
    /// <param name="pointId">The point ID for the desired value.</param>
    /// <returns>A <see cref="TreeStream{TKey, TValue}"/> containing the single value found.</returns>

    public static TreeStream<TKey, TValue> ReadSingleValue<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, ulong timestamp, ulong pointId)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(s_singleValueOptions, TimestampPointIdSeekFilter.FindKey<TKey>(timestamp, pointId), null);
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, ulong timestamp)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, TimestampSeekFilter.CreateFromRange<TKey>(timestamp, timestamp), null);
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, SeekFilterBase<TKey> timeFilter)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, timeFilter, null);
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, null, null);
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, ulong firstTime, ulong lastTime)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, TimestampSeekFilter.CreateFromRange<TKey>(firstTime, lastTime), null);
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, ulong firstTime, ulong lastTime, IEnumerable<ulong> pointIds)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, TimestampSeekFilter.CreateFromRange<TKey>(firstTime, lastTime), PointIdMatchFilter.CreateFromList<TKey, TValue>(pointIds.ToList()));
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, DateTime firstTime, DateTime lastTime, IEnumerable<ulong> pointIds)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, TimestampSeekFilter.CreateFromRange<TKey>(firstTime, lastTime), PointIdMatchFilter.CreateFromList<TKey, TValue>(pointIds.ToList()));
    }

    public static TreeStream<TKey, TValue> Read<TKey, TValue>(this IDatabaseReader<TKey, TValue> reader, SeekFilterBase<TKey> key1, IEnumerable<ulong> pointIds)
        where TKey : TimestampPointIdBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        return reader.Read(SortedTreeEngineReaderOptions.Default, key1, PointIdMatchFilter.CreateFromList<TKey, TValue>(pointIds.ToList()));
    }

}