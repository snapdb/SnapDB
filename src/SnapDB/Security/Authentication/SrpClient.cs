//******************************************************************************************************
//  SrpClient.cs - Gbtc
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

using Gemstone.ArrayExtensions;
using Gemstone.IO.StreamExtensions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math;

namespace SnapDB.Security.Authentication;

/// <summary>
/// Provides simple password based authentication that uses Secure Remote Password.
/// </summary>
public class SrpClient
{
    #region [ Members ]

    private Srp6Client m_client = default!;

    private readonly PbdkfCredentials m_credentials;
    private IDigest m_hash = default!;
    private SrpConstants m_param = default!;
    private byte[]? m_resumeTicket;

    // Session Resume Details
    private byte[]? m_sessionSecret;

    // The SRP Mechanism
    private int m_srpByteLength;
    private SrpStrength m_strength;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a client that will authenticate with the specified
    /// username and password.
    /// </summary>
    /// <param name="username">the username</param>
    /// <param name="password">the password</param>
    public SrpClient(string username, string password)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        if (password is null)
            throw new ArgumentNullException(nameof(password));


        m_credentials = new PbdkfCredentials(username, password);

        if (m_credentials.UsernameBytes.Length > 1024)
            throw new ArgumentException("Username cannot consume more than 1024 bytes encoded as UTF8", nameof(username));
    }

    #endregion

    #region [ Methods ]
    /// <summary>
    /// Authenticates the client session with the server using the provided network stream and optional additional challenge.
    /// </summary>
    /// <param name="stream">The network stream used for communication with the server.</param>
    /// <param name="additionalChallenge">An optional additional challenge to include in the authentication process (default is <c>null</c>).</param>
    /// <returns><c>true</c> if authentication succeeds; otherwise, <c>false</c>.</returns>
    public bool AuthenticateAsClient(Stream stream, byte[]? additionalChallenge = null)
    {
        additionalChallenge ??= Array.Empty<byte>();

        stream.Write((short)m_credentials.UsernameBytes.Length);
        stream.Write(m_credentials.UsernameBytes);

        if (m_resumeTicket is not null)
        {
            // resume
            stream.Write((byte)2);
            stream.Flush();

            return ResumeSession(stream, additionalChallenge);
        }

        stream.Write((byte)1);
        stream.Flush();

        return Authenticate(stream, additionalChallenge);
    }

    private void SetSrpStrength(SrpStrength strength)
    {
        m_strength = strength;
        m_srpByteLength = (int)strength >> 3;
        m_param = SrpConstants.Lookup(m_strength);
        m_client = new Srp6Client(m_param);
    }

    private void SetHashMethod(HashMethod hashMethod)
    {
        m_hash = hashMethod switch
        {
            HashMethod.Sha1 => new Sha1Digest(),
            HashMethod.Sha256 => new Sha256Digest(),
            HashMethod.Sha384 => new Sha384Digest(),
            HashMethod.Sha512 => new Sha512Digest(),
            _ => throw new Exception("Unsupported Hash Method")
        };
    }

    private bool ResumeSession(Stream stream, byte[] additionalChallenge)
    {
        stream.Write((byte)16);
        byte[] aChallenge = SaltGenerator.Create(16);

        stream.Write(aChallenge);
        stream.Write((short)m_resumeTicket!.Length);
        stream.Write(m_resumeTicket);
        stream.Flush();

        if (stream.ReadBoolean())
        {
            SetHashMethod((HashMethod)stream.ReadNextByte());

            byte[] bChallenge = stream.ReadBytes(stream.ReadNextByte());
            byte[] clientProof = m_hash.ComputeHash(aChallenge, bChallenge, m_sessionSecret!, additionalChallenge);

            stream.Write(clientProof);
            stream.Flush();

            if (stream.ReadBoolean())
            {
                byte[] serverProof = m_hash.ComputeHash(bChallenge, aChallenge, m_sessionSecret!, additionalChallenge);
                byte[] serverProofCheck = stream.ReadBytes(m_hash.GetDigestSize());

                return serverProof.SecureEquals(serverProofCheck);
            }
        }

        m_sessionSecret = null;
        m_resumeTicket = null;

        return Authenticate(stream, additionalChallenge);
    }

    private bool Authenticate(Stream stream, byte[] additionalChallenge)
    {
        HashMethod passwordHashMethod = (HashMethod)stream.ReadNextByte();
        byte[] salt = stream.ReadBytes(stream.ReadNextByte());
        int iterations = stream.ReadInt32();

        SetHashMethod((HashMethod)stream.ReadNextByte());
        SetSrpStrength((SrpStrength)stream.ReadInt32());

        m_credentials.TryUpdate(passwordHashMethod, salt, iterations);

        BigInteger pubA = m_client.GenerateClientCredentials(m_hash, salt, m_credentials.UsernameBytes, m_credentials.SaltedPassword);
        byte[] pubABytes = pubA.ToPaddedArray(m_srpByteLength);

        stream.Write(pubABytes);
        stream.Flush();

        // Read from Server: B
        byte[] pubBBytes = stream.ReadBytes(m_srpByteLength);
        BigInteger pubB = new(1, pubBBytes);

        // Calculate Session Key
        BigInteger s = m_client.CalculateSecret(m_hash, pubB);
        byte[] sBytes = s.ToPaddedArray(m_srpByteLength);
        byte[] clientProof = m_hash.ComputeHash(pubABytes, pubBBytes, sBytes, additionalChallenge);

        stream.Write(clientProof);
        stream.Flush();

        byte[] serverProof = m_hash.ComputeHash(pubBBytes, pubABytes, sBytes, additionalChallenge);

        if (!stream.ReadBoolean())
            return false;
        
        byte[] serverProofCheck = stream.ReadBytes(m_hash.GetDigestSize());
        int ticketLength = stream.ReadInt16();
        
        if (ticketLength is < 0 or > 10000)
            return false;

        if (!serverProofCheck.SecureEquals(serverProof))
            return false;
        
        m_resumeTicket = stream.ReadBytes(ticketLength);
        m_sessionSecret = m_hash.ComputeHash(pubABytes, sBytes, pubBBytes).Combine(m_hash.ComputeHash(pubBBytes, sBytes, pubABytes));
        
        return true;
    }

    #endregion
}