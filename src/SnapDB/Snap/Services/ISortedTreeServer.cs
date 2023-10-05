//******************************************************************************************************
//  ISortedTreeServer.cs - Gbtc
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
//  04/19/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap.Services;

/// <summary>
/// Represents a sorted tree server interface that provides methods for interacting with the server.
/// </summary>
public interface ISortedTreeServer : IDisposable
{
    #region [ Methods ]

    /// <summary>
    /// Creates a client connection to the server.
    /// </summary>
    /// <returns>A <see cref="SnapClient"/> representing the client connection to the server.</returns>
    /// <remarks>
    /// This method is used to create a client connection to the server. The returned <see cref="SnapClient"/>
    /// can be used to communicate with the server and perform various operations.
    /// </remarks>
    SnapClient CreateClientHost();

    #endregion
}