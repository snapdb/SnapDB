﻿//******************************************************************************************************
//  SrpServer.cs - Gbtc
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
//  07/27/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Text;
using Gemstone.IO.StreamExtensions;

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
public class SrpServer
{
    #region [ Members ]

    /// <summary>
    /// Contains the user credentials database
    /// </summary>
    public readonly SrpUserCredentials Users;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// </summary>
    public SrpServer()
    {
        Users = new SrpUserCredentials();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Requests that the provided stream be authenticated.
    /// </summary>
    /// <param name="stream">The input stream used for communication.</param>
    /// <param name="additionalChallenge">
    /// Additional data to include in the challenge. If using SSL certificates,
    /// adding the thumbprint to the challenge will allow detecting man in the middle attacks.
    /// </param>
    /// <returns>A <see cref="SrpServerSession"/> representing the authenticated server session, or null if authentication fails.</returns>
    public SrpServerSession? AuthenticateAsServer(Stream stream, byte[]? additionalChallenge = null)
    {
        additionalChallenge ??= [];

        // Header
        //  C => S
        //  int16   usernameLength (max 1024 characters)
        //  byte[]  usernameBytes

        int len = stream.ReadInt16();

        if (len is < 0 or > 1024)
            return null;

        byte[] usernameBytes = stream.ReadBytes(len);
        string username = s_utf8.GetString(usernameBytes);
        SrpUserCredential user = Users.Lookup(username);
        SrpServerSession session = new(user);

        return session.TryAuthenticate(stream, additionalChallenge) ? session : null;
    }

    #endregion

    #region [ Static ]

    private static readonly UTF8Encoding s_utf8 = new(true);

    #endregion
}