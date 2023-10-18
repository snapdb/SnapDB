//******************************************************************************************************
//  SnapClient.cs - Gbtc
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
//  05/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.Diagnostics;
using SnapDB.Snap.Services.Net;

namespace SnapDB.Snap.Services;

/// <summary>
/// Represents a client connection to a <see cref="SnapServer"/>.
/// </summary>
public abstract class SnapClient : DisposableLoggingClassBase
{
    #region [ Constructors ]
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapClient"/> class.
    /// </summary>
    protected SnapClient() : base(MessageClass.Framework)
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Gets the database that matches <paramref name="databaseName"/>.
    /// </summary>
    /// <param name="databaseName">The case insensitive name of the database to retrieve.</param>
    /// <returns>
    /// A <see cref="ClientDatabaseBase"/> instance representing the requested client database.
    /// </returns>
    /// <remarks>
    /// The <see cref="GetDatabase"/> method retrieves a client database with the specified <paramref name="databaseName"/>.
    /// If a database with the specified name does not exist, this method may return null or throw an exception.
    /// </remarks>
    public abstract ClientDatabaseBase GetDatabase(string databaseName);

    /// <summary>
    /// Accesses <see cref="ClientDatabaseBase{TKey,TValue}"/> for given <paramref name="databaseName"/>.
    /// </summary>
    /// <param name="databaseName">Name of database instance to access.</param>
    /// <typeparam name="TKey">The type of key to access.</typeparam>
    /// <typeparam name="TValue">The type of value associated with the key to access.</typeparam>
    /// <returns><see cref="ClientDatabaseBase{TKey,TValue}"/> for given <paramref name="databaseName"/>.</returns>
    public abstract ClientDatabaseBase<TKey, TValue> GetDatabase<TKey, TValue>(string databaseName) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new();

    /// <summary>
    /// Gets basic information for every database connected to the server.
    /// </summary>
    /// <returns>
    /// A list of <see cref="DatabaseInfo"/> objects containing information about available databases.
    /// </returns>
    /// <remarks>
    /// The <see cref="GetDatabaseInfo"/> method provides a list of <see cref="DatabaseInfo"/> objects
    /// that contain information about the available databases in the system.
    /// Each <see cref="DatabaseInfo"/> object typically includes details such as database name, key type,
    /// value type, and other relevant metadata.
    /// </remarks>
    /// <seealso cref="DatabaseInfo"/>
    public abstract List<DatabaseInfo> GetDatabaseInfo();

    /// <summary>
    /// Determines if <paramref name="databaseName"/> is contained in the database.
    /// </summary>
    /// <param name="databaseName">The name of the database to check for existence.</param>
    /// <returns>
    /// <c>true</c> if a database with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The <see cref="Contains"/> method allows you to check whether a database with the specified
    /// <paramref name="databaseName"/> exists within the system. It returns <c>true</c> if a database
    /// with the provided name is found; otherwise, it returns <c>false</c>.
    /// </remarks>
    public abstract bool Contains(string databaseName);

    #endregion

    #region [ Static ]

    /// <summary>
    /// Connects to a local <see cref="SnapServer"/>.
    /// </summary>
    /// <param name="host">The SnapServer host to connect to.</param>
    /// <returns>
    /// A <see cref="SnapClient"/> instance representing the connection to the specified SnapServer host.
    /// </returns>
    /// <remarks>
    /// The Connect method allows you to establish a connection to a SnapServer host.
    /// You should provide the <paramref name="host"/> as a parameter, and it returns a <see cref="SnapClient"/>
    /// instance that you can use to interact with the server.
    /// </remarks>
    public static SnapClient Connect(SnapServer host)
    {
        return new SnapServer.Client(host);
    }

    /// <summary>
    /// Connects to a server over a network socket.
    /// </summary>
    /// <param name="serverOrIP">The name of the server to connect to, or the IP address to use.</param>
    /// <param name="port">The port number to connect to.</param>
    /// <returns>A <see cref="SnapClient"/></returns>
    public static SnapClient Connect(string serverOrIP, int port)
    {
        SnapNetworkClientSettings settings = new()
        {
            ServerNameOrIP = serverOrIP,
            NetworkPort = port
        };

        return new SnapNetworkClient(settings);
    }

    #endregion
}