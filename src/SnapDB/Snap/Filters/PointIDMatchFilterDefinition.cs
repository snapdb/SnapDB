//******************************************************************************************************
//  PointIDMatchFilterDefinition.cs - Gbtc
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

using System.Reflection;
using SnapDB.IO;
using SnapDB.Snap.Definitions;

namespace SnapDB.Snap.Filters;

/// <summary>
/// Defines a filter for matching data based on point IDs using a bit array to set <c>true</c> and <c>false</c> values.
/// </summary>
public class PointIDMatchFilterDefinition : MatchFilterDefinitionBase
{
    #region [ Properties ]

    /// <summary>
    /// Gets the unique identifier for this match filter type.
    /// </summary>
    public override Guid FilterType => FilterGuid;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a match filter from a binary stream.
    /// </summary>
    /// <typeparam name="TKey">The key type of the match filter.</typeparam>
    /// <typeparam name="TValue">The value type of the match filter.</typeparam>
    /// <param name="stream">The binary stream containing match filter data.</param>
    /// <returns>A match filter created from the binary stream data.</returns>
    public override MatchFilterBase<TKey, TValue> Create<TKey, TValue>(BinaryStreamBase stream)
    {
        MethodInfo? method = typeof(PointIDMatchFilter).GetMethod("CreateFromStream", BindingFlags.NonPublic | BindingFlags.Static);

        if(method != null)
        {
            MethodInfo generic = method.MakeGenericMethod(typeof(TKey), typeof(TValue));
            object? rv = generic.Invoke(null, new[] { stream });

            if(rv != null)
                return (MatchFilterBase<TKey, TValue>)rv;
        }

        throw new InvalidOperationException("Failed to create the MatchFilter");
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// The globally unique identifier (GUID) for the PointIDMatchFilterDefinition.
    /// </summary>
    // {2034A3E3-F92E-4749-9306-B04DC36FD743}
    public static Guid FilterGuid = new(0x2034a3e3, 0xf92e, 0x4749, 0x93, 0x06, 0xb0, 0x4d, 0xc3, 0x6f, 0xd7, 0x43);

    #endregion
}