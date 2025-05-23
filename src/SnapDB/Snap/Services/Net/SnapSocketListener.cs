﻿//******************************************************************************************************
//  SnapSocketListener.cs - Gbtc
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
//  08/15/2019 - J. Ritchie Carroll
//       Updated to allow for dual-stack bindings.
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Net.Sockets;
using System.Text;
using Gemstone.Diagnostics;
using SnapDB.Security;

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// Hosts a <see cref="SnapServer"/> on a network socket.
/// </summary>
public class SnapSocketListener : DisposableLoggingClassBase
{
    #region [ Members ]

    private readonly SecureStreamServer<SocketUserPermissions> m_authenticator;
    private readonly List<SnapNetworkServer> m_clients = [];
    private volatile bool m_isRunning;
    private readonly TcpListener m_listener;
    private SnapServer m_server;
    private readonly SnapSocketListenerSettings m_settings;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="SnapSocketListener"/>
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="server"></param>
    public SnapSocketListener(SnapSocketListenerSettings settings, SnapServer server) : base(MessageClass.Framework)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        m_server = server ?? throw new ArgumentNullException(nameof(server));
        m_settings = settings.CloneReadonly();
        m_settings.Validate();

        // Authenticator exposes "Users" property to allow looks ups of user permissions
        m_authenticator = new SecureStreamServer<SocketUserPermissions>();

        if (settings.DefaultUserCanRead || settings.DefaultUserCanWrite || settings.DefaultUserIsAdmin)
        {
            m_authenticator.SetDefaultUser(true, new SocketUserPermissions
            {
                CanRead = settings.DefaultUserCanRead,
                CanWrite = settings.DefaultUserCanWrite,
                IsAdmin = settings.DefaultUserIsAdmin
            });
        }

        foreach (string user in settings.Users)
        {
            m_authenticator.AddUserIntegratedSecurity(user, new SocketUserPermissions
            {
                CanRead = true,
                CanWrite = true,
                IsAdmin = true
            });
        }

        m_isRunning = true;

        m_listener = new TcpListener(m_settings.LocalEndPoint);
        
        if (m_settings.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            m_listener.Server.DualMode = true;
        
        m_listener.Start();

        Log.Publish(MessageLevel.Info, "Constructor Called", $"Listening on {m_settings.LocalEndPoint}");
        new Thread(ProcessDataRequests) { IsBackground = true }.Start();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SnapSocketListener"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        try
        {
            if (!disposing)
                return;

            m_isRunning = false;

            m_listener?.Stop();

            m_server = null;

            lock (m_clients)
            {
                foreach (SnapNetworkServer client in m_clients)
                    client.Dispose();
                m_clients.Clear();
            }
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
            base.Dispose(disposing); // Call base class Dispose().
        }
    }

    /// <summary>
    /// Gets the status of the <see cref="SnapSocketListener"/>.
    /// </summary>
    /// <param name="status"></param>
    public void GetFullStatus(StringBuilder status)
    {
        lock (m_clients)
        {
            status.AppendFormat("Active Client Count: {0}\r\n", m_clients.Count);
            foreach (SnapNetworkServer client in m_clients)
                client.GetFullStatus(status);
        }
    }

    /// <summary>
    /// Processes the client
    /// </summary>
    /// <param name="state"></param>
    private void ProcessDataRequests(object? state)
    {
        try
        {
            while (m_isRunning && !m_listener.Pending())
                Thread.Sleep(10);

            if (!m_isRunning)
            {
                m_listener.Stop();
                Log.Publish(MessageLevel.Info, "Socket Listener Stopped");
                return;
            }

            TcpClient client = m_listener.AcceptTcpClient();
            Log.Publish(MessageLevel.Info, "Client Connection", $"Client connection attempted from: {client.Client.RemoteEndPoint}");

            new Thread(ProcessDataRequests) { IsBackground = true }.Start();

            SnapNetworkServer networkServerProcessing;

            using (Logger.AppendStackMessages(Log.InitialStackMessages))
            {
                networkServerProcessing = new SnapNetworkServer(m_authenticator, client, m_server, m_settings.DefaultUser)
                {
                    UserCanSeek = m_settings.UserCanSeek,
                    UserCanMatch = m_settings.UserCanMatch,
                    UserCanWrite = m_settings.UserCanWrite
                };
            }

            lock (m_clients)
            {
                if (m_isRunning)
                {
                    m_clients.Add(networkServerProcessing);
                }
                else
                {
                    TryShutdownSocket(client);
                    client.Close();
                    return;
                }
            }

            try
            {
                networkServerProcessing.ProcessClient();
            }
            finally
            {
                // If we made it this far, the client must have been added to the
                // list of active clients. In the past, errors in ProcessClient have
                // caused a leak here, so the try-finally should help protect against that
                lock (m_clients)
                    m_clients.Remove(networkServerProcessing);
            }
        }
        catch (Exception ex)
        {
            Log.Publish(MessageLevel.Critical, "Client Processing Failed", "An unhandled Exception occurred while processing clients", null, ex);
        }
    }

    private void TryShutdownSocket(TcpClient client)
    {
        try
        {
            client.Client?.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException ex)
        {
            // In particular, the ConnectionReset socket error absolutely should not prevent
            // us from calling TcpClient.Close(), nor should it propagate an exception up the stack
            Log.Publish(MessageLevel.Debug, "SnapConnectionReset", null, null, ex);
        }
    }

    #endregion
}