﻿//******************************************************************************************************
//  IntegratedSecurityUserCredentials.cs - Gbtc
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
using System.Security.Principal;
using Gemstone.IO.StreamExtensions;

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
/// <remarks>
/// It is safe to store the user's credential on the server. This is a zero knowledge
/// password proof, meaning if this database is compromised, a brute force attack
/// is the only way to reveal the password.
/// </remarks>
public class IntegratedSecurityUserCredentials
{
    #region [ Members ]

    private readonly Dictionary<string, IntegratedSecurityUserCredential> m_users = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Gets if the user exists in the database
    /// </summary>
    /// <param name="identity">the identity to check</param>
    /// <param name="token">The token to extract for the user.</param>
    /// <returns>true if the user exists</returns>
    public bool TryGetToken(IIdentity identity, out Guid token)
    {
        token = Guid.Empty;

        WindowsIdentity i = identity as WindowsIdentity;
        if (i?.User is null)
            return false;
        lock (m_users)
        {
            if (m_users.TryGetValue(i.User.Value, out IntegratedSecurityUserCredential user))
            {
                token = user.UserToken;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Adds the specified user to the credentials database.
    /// </summary>
    /// <param name="username"></param>
    public void AddUser(string username)
    {
        AddUser(username, Guid.NewGuid());
    }

    /// <summary>
    /// Adds the specified user to the credentials database.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="userToken"></param>
    public void AddUser(string username, Guid userToken)
    {
        IntegratedSecurityUserCredential user = new(username, userToken);
        lock (m_users)
        {
            m_users.Add(user.UserId, user);
        }
    }

    /// <summary>
    /// Saves to the supplied stream.
    /// </summary>
    /// <param name="stream"></param>
    public void Save(Stream stream)
    {
        stream.WriteByte(1);
        lock (m_users)
        {
            stream.Write(m_users.Count);
            foreach (IntegratedSecurityUserCredential user in m_users.Values)
                user.Save(stream);
        }
    }

    /// <summary>
    /// Loads from the supplied stream.
    /// </summary>
    /// <param name="stream"></param>
    public void Load(Stream stream)
    {
        byte version = stream.ReadNextByte();
        switch (version)
        {
            case 1:
                lock (m_users)
                {
                    int count = stream.ReadInt32();
                    m_users.Clear();
                    while (count > 0)
                    {
                        count--;
                        IntegratedSecurityUserCredential user = new(stream);
                        m_users.Add(user.UserId, user);
                    }
                }

                return;
            default:
                throw new VersionNotFoundException("Unknown encoding method");
        }
    }

    #endregion
}