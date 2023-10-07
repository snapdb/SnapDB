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
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace SnapDB.Security.Authentication;

/// <summary>
/// An individual server side user credential
/// </summary>
public class ScramUserCredential
{
    #region [ Members ]

    public HashMethod HashMethod;
    public int Iterations;
    public byte[] Salt;
    public byte[] ServerKey;
    public byte[] StoredKey;
    public ReadonlyByteArray UserBytes;
    public string UserName;

    private readonly HMac m_clientSignature;
    private readonly IDigest m_computeStoredKey;
    private readonly HMac m_serverSignature;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Adds the following user information to the server's user database
    /// </summary>
    /// <param name="username">the username. Cannot be more than 100 characters</param>
    /// <param name="password">the password. Cannot be more than 1024 characters</param>
    /// <param name="iterations">The number of iterations. On 2014 technology, 2000 iterations takes about 10ms to compute</param>
    /// <param name="saltSize">The size of the salt. Defaults to 32 bytes.</param>
    /// <param name="hashMethod">The hash method to use for authentication</param>
    /// <remarks>
    /// Setting a vary large Iterations will not effect how long it takes to negotiate a client on the server end. This is because
    /// the server precomputes the hash results. The client can optionally also precompute the results so negotiation can take
    /// milliseconds.
    /// </remarks>
    public ScramUserCredential(string username, string password, int iterations = 4000, int saltSize = 32, HashMethod hashMethod = HashMethod.Sha256)
    {
        UserName = username.Normalize(NormalizationForm.FormKC);
        UserBytes = new ReadonlyByteArray(Encoding.UTF8.GetBytes(UserName));

        Iterations = iterations;
        HashMethod = hashMethod;
        Salt = SaltGenerator.Create(saltSize);

        byte[] saltedPassword = Scram.GenerateSaltedPassword(password.Normalize(NormalizationForm.FormKC), Salt, Iterations);
        byte[] clientKey = Scram.ComputeClientKey(hashMethod, saltedPassword);

        StoredKey = Scram.ComputeStoredKey(hashMethod, clientKey);
        ServerKey = Scram.ComputeServerKey(hashMethod, saltedPassword);

        m_clientSignature = new HMac(Scram.CreateDigest(HashMethod));
        m_clientSignature.Init(new KeyParameter(StoredKey));

        m_serverSignature = new HMac(Scram.CreateDigest(HashMethod));
        m_serverSignature.Init(new KeyParameter(ServerKey));

        m_computeStoredKey = Scram.CreateDigest(HashMethod);
    }

    #endregion

    #region [ Methods ]

    public byte[] ComputeClientSignature(byte[] authMessage)
    {
        byte[] result = new byte[m_clientSignature.GetMacSize()];

        if (!Monitor.TryEnter(m_clientSignature))
            return Hmac.Compute(Scram.CreateDigest(HashMethod), StoredKey, authMessage);
        
        try
        {
            m_clientSignature.BlockUpdate(authMessage, 0, authMessage.Length);
            m_clientSignature.DoFinal(result, 0);
        }
        finally
        {
            Monitor.Exit(m_clientSignature);
        }

        return result;
    }

    public byte[] ComputeServerSignature(byte[] authMessage)
    {
        byte[] result = new byte[m_serverSignature.GetMacSize()];

        if (!Monitor.TryEnter(m_serverSignature))
            return Hmac.Compute(Scram.CreateDigest(HashMethod), StoredKey, authMessage);

        try
        {
            m_serverSignature.BlockUpdate(authMessage, 0, authMessage.Length);
            m_serverSignature.DoFinal(result, 0);
        }
        finally
        {
            Monitor.Exit(m_serverSignature);
        }

        return result;
    }

    public byte[] ComputeStoredKey(byte[] clientKey)
    {
        byte[] result = new byte[m_computeStoredKey.GetDigestSize()];

        if (!Monitor.TryEnter(m_computeStoredKey))
            return Hash.Compute(Scram.CreateDigest(HashMethod), clientKey);
        
        try
        {
            m_computeStoredKey.BlockUpdate(clientKey, 0, clientKey.Length);
            m_computeStoredKey.DoFinal(result, 0);
        }
        finally
        {
            Monitor.Exit(m_computeStoredKey);
        }

        return result;
    }

    public void Save()
    {
    }

    public void Load()
    {
    }

    #endregion
}