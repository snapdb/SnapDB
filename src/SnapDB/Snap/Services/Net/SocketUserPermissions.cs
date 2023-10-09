//******************************************************************************************************
//  SocketUserPermissions.cs - Gbtc
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
//  09/04/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.IO.StreamExtensions;
using SnapDB.Security.Authentication;

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// Permissions associated with an individual user.
/// </summary>
public struct SocketUserPermissions : IUserToken
{
    #region [ Members ]

    /// <summary>
    /// Gets if the user can perform read operations
    /// </summary>
    public bool CanRead;

    /// <summary>
    /// Gets if the user can perform write operations
    /// </summary>
    public bool CanWrite;

    /// <summary>
    /// Gets if the user can perform admin operations
    /// </summary>
    /// <remarks>
    /// Admin operations would include
    /// Detatching/Deleting/Moving
    /// archive file.
    /// </remarks>
    public bool IsAdmin;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the token to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    public void Save(Stream stream)
    {
        stream.WriteByte(1);
        stream.Write(CanWrite);
        stream.Write(CanRead);
        stream.Write(IsAdmin);
    }

    /// <summary>
    /// Loads the token from a stream
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    public void Load(Stream stream)
    {
        byte version = stream.ReadNextByte();
        switch (version)
        {
            case 1:
                CanWrite = stream.ReadBoolean();
                CanRead = stream.ReadBoolean();
                IsAdmin = stream.ReadBoolean();
                break;

            default:
                throw new VersionNotFoundException("Could not interpret version");
        }
    }

    #endregion
}