﻿//******************************************************************************************************
//  SeekFilterDefinitionBase.cs - Gbtc
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
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Definitions;

/// <summary>
/// Has the ability to create a filter based on the key and the value.
/// </summary>
public abstract class SeekFilterDefinitionBase
{
    #region [ Properties ]

    /// <summary>
    /// The filter GUID.
    /// </summary>
    public abstract Guid FilterType { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Determines if a key-value is contained in the filter.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the seek filter.</typeparam>
    /// <param name="stream">The binary stream to create the seek filter for.</param>
    /// <returns>An instance of <see cref="SeekFilterBase{TKey}"/>.</returns>
    public abstract SeekFilterBase<TKey> Create<TKey>(BinaryStreamBase stream);

    #endregion
}