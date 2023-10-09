//******************************************************************************************************
//  SecureStream.cs - Gbtc
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
//  08/29/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Net.Security;
using System.Text;

namespace SnapDB.Security;

internal enum AuthenticationMode : byte
{
    None = 1,
    Srp = 2,
    Scram = 3,
    Integrated = 4,
    Certificate = 5,
    ResumeSession = 255
}

/// <summary>
/// Provides utility methods for secure communication using SSL/TLS streams.
/// </summary>
public class SecureStream
{
    #region [ Static ]
    /// <summary>
    /// Computes a certificate challenge for secure communication.
    /// </summary>
    /// <param name="isServer">Indicates whether the calling entity is the server.</param>
    /// <param name="stream">The SSL stream used for communication.</param>
    /// <returns>
    /// A byte array representing the computed certificate challenge.
    /// If <paramref name="isServer"/> is <c>true</c>, the challenge combines the remote and local certificate hashes.
    /// If <paramref name="isServer"/> is <c>false</c>, the challenge combines the local and remote certificate hashes.
    /// </returns>
    internal static byte[] ComputeCertificateChallenge(bool isServer, SslStream stream)
    {
        string localChallenge = string.Empty;
        string remoteChallenge = string.Empty;
        if (stream.RemoteCertificate is not null)
            remoteChallenge = stream.RemoteCertificate.GetCertHashString();

        if (stream.LocalCertificate is not null)
            localChallenge = stream.LocalCertificate.GetCertHashString();

        if (isServer)
            return Encoding.UTF8.GetBytes(remoteChallenge + localChallenge);

        return Encoding.UTF8.GetBytes(localChallenge + remoteChallenge);
    }

    #endregion
}