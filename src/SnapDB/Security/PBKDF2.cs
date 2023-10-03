//******************************************************************************************************
//  PBKDF2.cs - Gbtc
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
//  08/01/2014 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Security.Cryptography;
using Gemstone.ArrayExtensions;
using Gemstone;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;

namespace SnapDB.Security;

/// <summary>
/// A series of HMAC implementations supported by .NET
/// </summary>
public enum HmacMethod
{
    Md5,
    TripleDes,
    Ripemd160,
    Sha1,
    Sha256,
    Sha384,
    Sha512
}
/// <summary>
/// Implements a generic PBKDF2 <see cref="DeriveBytes"/> that will work from a custom cryptographic transform.
/// <see cref="Rfc2898DeriveBytes"/> only implements a SHA-1 underlying hash function.
/// </summary>
/// <remarks>
/// It is recommended to use one of the HMAC-SHA implementations unless you understand the implications
/// of using something differently.
/// </remarks>
public class Pbkdf2
    : DeriveBytes
{
    //See defintion in: http://tools.ietf.org/html/rfc2898

    /// <summary>
    /// Contains the salt, along with an extra 4 bytes at the end to place the block number
    /// </summary>
    private byte[] m_saltWithBlock;
    /// <summary>
    /// The block number, which starts at 1, and increases every time that a new block must be computed.
    /// </summary>
    private int m_blockNumber;
    /// <summary>
    /// A temporary location to store the hashed bytes.
    /// </summary>
    private readonly Queue<byte> m_results = new();

    private HMac m_hash1;

    private uint m_iterations;

    /// <summary>
    /// Implements a <see cref="Pbkdf2"/> algorthim with a user definded MAC method.
    /// </summary>
    /// <param name="method">the HMAC method to use.</param>
    /// <param name="password">the password to use</param>
    /// <param name="salt">the salt. recommended to be at least 64-bit</param>
    /// <param name="iterations">the number of iterations. Recommended to be at least 1000</param>
    public Pbkdf2(HmacMethod method, byte[] password, byte[] salt, int iterations)
    {
        if (password is null)
            throw new ArgumentNullException(nameof(password));

        switch (method)
        {
            case HmacMethod.Md5:
                Initialize(new HMac(new MD5Digest()), password, salt, iterations);
                break;
            case HmacMethod.TripleDes:
                // Initialize(new MACTripleDES(password), salt, iterations);
                break;
            case HmacMethod.Ripemd160:
                Initialize(new HMac(new RipeMD160Digest()), password, salt, iterations);
                // Initialize(new HMACRIPEMD160(password), salt, iterations);
                break;
            case HmacMethod.Sha1:
                Initialize(new HMac(new Sha1Digest()), password, salt, iterations);
                // Initialize(new HMAC<SHA1Core>(password), salt, iterations);
                break;
            case HmacMethod.Sha256:
                Initialize(new HMac(new Sha256Digest()), password, salt, iterations);
                // Initialize(new HMAC<SHA256Core>(password), salt, iterations);
                break;
            case HmacMethod.Sha384:
                Initialize(new HMac(new Sha384Digest()), password, salt, iterations);
                // Initialize(new HMACSHA384(password), salt, iterations);
                break;
            case HmacMethod.Sha512:
                Initialize(new HMac(new Sha512Digest()), password, salt, iterations);
                // Initialize(new HMAC<SHA512Core>(password), salt, iterations);
                // Initialize(new HMACSHA512(password), salt, iterations);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method));
        }
    }

    private void Initialize(HMac hash, byte[] passwordBytes, byte[] salt, int iterations)
    {
        if (hash is null)
            throw new ArgumentNullException(nameof(hash));
        if (salt is null)
            throw new ArgumentNullException(nameof(salt));

        hash.Init(new KeyParameter(passwordBytes));
        m_blockNumber = 1;
        m_saltWithBlock = salt.Combine(BigEndian.GetBytes(m_blockNumber));
        m_iterations = (uint)iterations;
        m_results.Clear();
        m_hash1 = hash;
    }

    /// <summary>
    /// When overridden in a derived class, resets the state of the operation.
    /// </summary>
    public override void Reset()
    {
        m_results.Clear();
        m_blockNumber = 1;
    }

    /// <summary>
    /// When overridden in a derived class, returns pseudo-random key bytes.
    /// </summary>
    /// <returns>
    /// A byte array filled with pseudo-random key bytes.
    /// </returns>
    /// <param name="cb">The number of pseudo-random key bytes to generate. </param>
    public override byte[] GetBytes(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "must be positive");

        byte[] results = new byte[length];
        for (int x = 0; x < length; x++)
        {
            if (m_results.Count == 0)
                ComputeNextBlock();
            results[x] = m_results.Dequeue();
        }
        return results;
    }


    /// <summary>
    /// When overridden in a derived class, releases the unmanaged resources used by the <see cref="T:System.Security.Cryptography.DeriveBytes"/> class and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_hash1 = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Computes the next block of crypto bytes.
    /// </summary>
    private void ComputeNextBlock()
    {
        ComputeNextBlock(m_hash1);
    }

    private void ComputeNextBlock(HMac hash)
    {
        BigEndian.CopyBytes(m_blockNumber, m_saltWithBlock, m_saltWithBlock.Length - 4);

        byte[] final = new byte[hash.GetMacSize()];
        byte[] tmp = new byte[hash.GetMacSize()];

        // InitialPass: U1 = PRF(Password, Salt || INT_32_BE(i))
        hash.Reset();
        hash.BlockUpdate(m_saltWithBlock, 0, m_saltWithBlock.Length);
        hash.DoFinal(final, 0);

        final.CopyTo(tmp, 0);

        for (int iteration = 1; iteration < m_iterations; iteration++)
        {
            // U2 = PRF(Password, U1)
            // hash.Reset();
            hash.BlockUpdate(tmp, 0, tmp.Length);
            hash.DoFinal(tmp, 0);
            for (int x = 0; x < tmp.Length; x++)
                final[x] ^= tmp[x];
        }

        m_blockNumber++;
        foreach (byte b in final)
            m_results.Enqueue(b);
    }



    /// <summary>
    /// Implements a <see cref="Pbkdf2"/> algorthim with a user definded MAC method.
    /// </summary>
    /// <param name="method">the HMAC method to use.</param>
    /// <param name="password">the password to use</param>
    /// <param name="salt">the salt. Must be at least 64-bit</param>
    /// <param name="iterations">the number of iterations. Must be at least 1000.</param>
    /// <param name="length">the number of bytes to return</param>
    /// <returns>
    /// A salted password based on the specified length.
    /// </returns>
    public static byte[] ComputeSaltedPassword(HmacMethod method, byte[] password, byte[] salt, int iterations, int length)
    {
        using Pbkdf2 kdf = new(method, password, salt, iterations);
        return kdf.GetBytes(length);
    }

}
