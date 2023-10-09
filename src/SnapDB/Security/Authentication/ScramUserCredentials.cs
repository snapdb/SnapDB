//******************************************************************************************************
//  ScramUserCredential.cs - Gbtc
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
//  08/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses SCRAM.
/// </summary>
/// <remarks>
/// It is safe to store the user's credential on the server. This is a zero knowledge
/// password proof, meaning if this database is compromised, a brute force attack
/// is the only way to reveal the password.
/// </remarks>
public class ScramUserCredentials
{
    #region [ Members ]

    private readonly Dictionary<ReadonlyByteArray, ScramUserCredential> m_users = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Attempts to look up a user credential by their username.
    /// </summary>
    /// <param name="username">The username of the user credential to look up.</param>
    /// <param name="user">When this method returns, contains the user credential if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the user credential was found; otherwise, <c>false</c>.</returns>
    public bool TryLookup(byte[] username, out ScramUserCredential? user)
    {
        lock (m_users)
            return m_users.TryGetValue(new ReadonlyByteArray(username), out user);
    }

    /// <summary>
    /// Adds a new user credential with the specified username and password.
    /// </summary>
    /// <param name="username">The username of the new user credential.</param>
    /// <param name="password">The password associated with the new user credential.</param>
    /// <param name="iterations">The number of iterations for key derivation (optional, default is 4000).</param>
    /// <param name="saltSize">The size of the salt in bytes for key derivation (optional, default is 32).</param>
    /// <param name="hashMethod">The hash method used for key derivation (optional, default is SHA-256).</param>
    public void AddUser(string username, string password, int iterations = 4000, int saltSize = 32, HashMethod hashMethod = HashMethod.Sha256)
    {
        ScramUserCredential user = new(username, password, iterations, saltSize, hashMethod);

        lock (m_users)
            m_users.Add(user.UserBytes, user);
    }

    #endregion
}