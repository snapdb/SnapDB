//******************************************************************************************************
//  MatchFilterUniverse`2.cs - Gbtc
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
/// Represents a match filter that matches any key-value pair (universe filter).
/// </summary>
/// <typeparam name="TKey">The type of keys to be matched.</typeparam>
/// <typeparam name="TValue">The type of values to be matched.</typeparam>
public class MatchFilterUniverse<TKey, TValue> : MatchFilterBase<TKey, TValue>
{
    #region [ Properties ]

    /// <summary>
    /// Gets the unique identifier for this match filter, which is always Guid.Empty for the universe filter.
    /// </summary>
    public override Guid FilterType => Guid.Empty;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the universe filter to a binary stream (not supported).
    /// </summary>
    /// <param name="stream">The binary stream to which the filter should be saved.</param>
    /// <exception cref="NotSupportedException">Thrown when saving the universe filter is not supported.</exception>
    public override void Save(BinaryStreamBase stream)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Determines whether the universe filter contains any key-value pair.
    /// </summary>
    /// <param name="key">The key to be checked.</param>
    /// <param name="value">The value to be checked.</param>
    /// <returns>Always returns true, indicating that the universe filter matches any key-value pair.</returns>
    public override bool Contains(TKey key, TValue value)
    {
        return true;
    }

    #endregion
}