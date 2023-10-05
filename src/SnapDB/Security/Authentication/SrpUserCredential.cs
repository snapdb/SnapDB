//******************************************************************************************************
//  SrpUserCredential.cs - Gbtc
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
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math;

namespace SnapDB.Security.Authentication;

/// <summary>
/// An individual server side user credential
/// </summary>
public class SrpUserCredential
{
    #region [ Members ]

    /// <summary>
    /// The number of SHA512 iterations using PBKDF2
    /// </summary>
    public readonly int Iterations;

    /// <summary>
    /// The salt used to compute the password bytes (x)
    /// </summary>
    public readonly byte[] Salt;

    /// <summary>
    /// Session Resume Encryption Key
    /// </summary>
    public readonly byte[] ServerEncryptionkey = SaltGenerator.Create(32);

    /// <summary>
    /// Session Resume HMAC key
    /// </summary>
    public readonly byte[] ServerHmacKey = SaltGenerator.Create(32);

    /// <summary>
    /// Session Resume Key Name
    /// </summary>
    public readonly Guid ServerKeyName = Guid.NewGuid();

    /// <summary>
    /// The bit strength of the SRP algorithm.
    /// </summary>
    public readonly SrpStrength SrpStrength;

    /// <summary>
    /// The normalized name of the user
    /// </summary>
    public readonly string UserName;

    public readonly byte[] UsernameBytes;

    /// <summary>
    /// The Srp server verification bytes. (Computed as g^x % N)
    /// </summary>
    public readonly byte[] Verification;

    /// <summary>
    /// <see cref="Verification"/> as a <see cref="BigInteger"/>.
    /// </summary>
    public readonly BigInteger VerificationInteger;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates user credentials
    /// </summary>
    /// <param name="username"></param>
    /// <param name="salt"></param>
    /// <param name="verification"></param>
    /// <param name="iterations"></param>
    /// <param name="srpStrength"></param>
    public SrpUserCredential(string username, byte[] verification, byte[] salt, int iterations, SrpStrength srpStrength)
    {
        UserName = username;
        UsernameBytes = Encoding.UTF8.GetBytes(username);
        Salt = salt;
        Verification = verification;
        Iterations = iterations;
        SrpStrength = srpStrength;
        VerificationInteger = new BigInteger(1, verification);
    }

    /// <summary>
    /// Creates a user credential from the provided data.
    /// </summary>
    /// <param name="username">The username for the user credential.</param>
    /// <param name="password">The password for the user credential.</param>
    /// <param name="strength">The strength of the SRP protocol (default is <see cref="SrpStrength.Bits1024"/>).</param>
    /// <param name="saltSize">The size of the salt in bytes (default is 32 bytes).</param>
    /// <param name="iterations">The number of iterations for password hashing (default is 4000).</param>
    public SrpUserCredential(string username, string password, SrpStrength strength = SrpStrength.Bits1024, int saltSize = 32, int iterations = 4000)
    {
        username = username.Normalize(NormalizationForm.FormKC);
        password = password.Normalize(NormalizationForm.FormKC);
        UsernameBytes = Encoding.UTF8.GetBytes(username);

        SrpConstants constants = SrpConstants.Lookup(strength);
        BigInteger n = constants.N;
        BigInteger g = constants.G;
        byte[] s = SaltGenerator.Create(saltSize);
        byte[] hashPassword = Pbkdf2.ComputeSaltedPassword(HmacMethod.Sha512, Encoding.UTF8.GetBytes(password), s, iterations, 64);

        Sha512Digest hash = new();
        byte[] output = new byte[hash.GetDigestSize()];
        hash.BlockUpdate(UsernameBytes, 0, UsernameBytes.Length);
        hash.Update((byte)':');
        hash.BlockUpdate(hashPassword, 0, hashPassword.Length);
        hash.DoFinal(output, 0);
        hash.BlockUpdate(s, 0, s.Length);
        hash.BlockUpdate(output, 0, output.Length);
        hash.DoFinal(output, 0);
        BigInteger x = new BigInteger(1, output).Mod(n);
        BigInteger v = g.ModPow(x, n);

        UserName = username;
        Salt = s;
        Verification = v.ToByteArray();
        Iterations = iterations;
        SrpStrength = strength;
        VerificationInteger = new BigInteger(1, Verification);
    }

    #endregion

    #region [ Methods ]

    public void Save()
    {
    }

    public void Load()
    {
    }

    #endregion
}