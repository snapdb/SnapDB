﻿//******************************************************************************************************
//  IndividualEncodingDefinitionBase.cs - Gbtc
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
//  02/21/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Encoding;

namespace SnapDB.Snap.Definitions;

/// <summary>
/// The class that is used to construct an encoding method.
/// </summary>
public abstract class IndividualEncodingDefinitionBase
{
    #region [ Properties ]

    /// <summary>
    /// The encoding method as specified by a <see cref="Guid"/>.
    /// </summary>
    public abstract Guid Method { get; }

    /// <summary>
    /// The type supported by the encoded method. Can be <c>null</c> if the encoding is not type specific.
    /// </summary>
    public abstract Type TypeIfNotGeneric { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Constructs a new class based on this encoding method.
    /// </summary>
    /// <typeparam name="T">The type of this base class.</typeparam>
    /// <returns>
    /// The encoding method.
    /// </returns>
    public abstract IndividualEncodingBase<T> Create<T>() where T : SnapTypeBase<T>, new();

    #endregion
}