//******************************************************************************************************
//  ServerDatabaseSettings.cs - Gbtc
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
//  10/04/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;
using SnapDB.Snap.Services.Writer;

namespace SnapDB.Snap.Services;

/// <summary>
/// The settings for a <see cref="SnapServerDatabase{TKey,TValue}"/>.
/// </summary>
public class ServerDatabaseSettings : SettingsBase<ServerDatabaseSettings>, IToServerDatabaseSettings
{
    #region [ Members ]

    private string m_databaseName;
    private Guid m_keyType;
    private bool m_supportsWriting;
    private Guid m_valueType;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="ServerDatabaseSettings"/>.
    /// </summary>
    public ServerDatabaseSettings()
    {
        m_databaseName = string.Empty;
        ArchiveList = new ArchiveListSettings();
        WriteProcessor = new WriteProcessorSettings();
        RolloverLog = new RolloverLogSettings();
        m_keyType = Guid.Empty;
        m_valueType = Guid.Empty;
        StreamingEncodingMethods = new ImmutableList<EncodingDefinition>(x =>
        {
            if (x is null)
                throw new ArgumentNullException("value");

            return x;
        });
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The settings for the ArchiveList.
    /// </summary>
    public ArchiveListSettings ArchiveList { get; }

    /// <summary>
    /// The name associated with the database.
    /// </summary>
    public string DatabaseName
    {
        get => m_databaseName;
        set
        {
            TestForEditable();
            m_databaseName = value;
        }
    }

    /// <summary>
    /// Gets the type of the key component.
    /// </summary>
    public Guid KeyType
    {
        get => m_keyType;
        set
        {
            TestForEditable();
            m_keyType = value;
        }
    }

    /// <summary>
    /// The settings for the rollover log.
    /// </summary>
    public RolloverLogSettings RolloverLog { get; }

    /// <summary>
    /// Gets the supported streaming methods.
    /// </summary>
    public ImmutableList<EncodingDefinition> StreamingEncodingMethods { get; }

    /// <summary>
    /// Gets if writing or file combination will be enabled.
    /// </summary>
    public bool SupportsWriting
    {
        get => m_supportsWriting;
        set
        {
            TestForEditable();
            m_supportsWriting = value;
        }
    }

    /// <summary>
    /// Gets the type of the value component.
    /// </summary>
    public Guid ValueType
    {
        get => m_valueType;
        set
        {
            TestForEditable();
            m_valueType = value;
        }
    }

    /// <summary>
    /// Settings for the writer -- <c>null</c> if the server does not support writing.
    /// </summary>
    public WriteProcessorSettings WriteProcessor { get; }

    #endregion

    #region [ Methods ]

    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write(m_keyType);
        stream.Write(m_valueType);
        stream.Write(m_databaseName);
        stream.Write(StreamingEncodingMethods.Count);

        foreach (EncodingDefinition path in StreamingEncodingMethods)
            path.Save(stream);

        ArchiveList.Save(stream);
        WriteProcessor.Save(stream);
        RolloverLog.Save(stream);
    }

    public override void Load(Stream stream)
    {
        TestForEditable();
        byte version = stream.ReadNextByte();

        switch (version)
        {
            case 1:
                m_keyType = stream.ReadGuid();
                m_valueType = stream.ReadGuid();
                m_databaseName = stream.ReadString();
                int cnt = stream.ReadInt32();
                StreamingEncodingMethods.Clear();

                while (cnt > 0)
                {
                    cnt--;
                    StreamingEncodingMethods.Add(new EncodingDefinition(stream));
                }

                ArchiveList.Load(stream);
                WriteProcessor.Load(stream);
                RolloverLog.Load(stream);

                break;

            default:
                throw new VersionNotFoundException("Unknown Version Code: " + version);
        }
    }

    public override void Validate()
    {
        ArchiveList.Validate();
        RolloverLog.Validate();

        if (m_supportsWriting)
            WriteProcessor.Validate();

        if (StreamingEncodingMethods.Count == 0)
            throw new Exception("Must specify a streaming method");
    }

    ServerDatabaseSettings IToServerDatabaseSettings.ToServerDatabaseSettings()
    {
        return this;
    }

    #endregion
}