//******************************************************************************************************
//  HMAC.cs - Gbtc
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
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.ArrayExtensions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace SnapDB.Security;

/// <summary>
/// Provides utility methods for computing Hash-based Message Authentication Code (HMAC) values using various hash algorithms.
/// </summary>
public static class Hmac
{
    #region [ Static ]
    /// <summary>
    /// Computes an HMAC using the specified hash algorithm and key for the entire input data.
    /// </summary>
    /// <param name="hash">The hash algorithm to use for the HMAC.</param>
    /// <param name="key">The secret key for the HMAC computation.</param>
    /// <param name="data">The input data to compute the HMAC over.</param>
    /// <returns>The computed HMAC as a byte array.</returns>
    public static byte[] Compute(IDigest hash, byte[] key, byte[] data)
    {
        return Compute(hash, key, data, 0, data.Length);
    }

    /// <summary>
    /// Computes an HMAC using the specified hash algorithm and key for the entire input data.
    /// </summary>
    /// <param name="hash">The hash algorithm to use for the HMAC.</param>
    /// <param name="key">The secret key for the HMAC computation.</param>
    /// <param name="data">The input data to compute the HMAC over.</param>
    /// <returns>The computed HMAC as a byte array.</returns>
    public static byte[] Compute(IDigest hash, byte[] key, byte[] data, int position, int length)
    {
        data.ValidateParameters(position, length);
        HMac hmac = new(hash);
        hmac.Init(new KeyParameter(key));
        byte[] result = new byte[hmac.GetMacSize()];
        hmac.BlockUpdate(data, position, length);
        hmac.DoFinal(result, 0);
        return result;
    }

    #endregion
}

/// <summary>
/// Provides utility methods for computing Hash-based Message Authentication Code (HMAC) values using a specific hash algorithm of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of hash algorithm to use, which must implement the IDigest interface and have a default constructor.</typeparam>
public static class Hmac<T> where T : IDigest, new()
{
    #region [ Static ]

    /// <summary>
    /// Computes an HMAC using the specified secret key for the entire input data.
    /// </summary>
    /// <param name="key">The secret key for the HMAC computation.</param>
    /// <param name="data">The input data to compute the HMAC over.</param>
    /// <returns>The computed HMAC as a byte array.</returns>
    public static byte[] Compute(byte[] key, byte[] data)
    {
        return Hmac.Compute(new T(), key, data, 0, data.Length);
    }

    /// <summary>
    /// Computes an HMAC using the specified secret key, input data, and a specified range within the input data.
    /// </summary>
    /// <param name="key">The secret key for the HMAC computation.</param>
    /// <param name="data">The input data to compute the HMAC over.</param>
    /// <param name="position">The starting position in the input data.</param>
    /// <param name="length">The length of the input data to include in the HMAC computation.</param>
    /// <returns>The computed HMAC as a byte array.</returns>
    public static byte[] Compute(byte[] key, byte[] data, int position, int length)
    {
        return Hmac.Compute(new T(), key, data, position, length);
    }

    #endregion
}