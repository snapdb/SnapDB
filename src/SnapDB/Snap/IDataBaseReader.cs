//******************************************************************************************************
//  IDatabaseReader`2.cs - Gbtc
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
//  03/01/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Filters;
using SnapDB.Snap.Services.Reader;

namespace SnapDB.Snap;

/// <summary>
/// Represents a database reader interface for reading data from a SortedTreeEngine.
/// </summary>
/// <typeparam name="TKey">The type of keys in the database.</typeparam>
/// <typeparam name="TValue">The type of values in the database.</typeparam>
public interface IDatabaseReader<TKey, TValue> : IDisposable where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Methods ]

    /// <summary>
    /// Reads data from the SortedTreeEngine with the provided read options and server-side filters.
    /// </summary>
    /// <param name="readerOptions">Read options supplied to the reader. Can be <c>null</c>.</param>
    /// <param name="keySeekFilter">A seek-based filter to follow. Can be <c>null</c>.</param>
    /// <param name="keyMatchFilter">A match-based filter to follow. Can be <c>null</c>.</param>
    /// <returns>A stream that will read the specified data.</returns>
    TreeStream<TKey, TValue> Read(SortedTreeEngineReaderOptions? readerOptions, SeekFilterBase<TKey> keySeekFilter, MatchFilterBase<TKey, TValue>? keyMatchFilter);

    #endregion
}