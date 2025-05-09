﻿//******************************************************************************************************
//  SnapServerDatabaseBase.cs - Gbtc
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
//  12/8/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Gemstone.Diagnostics;

namespace SnapDB.Snap.Services;

/// <summary>
/// Represents the server side of a single database.
/// </summary>
public abstract class SnapServerDatabaseBase : DisposableLoggingClassBase
{
    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="SnapServerDatabaseBase"/>
    /// </summary>
    protected SnapServerDatabaseBase() : base(MessageClass.Framework)
    {
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets basic information about the current Database.
    /// </summary>
    public abstract DatabaseInfo Info { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a <see cref="ClientDatabaseBase"/>.
    /// </summary>
    /// <param name="client">The SnapClient associated with the new client-specific database.</param>
    /// <param name="onDispose">An action to be executed when the client-specific database is disposed.</param>
    /// <returns>
    /// A new instance of the client-specific database derived from <see cref="ClientDatabaseBase"/>.
    /// </returns>
    /// <remarks>
    /// The <see cref="CreateClientDatabase"/> method is used to create a new instance of a client-specific database
    /// associated with this SnapServerDatabase. It takes a SnapClient as a parameter to associate the new database with
    /// a specific client. Additionally, an optional action can be provided to be executed when the client-specific database is disposed.
    /// </remarks>
    public abstract ClientDatabaseBase CreateClientDatabase(SnapClient client, Action<ClientDatabaseBase> onDispose);

    /// <summary>
    /// Gets the full status text for the server.
    /// </summary>
    /// <param name="status">Target status output <see cref="StringBuilder"/>.</param>
    /// <param name="maxFileListing">Maximum file listing.</param>
    public abstract void GetFullStatus(StringBuilder status, int maxFileListing = -1);

    /// <summary>
    /// Retrieves source details for logging purposes, including database name, key type, and value type.
    /// </summary>
    /// <returns>A <see cref="LogStackMessages"/> object containing source details if available; otherwise, an empty object.</returns>
    protected LogStackMessages GetSourceDetails()
    {
        return Info is not null ? LogStackMessages.Empty.Union("Database", $"Database: {Info.DatabaseName} Key: {Info.KeyType.FullName} Value: {Info.ValueType.FullName}") : LogStackMessages.Empty;
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a new server database from the provided config.
    /// </summary>
    /// <param name="databaseConfig">The configuration settings for the database.</param>
    /// <returns>
    /// A new instance of SnapServerDatabaseBase derived from <see cref="SnapServerDatabaseBase"/>.
    /// </returns>
    /// <remarks>
    /// The <see cref="CreateDatabase"/> method is used to create a new instance of a SnapServerDatabaseBase
    /// with the specified configuration settings. It accepts a <see cref="ServerDatabaseSettings"/> object
    /// containing configuration parameters such as key and value types, and returns a new instance of the database.
    /// </remarks>
    public static SnapServerDatabaseBase CreateDatabase(ServerDatabaseSettings databaseConfig)
    {
        Type keyType = Library.GetSortedTreeType(databaseConfig.KeyType);
        Type valueType = Library.GetSortedTreeType(databaseConfig.ValueType);

        Type type = typeof(SnapServerDatabaseBase);
        MethodInfo method = type.GetMethod("CreateDatabase", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo reflectionMethod = method?.MakeGenericMethod(keyType, valueType);

        return (SnapServerDatabaseBase)reflectionMethod?.Invoke(null, [databaseConfig]);
    }

    // Called through reflection. Its the only way to call a generic function only knowing the Types
    [MethodImpl(MethodImplOptions.NoOptimization)] // Prevents removing this method as it may appear unused.
    private static SnapServerDatabaseBase CreateDatabase<TKey, TValue>(ServerDatabaseSettings databaseConfig) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return new SnapServerDatabase<TKey, TValue>(databaseConfig);
    }

    #endregion
}