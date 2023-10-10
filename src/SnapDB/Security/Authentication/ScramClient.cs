//******************************************************************************************************
//  ScramClient.cs - Gbtc
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
//  8/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Text;
using Gemstone.IO.StreamExtensions;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
public class ScramClient
{
    #region [ Members ]

    private byte[] m_clientKey;
    private HMac m_clientSignature;
    private HashMethod m_hashMethod;
    private int m_iterations;

    private readonly NonceGenerator m_nonce = new(16);
    private readonly byte[] m_passwordBytes;
    private byte[]? m_salt;
    private byte[] m_saltedPassword;
    private byte[] m_serverKey;
    private HMac m_serverSignature;
    private byte[] m_storedKey;
    private readonly byte[] m_usernameBytes;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the SCRAM (Salted Challenge Response Authentication Mechanism) client with the provided username and password.
    /// </summary>
    /// <param name="username">The username to be used for authentication.</param>
    /// <param name="password">The password associated with the username.</param>
    /// <remarks>
    /// This constructor initializes a new instance of the SCRAM client with the provided <paramref name="username"/> and <paramref name="password"/>.
    /// It converts the username and password to UTF-8 bytes and normalizes them using FormKC normalization.
    /// </remarks>
    /// <seealso cref="Scram"/>
    public ScramClient(string username, string password)
    {
        m_usernameBytes = Scram.Utf8.GetBytes(username.Normalize(NormalizationForm.FormKC));
        m_passwordBytes = Scram.Utf8.GetBytes(password.Normalize(NormalizationForm.FormKC));
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Authenticates the client to the server using SCRAM-SHA-256 or SCRAM-SHA-1 mechanism.
    /// </summary>
    /// <param name="stream">The stream used for communication with the server.</param>
    /// <param name="additionalChallenge">Optional additional challenge data to include in the authentication process.</param>
    /// <returns>True if authentication is successful, otherwise false.</returns>
    public bool AuthenticateAsClient(Stream stream, byte[]? additionalChallenge = null)
    {
        additionalChallenge ??= Array.Empty<byte>();

        byte[] clientNonce = m_nonce.Next();
        stream.WriteWithLength(m_usernameBytes);
        stream.WriteWithLength(clientNonce);
        stream.Flush();

        HashMethod hashMethod = (HashMethod)stream.ReadByte();
        byte[] serverNonce = stream.ReadBytes();
        byte[] salt = stream.ReadBytes();
        int iterations = stream.ReadInt32();

        SetServerValues(hashMethod, salt, iterations);

        byte[] authMessage = Scram.ComputeAuthMessage(serverNonce, clientNonce, salt, m_usernameBytes, iterations, additionalChallenge);
        byte[] clientSignature = ComputeClientSignature(authMessage);
        byte[] clientProof = Scram.Xor(m_clientKey, clientSignature);
        stream.WriteWithLength(clientProof);
        stream.Flush();

        byte[] serverSignature = ComputeServerSignature(authMessage);
        byte[] serverSignatureVerify = stream.ReadBytes();

        return serverSignature.SecureEquals(serverSignatureVerify);
    }

    /// <summary>
    /// Sets the server parameters and regenerates the salted password if
    /// the salt values have changed.
    /// </summary>
    /// <param name="hashMethod">The hashing method.</param>
    /// <param name="salt">The salt for the user credentials.</param>
    /// <param name="iterations">The number of iterations.</param>
    private void SetServerValues(HashMethod hashMethod, byte[] salt, int iterations)
    {
        bool hasPasswordDataChanged = false;
        bool hasHashMethodChanged = false;

        if (m_salt is null || !salt.SecureEquals(m_salt))
        {
            hasPasswordDataChanged = true;
            m_salt = salt;
        }

        if (iterations != m_iterations)
        {
            hasPasswordDataChanged = true;
            m_iterations = iterations;
        }

        if (m_hashMethod != hashMethod)
        {
            m_hashMethod = hashMethod;
            hasHashMethodChanged = true;
        }

        if (hasPasswordDataChanged)
            m_saltedPassword = Scram.GenerateSaltedPassword(m_passwordBytes, m_salt, m_iterations);

        if (hasPasswordDataChanged || hasHashMethodChanged)
        {
            m_serverKey = Scram.ComputeServerKey(m_hashMethod, m_saltedPassword);
            m_clientKey = Scram.ComputeClientKey(m_hashMethod, m_saltedPassword);
            m_storedKey = Scram.ComputeStoredKey(m_hashMethod, m_clientKey);
            m_clientSignature = new HMac(Scram.CreateDigest(m_hashMethod));
            m_clientSignature.Init(new KeyParameter(m_storedKey));

            m_serverSignature = new HMac(Scram.CreateDigest(m_hashMethod));
            m_serverSignature.Init(new KeyParameter(m_serverKey));
        }
    }

    private byte[] ComputeClientSignature(byte[] authMessage)
    {
        byte[] result = new byte[m_clientSignature.GetMacSize()];

        lock (m_clientSignature)
        {
            m_clientSignature.BlockUpdate(authMessage, 0, authMessage.Length);
            m_clientSignature.DoFinal(result, 0);
        }

        return result;
    }

    private byte[] ComputeServerSignature(byte[] authMessage)
    {
        byte[] result = new byte[m_serverSignature.GetMacSize()];

        lock (m_serverSignature)
        {
            m_serverSignature.BlockUpdate(authMessage, 0, authMessage.Length);
            m_serverSignature.DoFinal(result, 0);
        }

        return result;
    }

    #endregion
}