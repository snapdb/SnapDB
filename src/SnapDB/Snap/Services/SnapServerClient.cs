//******************************************************************************************************
//  SnapServerClient.cs - Gbtc
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
//  04/19/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap.Services;

public partial class SnapServer
{
    #region [ Members ]

    /// <summary>
    /// A client wrapper around a <see cref="SnapServer"/>. This protects
    /// the server from a client being able to manipulate it.
    /// (For example. Call the IDispose.Dispose method)
    /// </summary>
    internal class Client : SnapClient
    {
        #region [ Members ]

        private readonly Dictionary<string, ClientDatabaseBase> m_connectedDatabases;
        private SnapServer m_server;
        private readonly object m_syncRoot;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a <see cref="Client"/>
        /// </summary>
        /// <param name="server">The collection to wrap.</param>
        public Client(SnapServer server)
        {
            m_syncRoot = new object();
            m_connectedDatabases = new Dictionary<string, ClientDatabaseBase>();
            m_server = server ?? throw new ArgumentNullException(nameof(server));
            server.RegisterClient(this);
        }

        #endregion

        #region [ Methods ]

        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
                if (disposing)
                {
                    lock (m_syncRoot)
                    {
                        // Must include .ToArray because calling dispose will remove the item from the m_coonectionDatabase via a callback.
                        foreach (ClientDatabaseBase db in m_connectedDatabases.Values.ToArray())
                            db.Dispose();
                    }

                    m_server.UnRegisterClient(this);
                    m_server = null;
                    m_disposed = true;
                }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the database that matches <see cref="databaseName"/>.
        /// </summary>
        /// <param name="databaseName">The case insensitive name of the database.</param>
        /// <returns>The database that matches <see cref="databaseName"/>.</returns>
        public override ClientDatabaseBase GetDatabase(string databaseName)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            databaseName = databaseName.ToUpper();

            ClientDatabaseBase database;
            lock (m_syncRoot)
            {
                if (!m_connectedDatabases.TryGetValue(databaseName, out database))
                {
                    SnapServerDatabaseBase serverDb = m_server.GetDatabase(databaseName);
                    database = serverDb.CreateClientDatabase(this, Unregister);
                    m_connectedDatabases.Add(databaseName, database);
                }
            }

            return database;
        }

        /// <summary>
        /// Accesses <see cref="ClientDatabaseBase{TKey,TValue}"/> for given <paramref name="databaseName"/>.
        /// </summary>
        /// <param name="databaseName">Name of database instance to access.</param>
        /// <returns><see cref="ClientDatabaseBase{TKey,TValue}"/> for given <paramref name="databaseName"/>.</returns>
        public override ClientDatabaseBase<TKey, TValue> GetDatabase<TKey, TValue>(string databaseName)
        {
            return (ClientDatabaseBase<TKey, TValue>)GetDatabase(databaseName);
        }

        /// <summary>
        /// Determines if <see cref="databaseName"/> is contained in the database.
        /// </summary>
        /// <param name="databaseName">Name of database instance to access.</param>
        /// <returns>
        /// <c>true</c> if the database with the specified name exists within the server; otherwise, <c>false</c>.
        /// </returns>
        public override bool Contains(string databaseName)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            return m_server.Contains(databaseName);
        }

        /// <summary>
        /// Gets basic information for every database connected to the server.
        /// </summary>
        /// <returns>A list of <see cref="DatabaseInfo"/> objects representing database information.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the server settings object has been disposed.</exception>
        public override List<DatabaseInfo> GetDatabaseInfo()
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            return m_server.GetDatabaseInfo();
        }

        /// <summary>
        /// Unregisters a client database.
        /// </summary>
        /// <param name="client">The client database to unregister.</param>
        private void Unregister(ClientDatabaseBase client)
        {
            lock (m_syncRoot)
            {
                m_connectedDatabases.Remove(client.Info.DatabaseName.ToUpper());
            }
        }

        #endregion
    }

    #endregion
}