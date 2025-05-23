﻿//******************************************************************************************************
//  SnapStreamingClient.cs - Gbtc
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

using System.Reflection;
using System.Runtime.CompilerServices;
using Gemstone.Diagnostics;
using Gemstone.IO.StreamExtensions;
using SnapDB.IO;
using SnapDB.Security;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace SnapDB.Snap.Services.Net;

/// <summary>
/// A client that communicates over a stream.
/// </summary>
public class SnapStreamingClient : SnapClient
{
    #region [ Members ]

    private SecureStreamClientBase m_credentials;
    private Dictionary<string, DatabaseInfo> m_databaseInfos;
    private string m_historianDatabaseString;
    private Stream m_rawStream;
    private Stream m_secureStream;
    private ClientDatabaseBase m_sortedTreeEngine;
    private RemoteBinaryStream m_stream;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="SnapStreamingClient"/>
    /// </summary>
    /// <param name="stream">The config to use for the client</param>
    /// <param name="credentials">Authenticates using the supplied user credentials.</param>
    /// <param name="useSsl">specifies if a ssl connection is desired.</param>
    public SnapStreamingClient(Stream stream, SecureStreamClientBase credentials, bool useSsl)
    {
        Initialize(stream, credentials, useSsl);
    }

    /// <summary>
    /// Allows derived classes to call <see cref="Initialize"/> after the inheriting class
    /// has done something in the constructor.
    /// </summary>
    protected SnapStreamingClient()
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SnapNetworkClient"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        try
        {
            if (!disposing)
                return;

            m_sortedTreeEngine?.Dispose();

            m_sortedTreeEngine = null;

            try
            {
                m_stream.Write((byte)ServerCommand.Disconnect);
                m_stream.Flush();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex);
            }

            m_rawStream?.Dispose();

            m_rawStream = null;
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
            base.Dispose(disposing); // Call base class Dispose().
        }
    }

    /// <summary>
    /// Gets the database that matches <paramref name="databaseName"/>.
    /// </summary>
    /// <param name="databaseName">The name of the database to retrieve.</param>
    /// <returns>A client database instance for the specified database name.</returns>
    public override ClientDatabaseBase GetDatabase(string databaseName)
    {
        DatabaseInfo info = m_databaseInfos[databaseName.ToUpper()];
        Type type = typeof(SnapStreamingClient);
        MethodInfo method = type.GetMethod("InternalGetDatabase", BindingFlags.NonPublic | BindingFlags.Instance);

        // ReSharper disable once PossibleNullReferenceException
        MethodInfo reflectionMethod = method.MakeGenericMethod(info.KeyType, info.ValueType);
        ClientDatabaseBase db = (ClientDatabaseBase)reflectionMethod.Invoke(this, [databaseName]);

        return db;
    }

    /// <summary>
    /// Accesses <see cref="ClientDatabaseBase{TKey, TValue}"/> for given <paramref name="databaseName"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of key to get.</typeparam>
    /// <typeparam name="TValue">The type of value associated with the key being acquired.</typeparam>
    /// <param name="databaseName">The name of the database to access.</param>
    /// <returns><see cref="ClientDatabaseBase{TKey, TValue}"/> for given <paramref name="databaseName"/>.</returns>
    public override ClientDatabaseBase<TKey, TValue> GetDatabase<TKey, TValue>(string databaseName)
    {
        return GetDatabase<TKey, TValue>(databaseName, null);
    }

    /// <summary>
    /// Gets basic information for every database connected to the server.
    /// </summary>
    /// <returns>A list of <see cref="DatabaseInfo"/> objects representing available databases.</returns>
    /// <remarks>
    /// This method retrieves a list of database information objects for all available databases.
    /// The database information objects contain details about each database, such as its name, key type, and value type.
    /// </remarks>
    public override List<DatabaseInfo> GetDatabaseInfo()
    {
        return m_databaseInfos.Values.ToList();
    }

    /// <summary>
    /// Determines if <paramref name="databaseName"/> is contained in the database.
    /// </summary>
    /// <param name="databaseName">Name of database instance to access.</param>
    /// <returns>
    /// <c>true</c> if a database with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks whether a database with the specified name exists in the client.
    /// It performs a case-insensitive comparison of the database name.
    /// </remarks>
    public override bool Contains(string databaseName)
    {
        return m_databaseInfos.ContainsKey(databaseName.ToUpper());
    }

    /// <summary>
    /// Creates a <see cref="SnapStreamingClient"/>
    /// </summary>
    /// <param name="stream">The config to use for the client</param>
    /// <param name="credentials">Authenticates using the supplied user credentials.</param>
    /// <param name="useSsl">Specifies if an SSL connection is desired.</param>
    protected void Initialize(Stream stream, SecureStreamClientBase credentials, bool useSsl)
    {
        m_credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        m_rawStream = stream ?? throw new ArgumentNullException(nameof(stream));
        m_rawStream.Write(0x2BA517361121L);
        m_rawStream.Write(useSsl); // UseSSL

        ServerResponse command = (ServerResponse)m_rawStream.ReadNextByte();

        switch (command)
        {
            case ServerResponse.UnknownProtocol:
                throw new Exception("Client and server cannot agree on a protocol, this is commonly because they are running incompatible versions.");
            case ServerResponse.KnownProtocol:
                break;
            default:
                throw new Exception($"Unknown server response: {command}");
        }

        useSsl = m_rawStream.ReadBoolean();

        if (!m_credentials.TryAuthenticate(m_rawStream, useSsl, out m_secureStream))
            throw new Exception("Authentication Failed");

        m_stream = new RemoteBinaryStream(m_secureStream);

        command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception($"Server UnhandledException: \n{exception}");
            case ServerResponse.UnknownProtocol:
                throw new Exception("Client and server cannot agree on a protocol, this is commonly because they are running incompatible versions.");
            case ServerResponse.ConnectedToRoot:
                break;
            default:
                throw new Exception($"Unknown server response: {command}");
        }

        RefreshDatabaseInfo();
    }

    private void RefreshDatabaseInfo()
    {
        m_stream.Write((byte)ServerCommand.GetAllDatabases);
        m_stream.Flush();

        ServerResponse command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception($"Server UnhandledException: \n{exception}");
            case ServerResponse.ListOfDatabases:
                int count = m_stream.ReadInt32();
                Dictionary<string, DatabaseInfo> dict = new();

                while (count > 0)
                {
                    count--;
                    DatabaseInfo info = new(m_stream);
                    dict.Add(info.DatabaseName.ToUpper(), info);
                }

                m_databaseInfos = dict;
                break;
            default:
                throw new Exception($"Unknown server response: {command}");
        }
    }

    //Called through reflection. Its the only way to call a generic function only knowing the Types
    [MethodImpl(MethodImplOptions.NoOptimization)] //Prevents removing this method as it may appear unused.
    private ClientDatabaseBase<TKey, TValue> InternalGetDatabase<TKey, TValue>(string databaseName) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return GetDatabase<TKey, TValue>(databaseName, null);
    }

    /// <summary>
    /// Accesses <see cref="StreamingClientDatabase{TKey,TValue}"/> for given <paramref name="databaseName"/>.
    /// </summary>
    /// <param name="databaseName">Name of database instance to access.</param>
    /// <param name="encodingMethod"></param>
    /// <returns><see cref="StreamingClientDatabase{TKey,TValue}"/> for given <paramref name="databaseName"/>.</returns>
    private StreamingClientDatabase<TKey, TValue> GetDatabase<TKey, TValue>(string databaseName, EncodingDefinition encodingMethod) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        DatabaseInfo dbInfo = m_databaseInfos[databaseName.ToUpper()];

        encodingMethod ??= dbInfo.SupportedStreamingModes.First();

        if (m_sortedTreeEngine is not null)
            throw new Exception($"Can only connect to one database at a time. Please disconnect from database{m_historianDatabaseString}");

        if (dbInfo.KeyType != typeof(TKey))
            throw new InvalidCastException("Key types do not match");

        if (dbInfo.ValueType != typeof(TValue))
            throw new InvalidCastException("Value types do not match");

        m_stream.Write((byte)ServerCommand.ConnectToDatabase);
        m_stream.Write(databaseName);
        m_stream.Write(new TKey().GenericTypeGuid);
        m_stream.Write(new TValue().GenericTypeGuid);
        m_stream.Flush();

        ServerResponse command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception($"Server UnhandledException: \n{exception}");
            case ServerResponse.DatabaseDoesNotExist:
                throw new Exception($"Database does not exist on the server: {databaseName}");
            case ServerResponse.DatabaseKeyUnknown:
                throw new Exception("Database key does not match that passed to this function");
            case ServerResponse.DatabaseValueUnknown:
                throw new Exception("Database value does not match that passed to this function");
            case ServerResponse.SuccessfullyConnectedToDatabase:
                break;
            default:
                throw new Exception($"Unknown server response: {command}");
        }

        StreamingClientDatabase<TKey, TValue> db = new(m_stream, () => m_sortedTreeEngine = null, dbInfo);
        m_sortedTreeEngine = db;
        m_historianDatabaseString = databaseName;

        db.SetEncodingMode(encodingMethod);

        return db;
    }

    #endregion
}