//******************************************************************************************************
//  TimestampSeekFilter.cs - Gbtc
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

using System.Data;
using System.Runtime.CompilerServices;
using SnapDB.IO;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Filters;

public partial class TimestampSeekFilter
{
    #region [ Static ]

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the seek filter.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <returns>A <see cref="SeekFilterBase{TKey}"/> that filters keys within the specified time range.</returns>
    /// <remarks>
    /// The seek filter includes keys with timestamps greater than or equal to <paramref name="firstTime"/> and
    /// less than or equal to <paramref name="lastTime"/>. It effectively filters keys within the time range.
    /// </remarks>
    public static SeekFilterBase<TKey> CreateFromRange<TKey>(DateTime firstTime, DateTime lastTime) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new FixedRange<TKey>((ulong)firstTime.Ticks, (ulong)lastTime.Ticks);
    }

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <returns>The fixed range for the seek filter to search during.</returns>
    public static SeekFilterBase<TKey> CreateFromRange<TKey>(ulong firstTime, ulong lastTime) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new FixedRange<TKey>(firstTime, lastTime);
    }

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <param name="mainInterval">The smallest interval that is exact.</param>
    /// <param name="subInterval">The interval that will be parsed. Possible to be rounded.</param>
    /// <param name="tolerance">The width of every window.</param>
    /// <returns>A key seek filter base that will be able to do this parsing.</returns>
    /// <remarks>
    /// Example uses. FirstTime = 1/1/2013. LastTime = 1/2/2013.
    /// MainInterval = 0.1 seconds. SubInterval = 0.0333333 seconds.
    /// Tolerance = 0.001 seconds.
    /// </remarks>
    public static SeekFilterBase<TKey> CreateFromIntervalData<TKey>(ulong firstTime, ulong lastTime, ulong mainInterval, ulong subInterval, ulong tolerance) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new IntervalRanges<TKey>(firstTime, lastTime, mainInterval, subInterval, tolerance);
    }

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <param name="interval">The exact interval.</param>
    /// <param name="tolerance">The width of every window</param>
    /// <returns>A key seek filter base that will be able to do this parsing.</returns>
    /// <remarks>
    /// Example uses. FirstTime = 1/1/2013. LastTime = 1/2/2013.
    /// MainInterval = 0.1 seconds. SubInterval = 0.0333333 seconds.
    /// Tolerance = 0.001 seconds.
    /// </remarks>
    public static SeekFilterBase<TKey> CreateFromIntervalData<TKey>(ulong firstTime, ulong lastTime, ulong interval, ulong tolerance) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new IntervalRanges<TKey>(firstTime, lastTime, interval, interval, tolerance);
    }

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <param name="mainInterval">The smallest interval that is exact.</param>
    /// <param name="subInterval">The interval that will be parsed. Possible to be rounded.</param>
    /// <param name="tolerance">The width of every window.</param>
    /// <returns>A key seek filter base that will be able to do this parsing.</returns>
    /// <remarks>
    /// Example uses. FirstTime = 1/1/2013. LastTime = 1/2/2013.
    /// MainInterval = 0.1 seconds. SubInterval = 0.0333333 seconds.
    /// Tolerance = 0.001 seconds.
    /// </remarks>
    public static SeekFilterBase<TKey> CreateFromIntervalData<TKey>(DateTime firstTime, DateTime lastTime, TimeSpan mainInterval, TimeSpan subInterval, TimeSpan tolerance) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new IntervalRanges<TKey>((ulong)firstTime.Ticks, (ulong)lastTime.Ticks, (ulong)mainInterval.Ticks, (ulong)subInterval.Ticks, (ulong)tolerance.Ticks);
    }

    /// <summary>
    /// Creates a seek filter that filters keys falling within a specified time range.
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="firstTime">The starting timestamp of the time range query (inclusive).</param>
    /// <param name="lastTime">The ending timestamp of the time range query (inclusive).</param>
    /// <param name="interval">The exact interval to do the scan.</param>
    /// <param name="tolerance">The width of every window.</param>
    /// <returns>A key seek filter base that will be able to do this parsing.</returns>
    /// <remarks>
    /// Example uses. FirstTime = 1/1/2013. LastTime = 1/2/2013.
    /// Interval = 0.1 seconds.
    /// Tolerance = 0.001 seconds.
    /// </remarks>
    public static SeekFilterBase<TKey> CreateFromIntervalData<TKey>(DateTime firstTime, DateTime lastTime, TimeSpan interval, TimeSpan tolerance) where TKey : TimestampPointIdBase<TKey>, new()
    {
        return new IntervalRanges<TKey>((ulong)firstTime.Ticks, (ulong)lastTime.Ticks, (ulong)interval.Ticks, (ulong)interval.Ticks, (ulong)tolerance.Ticks);
    }

    /// <summary>
    /// Loads a key seek filter base from the provided <paramref name="stream"/>;
    /// </summary>
    /// <typeparam name="TKey">The type of key that implements SeekFilterBase.</typeparam>
    /// <param name="stream">The stream to load the filter from.</param>
    /// <returns>A seek filter base that will be created from the specified stream.</returns>
    /// <exception cref="VersionNotFoundException">Thrown if the version cannot be reached.</exception>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    private static SeekFilterBase<TKey> CreateFromStream<TKey>(BinaryStreamBase stream) where TKey : TimestampPointIdBase<TKey>, new()
    {
        byte version = stream.ReadUInt8();

        return version 
            switch
        {
            0 => null,
            1 => new FixedRange<TKey>(stream),
            2 => new IntervalRanges<TKey>(stream),
            _ => throw new VersionNotFoundException("Unknown Version")
        };
    }

    #endregion
}