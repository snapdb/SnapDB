//******************************************************************************************************
//  SrpUserCredentials.cs - Gbtc
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

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
/// <remarks>
/// It is safe to store the user's credential on the server. This is a zero knowledge
/// password proof, meaning if this database is compromised, a brute force attack
/// is the only way to reveal the password.
/// </remarks>
public class SrpUserCredentials
{
    #region [ Members ]

    private readonly Dictionary<string, SrpUserCredential> m_users = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Looks up the username from the database.
    /// </summary>
    /// <param name="username">The username to look up.</param>
    /// <returns>
    /// The <see cref="SrpUserCredential"/> object associated with the specified username, or <c>null</c> if not found.
    /// </returns>
    public SrpUserCredential Lookup(string username)
    {
        lock (m_users)
            return m_users[username];
    }

    /// <summary>
    /// Adds the specified user to the credentials database.
    /// </summary>
    /// <param name="username">The user's chosen name to add to the database.</param>
    /// <param name="password">The user's chosen password to add to the database.</param>
    /// <param name="strength">The strength rating of the password.</param>
    /// <param name="saltSize">The size of salt to be used for password hashing (default is 32).</param>
    /// <param name="iterations">How many iterations used for password hashing (default is 4000).</param>
    public void AddUser(string username, string password, SrpStrength strength = SrpStrength.Bits1024, int saltSize = 32, int iterations = 4000)
    {
        SrpUserCredential user = new(username, password, strength, saltSize, iterations);

        lock (m_users)
            m_users.Add(username, user);
    }

    /// <summary>
    /// Adds the specified user to the credential database.
    /// </summary>
    /// <param name="username">The username of the new user.</param>
    /// <param name="verifier">The SRP verifier value associated with the user.</param>
    /// <param name="passwordSalt">The salt value used for password hashing.</param>
    /// <param name="iterations">The number of iterations used for password hashing.</param>
    /// <param name="strength">The strength of the SRP protocol.</param>
    public void AddUser(string username, byte[] verifier, byte[] passwordSalt, int iterations, SrpStrength strength)
    {
        SrpUserCredential user = new(username, passwordSalt, verifier, iterations, strength);

        lock (m_users)
            m_users.Add(username, user);
    }

    #endregion
}