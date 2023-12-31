﻿//******************************************************************************************************
//  ArchiveListLogSettings.cs - Gbtc
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
using SnapDB.IO;

namespace SnapDB.Snap.Services;

/// <summary>
/// The settings for an archive list log.
/// </summary>
public class ArchiveListLogSettings : SettingsBase<ArchiveListLogSettings>
{
    #region [ Members ]

    private string m_logFileExtension = ".ArchiveListLog";
    private string m_logFilePrefix = "PendingDeletionLog";
    private string m_logPath = string.Empty;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets if this archive log will be file backed.
    /// This is true as long as <see cref="LogPath"/> is assigned
    /// a value.
    /// </summary>
    public bool IsFileBacked => m_logPath != string.Empty;

    /// <summary>
    /// The file extension to write the log files.
    /// </summary>
    public string LogFileExtension
    {
        get => m_logFileExtension;
        set
        {
            TestForEditable();
            m_logFileExtension = PathHelpers.FormatExtension(value);
        }
    }

    /// <summary>
    /// The prefix to assign to all log files. Can be string.empty
    /// </summary>
    public string LogFilePrefix
    {
        get => m_logFilePrefix;
        set
        {
            TestForEditable();
            if (string.IsNullOrWhiteSpace(value))
            {
                m_logFilePrefix = string.Empty;
                return;
            }

            PathHelpers.ValidatePathName(value);
            m_logFilePrefix = value;
        }
    }

    /// <summary>
    /// The path to store all log files. Can be an empty string to
    /// not enable file based logging.
    /// </summary>
    public string LogPath
    {
        get => m_logPath;
        set
        {
            TestForEditable();
            if (string.IsNullOrWhiteSpace(value))
            {
                m_logPath = string.Empty;
                return;
            }

            PathHelpers.ValidatePathName(value);
            m_logPath = value;
        }
    }

    /// <summary>
    /// Gets the wildcard search string for a log file.
    /// </summary>
    internal string SearchPattern
    {
        get
        {
            if (LogFilePrefix == string.Empty)
                return "*" + LogFileExtension;
            return LogFilePrefix + " *" + LogFileExtension;
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the configuration of the log settings to the specified stream.
    /// </summary>
    /// <param name="stream">The stream where the configuration data will be saved.</param>
    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write(m_logPath);
        stream.Write(m_logFilePrefix);
        stream.Write(m_logFileExtension);
    }

    /// <summary>
    /// Loads the configuration of the log settings from the specified stream.
    /// </summary>
    /// <param name="stream">The stream from which the configuration data will be loaded.</param>
    public override void Load(Stream stream)
    {
        TestForEditable();
        byte version = stream.ReadNextByte();
        switch (version)
        {
            case 1:
                m_logPath = stream.ReadString();
                m_logFilePrefix = stream.ReadString();
                m_logFileExtension = stream.ReadString();
                break;
            default:
                throw new VersionNotFoundException("Unknown Version Code: " + version);
        }
    }

    /// <summary>
    /// Validates the configuration of the log settings.
    /// </summary>
    public override void Validate()
    {
        if (m_logPath != string.Empty && !Directory.Exists(m_logPath))
            Directory.CreateDirectory(m_logPath);
    }

    /// <summary>
    /// Generates a new file name.
    /// </summary>
    /// <returns>A new unique file name for the log.</returns>
    /// <exception cref="Exception">Thrown if the log is not file-backed.</exception>
    internal string GenerateNewFileName()
    {
        if (!IsFileBacked)
            throw new Exception("Cannot generate a file name when the log is not a file backed log");

        if (LogFilePrefix == string.Empty)
            return Path.Combine(LogPath, Guid.NewGuid() + LogFileExtension);

        return Path.Combine(LogPath, LogFilePrefix + " " + Guid.NewGuid() + LogFileExtension);
    }

    #endregion
}