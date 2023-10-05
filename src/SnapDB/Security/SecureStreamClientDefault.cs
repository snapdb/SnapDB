﻿//******************************************************************************************************
//  SecureStreamClientDefault.cs - Gbtc
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
//  10/31/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.IO.StreamExtensions;

namespace SnapDB.Security;

/// <summary>
/// Creates a secure stream that connects via the default credential
/// </summary>
public class SecureStreamClientDefault : SecureStreamClientBase
{
    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="SecureStreamClientIntegratedSecurity"/>
    /// </summary>
    public SecureStreamClientDefault()
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Authenticates with the remote server.
    /// </summary>
    /// <param name="stream">The stream for authentication.</param>
    /// <param name="certSignatures">The certificate signatures to use for authentication.</param>
    /// <returns>
    /// True if authentication succeeds; otherwise, false.
    /// </returns>
    protected override bool InternalTryAuthenticate(Stream stream, byte[] certSignatures)
    {
        stream.WriteByte((byte)AuthenticationMode.None);
        return stream.ReadBoolean();
    }

    #endregion
}