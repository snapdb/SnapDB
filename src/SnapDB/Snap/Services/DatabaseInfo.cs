﻿//******************************************************************************************************
//  DatabaseInfo.cs - Gbtc
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
//  03/01/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections.ObjectModel;
using System.Data;
using SnapDB.IO;

namespace SnapDB.Snap.Services;

/// <summary>
/// Contains a basic set of data for a <see cref="ClientDatabaseBase{TKey,TValue}"/>.
/// </summary>
public class DatabaseInfo
{
    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="DatabaseInfo"/>
    /// </summary>
    /// <param name="databaseName">the name of the database</param>
    /// <param name="key">the key type</param>
    /// <param name="value">the value type</param>
    /// <param name="supportedStreamingModes"></param>
    public DatabaseInfo(string databaseName, SnapTypeBase key, SnapTypeBase value, IList<EncodingDefinition> supportedStreamingModes)
    {
        DatabaseName = databaseName;
        KeyTypeId = key.GenericTypeGuid;
        KeyType = key.GetType();
        ValueTypeId = value.GenericTypeGuid;
        ValueType = value.GetType();
        SupportedStreamingModes = new ReadOnlyCollection<EncodingDefinition>(supportedStreamingModes);
    }

    /// <summary>
    /// Loads a <see cref="DatabaseInfo"/> from stream.
    /// </summary>
    /// <param name="stream"></param>
    public DatabaseInfo(BinaryStreamBase stream)
    {
        byte version = stream.ReadUInt8();
        switch (version)
        {
            case 1:
                DatabaseName = stream.ReadString();
                KeyTypeId = stream.ReadGuid();
                ValueTypeId = stream.ReadGuid();
                int count = stream.ReadInt32();
                EncodingDefinition[] definitions = new EncodingDefinition[count];
                for (int x = 0; x < count; x++)
                    definitions[x] = new EncodingDefinition(stream);
                SupportedStreamingModes = new ReadOnlyCollection<EncodingDefinition>(definitions);
                KeyType = Library.GetSortedTreeType(KeyTypeId);
                ValueType = Library.GetSortedTreeType(ValueTypeId);
                break;
            default:
                throw new VersionNotFoundException("Unknown version code.");
        }
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the name of the database
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Gets the type for the database key.
    /// </summary>
    public Type KeyType { get; private set; }

    /// <summary>
    /// Gets the ID for the database key.
    /// </summary>
    public Guid KeyTypeId { get; }

    /// <summary>
    /// Gets all of the supported streaming modes for the server.
    /// </summary>
    public ReadOnlyCollection<EncodingDefinition> SupportedStreamingModes { get; }

    /// <summary>
    /// Gets the type for the database value.
    /// </summary>
    public Type ValueType { get; private set; }

    /// <summary>
    /// Gets the ID for the database value.
    /// </summary>
    public Guid ValueTypeId { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the configuration of the database to the specified binary stream.
    /// </summary>
    /// <param name="stream">The binary stream where the database configuration data will be saved.</param>
    public void Save(BinaryStreamBase stream)
    {
        stream.Write((byte)1);
        stream.Write(DatabaseName);
        stream.Write(KeyTypeId);
        stream.Write(ValueTypeId);
        stream.Write(SupportedStreamingModes.Count);
        foreach (EncodingDefinition encoding in SupportedStreamingModes)
            encoding.Save(stream);
    }

    #endregion
}