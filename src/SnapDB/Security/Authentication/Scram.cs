﻿//******************************************************************************************************
//  Scram.cs - Gbtc
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
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.ComponentModel;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace SnapDB.Security.Authentication;

internal static class Scram
{
    #region [ Members ]

    internal const int PasswordSize = 64;

    #endregion

    #region [ Static ]

    internal static readonly UTF8Encoding Utf8 = new(true);
    internal static readonly byte[] StringClientKey = Utf8.GetBytes("Client Key");
    internal static readonly byte[] StringServerKey = Utf8.GetBytes("Server Key");

    internal static IDigest CreateDigest(HashMethod hashMethod)
    {
        return hashMethod switch
        {
            HashMethod.Sha1 => new Sha1Digest(),
            HashMethod.Sha256 => new Sha256Digest(),
            HashMethod.Sha384 => new Sha384Digest(),
            HashMethod.Sha512 => new Sha512Digest(),
            _ => throw new InvalidEnumArgumentException(nameof(hashMethod), (int)hashMethod, typeof(HashMethod))
        };
    }

    internal static byte[] Xor(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            throw new Exception();

        byte[] rv = new byte[a.Length];

        for (int x = 0; x < a.Length; x++)
            rv[x] = (byte)(a[x] ^ b[x]);

        return rv;
    }

    internal static byte[] ComputeAuthMessage(byte[] serverNonce, byte[] clientNonce, byte[] salt, byte[] username, int iterations, byte[] additionalChallenge)
    {
        byte[] data = new byte[serverNonce.Length + clientNonce.Length + salt.Length + username.Length + additionalChallenge.Length + 4];

        serverNonce.CopyTo(data, 0);
        clientNonce.CopyTo(data, serverNonce.Length);
        salt.CopyTo(data, serverNonce.Length + clientNonce.Length);
        username.CopyTo(data, serverNonce.Length + clientNonce.Length + salt.Length);
        additionalChallenge.CopyTo(data, serverNonce.Length + clientNonce.Length + salt.Length + username.Length);

        data[^4] = (byte)iterations;
        data[^3] = (byte)(iterations >> 8);
        data[^2] = (byte)(iterations >> 16);
        data[^1] = (byte)(iterations >> 24);

        return data;
    }

    internal static byte[] ComputeClientKey(HashMethod hashMethod, byte[] saltedPassword)
    {
        return ComputeHmac(hashMethod, saltedPassword, StringClientKey);
    }

    internal static byte[] ComputeServerKey(HashMethod hashMethod, byte[] saltedPassword)
    {
        return ComputeHmac(hashMethod, saltedPassword, StringServerKey);
    }

    internal static byte[] ComputeStoredKey(HashMethod hashMethod, byte[] clientKey)
    {
        return Hash.Compute(CreateDigest(hashMethod), clientKey);
    }

    internal static byte[] ComputeHmac(HashMethod hashMethod, byte[] key, byte[] message)
    {
        return Hmac.Compute(CreateDigest(hashMethod), key, message);
    }

    internal static byte[] GenerateSaltedPassword(string password, byte[] salt, int iterations)
    {
        byte[] passwordBytes = Utf8.GetBytes(password.Normalize(NormalizationForm.FormKC));
        return GenerateSaltedPassword(passwordBytes, salt, iterations);
    }

    internal static byte[] GenerateSaltedPassword(byte[] passwordBytes, byte[] salt, int iterations)
    {
        using Pbkdf2 pass = new(HmacMethod.Sha512, passwordBytes, salt, iterations);
        return pass.GetBytes(PasswordSize);
    }

    #endregion
}