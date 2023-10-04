//******************************************************************************************************
//  NetworkBinaryStream.cs - Gbtc
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
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Net.Sockets;
using SnapDB.IO;
using SnapDB.Threading;

namespace SnapDB.Net;

/// <summary>
/// Represents a binary stream over a network connection.
/// </summary>
public class NetworkBinaryStream : RemoteBinaryStream
{
    #region [ Members ]

    private Socket m_socket;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkBinaryStream"/> class.
    /// </summary>
    /// <param name="socket">The underlying socket to use for communication.</param>
    /// <param name="timeout">The socket timeout in milliseconds.</param>
    /// <param name="workerThreadSynchronization">Optional worker thread synchronization object.</param>
    /// <exception cref="Exception"></exception>
    public NetworkBinaryStream(Socket socket, int timeout = -1, WorkerThreadSynchronization? workerThreadSynchronization = null) : base(new NetworkStream(socket), workerThreadSynchronization)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("BigEndian processors are not supported");

        m_socket = socket;
        Timeout = timeout;
        m_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the underlying socket used for communication.
    /// </summary>
    public Socket Socket => m_socket;

    /// <summary>
    /// Gets or sets the socket timeout in milliseconds.
    /// </summary>
    public int Timeout
    {
        get => m_socket.ReceiveTimeout;
        set
        {
            m_socket.ReceiveTimeout = value;
            m_socket.SendTimeout = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not the socket is connected.
    /// </summary>
    public bool Connected => m_socket is not null && m_socket.Connected;

    /// <summary>
    /// Gets the number of available bytes to read from the stream.
    /// </summary>
    public int AvailableReadBytes
    {
        get
        {
            WorkerThreadSynchronization.PulseSafeToCallback();

            return ReceiveBufferAvailable + m_socket.Available;
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Disconnects the socket.
    /// </summary>
    public void Disconnect()
    {
        Socket socket = Interlocked.Exchange(ref m_socket, null);
        if (socket is not null)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                socket.Close();
            }
            catch
            {
            }
        }

        WorkerThreadSynchronization.BeginSafeToCallbackRegion();
    }

    /// <summary>
    /// Disposes of the <see cref="NetworkBinaryStream"/> instance, disconnecting the socket if necessary.
    /// </summary>
    /// <param name="disposing"><c>true</c> if called from <see cref="Dispose"/>, <c>false</c> if called from the finalizer.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Disconnect();

        base.Dispose(disposing);
    }

    #endregion
}