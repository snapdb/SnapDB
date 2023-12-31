﻿//******************************************************************************************************
//  CertificateUserCredentials.cs - Gbtc
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
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Security.Principal;

namespace SnapDB.Security.Authentication;

#pragma warning disable CA1416

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
/// <remarks>
/// It is safe to store the user's credential on the server. This is a zero knowledge
/// password proof, meaning if this database is compromised, a brute force attack
/// is the only way to reveal the password.
/// </remarks>
public class CertificateUserCredentials
{
    #region [ Members ]

    private readonly Dictionary<string, CertificateUserCredential> m_users = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Looks up the username from the database.
    /// </summary>
    /// <param name="username">The username to look up.</param>
    /// <returns>The <see cref="CertificateUserCredential"/> associated with the specified username.</returns>
    public CertificateUserCredential Lookup(string username)
    {
        lock (m_users)
        {
            return m_users[username];
        }
    }

    /// <summary>
    /// Gets if the user exists in the database
    /// </summary>
    /// <param name="identity">The <see cref="IIdentity"/> to check for existence.</param>
    /// <returns>
    /// <c>true</c> if a user with the specified <see cref="IIdentity"/> exists; otherwise, <c>false</c>.
    /// </returns>
    public bool Exists(IIdentity identity)
    {
        if (identity is not WindowsIdentity windowsIdentity)
            return false;

        lock (m_users)
        {
            return m_users.ContainsKey(windowsIdentity.User?.Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Adds the specified user to the credentials database.
    /// </summary>
    /// <param name="username">The username of the user to add.</param>
    public void AddUser(string username)
    {
        CertificateUserCredential user = new(username);

        lock (m_users)
        {
            m_users.Add(user.UserId, user);
        }
    }

    #endregion
}