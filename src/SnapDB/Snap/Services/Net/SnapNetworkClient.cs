﻿//******************************************************************************************************
//  SnapNetworkClient.cs - Gbtc
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
//  12/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Net;
using System.Net.Sockets;
using Gemstone.Diagnostics;
using SnapDB.Security;

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// A client that communicates over a network socket.
/// </summary>
public class SnapNetworkClient : SnapStreamingClient
{
    #region [ Members ]

    private readonly TcpClient m_client;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="SnapNetworkClient"/>
    /// </summary>
    /// <param name="settings">The config to use for the client</param>
    /// <param name="credentials">
    /// The network credentials to use.
    /// If left null, the computers current credentials are use.
    /// </param>
    /// <param name="useSsl">Specifies if ssl encryption is desired for the connection.</param>
    public SnapNetworkClient(SnapNetworkClientSettings settings, SecureStreamClientBase? credentials = null, bool useSsl = false)
    {
        if (credentials is null)
        {
            if (settings.UseIntegratedSecurity)
                credentials = new SecureStreamClientIntegratedSecurity();
            else
                credentials = new SecureStreamClientDefault();
        }

        if (!IPAddress.TryParse(settings.ServerNameOrIP, out IPAddress? ip))
            ip = Dns.GetHostAddresses(settings.ServerNameOrIP)[0];

        IPEndPoint server = new(ip, settings.NetworkPort);

        m_client = new TcpClient(ip.AddressFamily);
        m_client.Connect(server);

        m_client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        Initialize(new NetworkStream(m_client.Client), credentials, useSsl);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SnapNetworkClient"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        base.Dispose(disposing); // Call base class Dispose().

        try
        {
            // This will be done regardless of whether the object is finalized or disposed.
            if (!disposing)
                return;
            
            try
            {
                m_client.Client.Shutdown(SocketShutdown.Both);
                m_client.Close();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex);
            }
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
        }
    }

    #endregion
}