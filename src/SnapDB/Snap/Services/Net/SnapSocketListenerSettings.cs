//******************************************************************************************************
//  SnapSocketListenerSettings.cs - Gbtc
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
//  05/16/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  08/15/2019 - J. Ritchie Carroll
//       Updated to allow for IPv6 bindings.
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using System.Net;
using Gemstone.Communication;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;

// ReSharper disable RedundantDefaultMemberInitializer
namespace SnapDB.Snap.Services.Net;

/// <summary>
/// Contains the basic config for a socket interface.
/// </summary>
public class SnapSocketListenerSettings : SettingsBase<SnapSocketListenerSettings>
{
    #region [ Members ]

    /// <summary>
    /// Defines the default network IP address for the <see cref="SnapSocketListener"/>.
    /// </summary>
    public const string DefaultIpAddress = "";

    /// <summary>
    /// Defines the default network port for a <see cref="SnapSocketListener"/>.
    /// </summary>
    public const int DefaultNetworkPort = 38402;

    /// <summary>
    /// A server name that must be supplied at startup before a key exchange occurs.
    /// </summary>
    public const string DefaultServerName = "openHistorian";

    /// <summary>
    /// Gets or sets a value indicating whether the default user has read access.
    /// </summary>
    public bool DefaultUserCanRead = false;

    /// <summary>
    /// Gets or sets a value indicating whether the default user has write access.
    /// </summary>
    public bool DefaultUserCanWrite = false;

    /// <summary>
    /// Gets or sets a value indicating whether the default user is an admin.
    /// </summary>
    public bool DefaultUserIsAdmin = false;

    // Force the use of SSL for all clients connecting to this socket.
    private bool m_forceSsl = false;

    // The local IP address to host on. Leave empty to bind to all local interfaces.
    private string m_localIpAddress = DefaultIpAddress;

    // The local TCP port to host on.
    private int m_localTcpPort = DefaultNetworkPort;

    // A server name that must be supplied at startup before a key exchange occurs.
    private string m_serverName = DefaultServerName;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Force the use of SSL for all clients connecting to this socket.
    /// </summary>
    public bool ForceSsl
    {
        get => m_forceSsl;
        set
        {
            TestForEditable();
            m_forceSsl = value;
        }
    }

    /// <summary>
    /// Gets the local <see cref="IPEndPoint"/> from the values in local IP address TCP port.
    /// </summary>
    public IPEndPoint LocalEndPoint
    {
        get
        {
        #if SQLCLR
            if (string.IsNullOrWhiteSpace(m_localIpAddress))
                return new IPEndPoint(IPAddress.Any, m_localTcpPort);

            return new IPEndPoint(IPAddress.Parse(m_localIpAddress), m_localTcpPort);
        #else
            // SnapSocketListener automatically enables dual-stack socket for IPv6 to support legacy client implementations expecting IPv4 hosting
            IPStack ipStack = string.IsNullOrWhiteSpace(m_localIpAddress) ? Transport.GetDefaultIPStack() : Transport.IsIPv6IP(m_localIpAddress) ? IPStack.IPv6 : IPStack.IPv4;

            return Transport.CreateEndPoint(m_localIpAddress, m_localTcpPort, ipStack);
        #endif
        }
    }

    /// <summary>
    /// The local IP address to host on. Leave empty to bind to all local interfaces.
    /// </summary>
    public string LocalIpAddress
    {
        get => m_localIpAddress;
        set
        {
            TestForEditable();
            m_localIpAddress = value;
        }
    }

    /// <summary>
    /// The local TCP port to host on.
    /// </summary>
    public int LocalTcpPort
    {
        get => m_localTcpPort;
        set
        {
            TestForEditable();
            m_localTcpPort = value;
        }
    }

    /// <summary>
    /// A list of all Windows users that are allowed to connect to the historian.
    /// </summary>
    public ImmutableList<string> Users { get; } = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves data to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to which data is saved.</param>
    public override void Save(Stream stream)
    {
        // Write a byte with the value 1 to the stream.
        stream.Write((byte)1);
    }

    /// <summary>
    /// Loads data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream from which data is loaded.</param>
    public override void Load(Stream stream)
    {
        // Check if the object is editable.
        TestForEditable();

        // Read the next byte from the stream, which represents the version.
        byte version = stream.ReadNextByte();

        // Check if the version is not equal to 1 and throw an exception if it's unknown.
        if (version != 1)
            throw new VersionNotFoundException("Unknown Version Code: " + version);
    }

    /// <summary>
    /// Validates the object's state.
    /// </summary>
    public override void Validate()
    {
        // This method is currently empty and does not perform any specific validation.
    }

    #endregion
}