﻿//******************************************************************************************************
//  MatchFilterBase`2.cs - Gbtc
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

namespace SnapDB.Snap.Filters;

/// <summary>
/// Represents some kind of filter that does a match based on the key/value.
/// </summary>
/// <typeparam name="TKey">The key to match.</typeparam>
/// <typeparam name="TValue">The value to match.</typeparam>
public abstract class MatchFilterBase<TKey, TValue>
{
    #region [ Properties ]

    /// <summary>
    /// The filter GUID.
    /// </summary>
    public abstract Guid FilterType { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Serializes the filter to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public abstract void Save(BinaryStreamBase stream);

    /// <summary>
    /// Determines if a key-value is contained in the filter.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the key-value is contained in the filter; otherwise, false.</returns>
    public abstract bool Contains(TKey key, TValue value);

    #endregion
}