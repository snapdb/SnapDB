﻿//******************************************************************************************************
//  PBDKFCredentials.cs - Gbtc
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
//  08/26/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Text;

namespace SnapDB.Security;

/// <summary>
/// Computes the password credentials.
/// Optimized so duplicate calls will not recompute the password unless necessary.
/// </summary>
internal class PbdkfCredentials
{
    #region [ Members ]

    /// <summary>
    /// The password value
    /// </summary>
    public byte[] SaltedPassword;

    // Original username and password
    /// <summary>
    /// The UTF8 encoded normalized username.
    /// </summary>
    public byte[] UsernameBytes;

    // Salted password, base on PBKDF2
    private HashMethod m_hashMethod;
    private int m_iterations;

    private readonly byte[] m_passwordBytes;
    private byte[] m_salt;

    #endregion

    #region [ Constructors ]

    public PbdkfCredentials(string username, string password)
    {
        UsernameBytes = Encoding.UTF8.GetBytes(username.Normalize(NormalizationForm.FormKC));
        m_passwordBytes = Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormKC));
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Updates the <see cref="SaltedPassword"/>. Returns False if the password value did not change.
    /// </summary>
    /// <param name="hashMethod"></param>
    /// <param name="salt"></param>
    /// <param name="iterations"></param>
    /// <returns>Returns <c>false</c> if the password value did not change.</returns>
    public bool TryUpdate(HashMethod hashMethod, byte[] salt, int iterations)
    {
        bool hasChanged = false;

        if (m_salt is null || !salt.SecureEquals(m_salt))
        {
            hasChanged = true;
            m_salt = salt;
        }

        if (m_hashMethod != hashMethod)
        {
            hasChanged = true;
            m_hashMethod = hashMethod;
        }

        if (iterations != m_iterations)
        {
            hasChanged = true;
            m_iterations = iterations;
        }

        if (hasChanged)
        {
            SaltedPassword = hashMethod switch
            {
                HashMethod.Sha1 => Pbkdf2.ComputeSaltedPassword(HmacMethod.Sha1, m_passwordBytes, m_salt, m_iterations, 20),
                HashMethod.Sha256 => Pbkdf2.ComputeSaltedPassword(HmacMethod.Sha256, m_passwordBytes, m_salt, m_iterations, 32),
                HashMethod.Sha384 => Pbkdf2.ComputeSaltedPassword(HmacMethod.Sha384, m_passwordBytes, m_salt, m_iterations, 48),
                HashMethod.Sha512 => Pbkdf2.ComputeSaltedPassword(HmacMethod.Sha512, m_passwordBytes, m_salt, m_iterations, 64),
                _ => throw new Exception("Invalid Hash Method")
            };

            return true;
        }

        return false;
    }

    #endregion
}