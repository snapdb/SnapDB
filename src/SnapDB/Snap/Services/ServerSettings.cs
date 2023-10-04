//******************************************************************************************************
//  ServerSettings.cs - Gbtc
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
//  10/01/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;
using SnapDB.Snap.Services.Net;

namespace SnapDB.Snap.Services;

/// <summary>
/// Settings for <see cref="SnapServer"/>.
/// </summary>
public class ServerSettings : SettingsBase<ServerSettings>, IToServerSettings
{
    #region [ Members ]

    /// <summary>
    /// Lists all of the databases that are part of the server.
    /// </summary>
    private readonly ImmutableList<ServerDatabaseSettings> m_databases;

    /// <summary>
    /// All of the socket based listeners for the database.
    /// </summary>
    private readonly ImmutableList<SnapSocketListenerSettings> m_listeners;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new instance of <see cref="ServerSettings"/>.
    /// </summary>
    public ServerSettings()
    {
        m_databases = new ImmutableList<ServerDatabaseSettings>(x =>
        {
            if (x is null)
                throw new ArgumentNullException("value");

            return x;
        });
        m_listeners = new ImmutableList<SnapSocketListenerSettings>(x =>
        {
            if (x is null)
                throw new ArgumentNullException("value");

            return x;
        });
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Lists all of the databases that are part of the server.
    /// </summary>
    public ImmutableList<ServerDatabaseSettings> Databases => m_databases;

    /// <summary>
    /// Lists all of the socket based listeners for the database.
    /// </summary>
    public ImmutableList<SnapSocketListenerSettings> Listeners => m_listeners;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the current <see cref="ServerSettings"/> instance to a provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to which the settings will be saved.</param>
    /// <remarks>
    /// This method serializes the <see cref="ServerSettings"/> instance and writes it to the specified <paramref name="stream"/>.
    /// It includes information about databases and listeners within the server settings.
    /// </remarks>
    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write(m_databases.Count);

        foreach (ServerDatabaseSettings databaseSettings in m_databases)
            databaseSettings.Save(stream);

        stream.Write(m_listeners.Count);

        foreach (SnapSocketListenerSettings listenerSettings in m_listeners)
            listenerSettings.Save(stream);
    }

    /// <summary>
    /// Loads server settings from a given <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> containing the server settings to load.</param>
    /// <remarks>
    /// This method deserializes server settings from the provided <paramref name="stream"/> and updates the current instance.
    /// The method handles different versions of serialized data.
    /// </remarks>
    public override void Load(Stream stream)
    {
        TestForEditable();
        byte version = stream.ReadNextByte();

        switch (version)
        {
            case 1:
                int databaseCount = stream.ReadInt32();
                m_databases.Clear();

                while (databaseCount > 0)
                {
                    databaseCount--;
                    ServerDatabaseSettings database = new();
                    database.Load(stream);
                    m_databases.Add(database);
                }

                int listenerCount = stream.ReadInt32();
                m_listeners.Clear();

                while (listenerCount > 0)
                {
                    listenerCount--;
                    SnapSocketListenerSettings listener = new();
                    listener.Load(stream);
                    m_listeners.Add(listener);
                }

                break;

            default:
                throw new VersionNotFoundException("Unknown Version Code: " + version);
        }
    }

    /// <summary>
    /// Validates the server settings by validating each contained database and listener settings.
    /// </summary>
    /// <remarks>
    /// This method iterates through the databases and listener settings within the server settings
    /// and validates each of them by calling their respective <c>Validate</c> methods.
    /// </remarks>
    public override void Validate()
    {
        foreach (ServerDatabaseSettings db in m_databases)
            db.Validate();
        foreach (SnapSocketListenerSettings lst in m_listeners)
            lst.Validate();
    }

    /// <summary>
    /// Converts an instance of <see cref="ServerSettings"/> to its interface representation <see cref="IToServerSettings"/>.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ServerSettings"/> as an interface <see cref="IToServerSettings"/>.
    /// </returns>
    /// <remarks>
    /// This method allows the <see cref="ServerSettings"/> to be cast as an interface <see cref="IToServerSettings"/>.
    /// </remarks>
    ServerSettings IToServerSettings.ToServerSettings()
    {
        return this;
    }

    #endregion
}