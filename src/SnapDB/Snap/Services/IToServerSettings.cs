﻿//******************************************************************************************************
//  IToServerSettings.cs - Gbtc
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
//  10/05/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap.Services;

/// <summary>
/// Allows the creation of <see cref="ServerSettings"/> from a class that implements this method.
/// </summary>
public interface IToServerSettings
{
    #region [ Methods ]

    /// <summary>
    /// Creates a <see cref="ServerSettings"/> configuration that can be used for <see cref="SnapServer"/>
    /// </summary>
    /// <returns>A <see cref="ServerSettings"/> object that can be used for configuring a <see cref="SnapServer"/>.</returns>
    /// <remarks>
    /// This method is used to convert the current configuration settings into a <see cref="ServerSettings"/> object
    /// that can be applied when configuring a <see cref="SnapServer"/>.
    /// </remarks>
    ServerSettings ToServerSettings();

    #endregion
}