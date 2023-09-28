//******************************************************************************************************
//  ClientDatabaseBase`2.cs - Gbtc
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
//  12/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Filters;
using SnapDB.Snap.Services.Reader;

namespace SnapDB.Snap.Services;

/// <summary>
/// Represents a single historian database.
/// </summary>
public abstract class ClientDatabaseBase<TKey, TValue>
    : ClientDatabaseBase, IDatabaseReader<TKey, TValue>
    where TKey : SnapTypeBaseOfT<TKey>, new()
    where TValue : SnapTypeBaseOfT<TValue>, new()
{
    /// <summary>
    /// Reads data from the SortedTreeEngine with the provided read options and server side filters.
    /// </summary>
    /// <param name="readerOptions">Read options supplied to the reader. Can be <c>null</c>.</param>
    /// <param name="keySeekFilter">A seek based filter to follow. Can be <c>null</c>.</param>
    /// <param name="keyMatchFilter">A match based filter to follow. Can be <c>null</c>.</param>
    /// <returns>A stream that will read the specified data.</returns>
    public abstract TreeStream<TKey, TValue> Read(SortedTreeEngineReaderOptions readerOptions, SeekFilterBase<TKey> keySeekFilter, MatchFilterBase<TKey, TValue> keyMatchFilter);

    /// <summary>
    /// Writes the tree stream to the database. 
    /// </summary>
    /// <param name="stream">All of the key-value pairs to add to the database.</param>
    public abstract void Write(TreeStream<TKey, TValue> stream);

    /// <summary>
    /// Writes an individual key/value to the sorted tree store.
    /// </summary>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value associated with the key to write.</param>
    public abstract void Write(TKey key, TValue value);

}