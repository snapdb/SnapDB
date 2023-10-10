//******************************************************************************************************
//  IntegratedSecurityUserCredential.cs - Gbtc
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
//  08/29/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.Identity;
using Gemstone.IO.StreamExtensions;

namespace SnapDB.Security.Authentication;

/// <summary>
/// An individual server side user credential
/// </summary>
public class IntegratedSecurityUserCredential
{
    #region [ Members ]

    /// <summary>
    /// The security identifier for the username
    /// </summary>
    public string UserId;

    /// <summary>
    /// The username that was passed to the constructor.
    /// </summary>
    public string Username;

    /// <summary>
    /// The token associated with this user and their permissions.
    /// </summary>
    public Guid UserToken;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates user credentials.
    /// </summary>
    /// <param name="username">The created name for the user.</param>
    /// <param name="userToken">The generated token to be associated with the user's chosen name.</param>
    public IntegratedSecurityUserCredential(string username, Guid userToken)
    {
        Username = username;
    #if SQLCLR
        SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        UserID = sid.ToString();
    #else
        UserId = UserInfo.UserNameToSID(username);
    #endif
        UserToken = userToken;
    }

    /// <summary>
    /// Loads user credentials from the supplied stream.
    /// </summary>
    /// <param name="stream">The binary stream from which the state will be loaded.</param>

    public IntegratedSecurityUserCredential(Stream stream)
    {
        Load(stream);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves to the supplied stream.
    /// </summary>
    /// <param name="stream">The binary stream to which the state will be saved.</param>
    public void Save(Stream stream)
    {
        stream.WriteByte(1);
        stream.Write(Username);
        stream.Write(UserId);
        stream.Write(UserToken);
    }

    /// <summary>
    /// Loads from the supplied stream.
    /// </summary>
    /// <param name="stream">The binary stream from which the state will be loaded.</param>
    public void Load(Stream stream)
    {
        byte version = stream.ReadNextByte();

        if (version != 1)
            throw new VersionNotFoundException("Unknown encoding method");
        
        Username = stream.ReadString();
        UserId = stream.ReadString();
        UserToken = stream.ReadGuid();
    }

    #endregion
}