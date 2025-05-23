﻿//******************************************************************************************************
//  SimplifiedArchiveInitializerSettings.cs - Gbtc
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
//  10/18/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;
using SnapDB.IO;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Settings for <see cref="SimplifiedArchiveInitializer{TKey,TValue}"/>.
/// </summary>
public class SimplifiedArchiveInitializerSettings : SettingsBase<SimplifiedArchiveInitializerSettings>
{
    #region [ Members ]

    private long m_desiredRemainingSpace;
    private ArchiveDirectoryMethod m_directoryMethod;
    private EncodingDefinition m_encodingMethod;
    private string m_finalExtension;
    private string m_pendingExtension;
    private string m_prefix;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="ArchiveInitializerSettings"/>.
    /// </summary>
    public SimplifiedArchiveInitializerSettings()
    {
        Initialize();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The desired number of bytes to leave on the disk after a rollover has completed.
    /// Otherwise, pick a different directory or throw an out of disk space exception.
    /// </summary>
    /// <remarks>
    /// Value must be between 100MB and 1TB.
    /// </remarks>
    public long DesiredRemainingSpace
    {
        get => m_desiredRemainingSpace;
        set
        {
            TestForEditable();
            if (value < 100 * 1024L * 1024L)
                m_desiredRemainingSpace = 100 * 1024L * 1024L;

            if (value > 1024 * 1024L * 1024L * 1024L)
                m_desiredRemainingSpace = 1024 * 1024L * 1024L * 1024L;

            else
                m_desiredRemainingSpace = value;
        }
    }

    /// <summary>
    /// Gets the method that the directory structure will follow when writing a new file.
    /// </summary>
    public ArchiveDirectoryMethod DirectoryMethod
    {
        get => m_directoryMethod;
        set
        {
            TestForEditable();
            m_directoryMethod = value;
        }
    }

    /// <summary>
    /// The encoding method that will be used to write files.
    /// </summary>
    public EncodingDefinition EncodingMethod
    {
        get => m_encodingMethod;
        set
        {
            TestForEditable();

            m_encodingMethod = value ?? throw new ArgumentNullException(nameof(value));
        }
    }


    /// <summary>
    /// The extension to name the file.
    /// </summary>
    public string FinalExtension
    {
        get => m_finalExtension;
        set
        {
            TestForEditable();
            m_finalExtension = PathHelpers.FormatExtension(value);
        }
    }

    /// <summary>
    /// The flags that will be added to any created archive files.
    /// </summary>
    public ImmutableList<Guid> Flags { get; private set; }

    /// <summary>
    /// The extension to name the file.
    /// </summary>
    public string PendingExtension
    {
        get => m_pendingExtension;
        set
        {
            TestForEditable();
            m_pendingExtension = PathHelpers.FormatExtension(value);
        }
    }

    /// <summary>
    /// Gets or sets the file prefix. Can be String.Empty for no prefix.
    /// </summary>
    public string Prefix
    {
        get => m_prefix;
        set
        {
            TestForEditable();
            if (string.IsNullOrWhiteSpace(value))
            {
                m_prefix = string.Empty;

                return;
            }

            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("filename has invalid characters.", nameof(value));

            m_prefix = value;
        }
    }

    /// <summary>
    /// The list of all available paths to write files to.
    /// </summary>
    public ImmutableList<string> WritePath { get; private set; }

    /// <summary>
    /// Gets or sets the metadata to be written to the archive.
    /// </summary>
    public byte[]? Metadata { get; set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a <see cref="ArchiveInitializer{TKey,TValue}"/> that will reside on the disk.
    /// </summary>
    /// <param name="paths">The paths to place the files.</param>
    /// <param name="desiredRemainingSpace">The desired free space to leave on the disk before moving to another disk.</param>
    /// <param name="directoryMethod">The method for storing files in a directory.</param>
    /// <param name="encodingMethod">The encoding method to use for the archive file.</param>
    /// <param name="prefix">The prefix to affix to the files created.</param>
    /// <param name="pendingExtension">The extension file name.</param>
    /// <param name="finalExtension">The final extension to specify.</param>
    /// <param name="flags">Flags to include in the archive that is created.</param>
    public void ConfigureOnDisk(IEnumerable<string> paths, long desiredRemainingSpace, ArchiveDirectoryMethod directoryMethod, EncodingDefinition encodingMethod, string prefix, string pendingExtension, string finalExtension, params Guid[] flags)
    {
        TestForEditable();
        Initialize();
        DirectoryMethod = directoryMethod;
        PendingExtension = pendingExtension;
        FinalExtension = finalExtension;
        Flags.AddRange(flags);
        Prefix = prefix;
        WritePath.AddRange(paths);
        DesiredRemainingSpace = desiredRemainingSpace;
        EncodingMethod = encodingMethod;
    }

    /// <summary>
    /// Saves the configuration settings of the SnapServer to a stream.
    /// </summary>
    /// <param name="stream">The stream to which the configuration will be saved.</param>
    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write((int)m_directoryMethod);
        stream.Write(m_prefix);
        stream.Write(m_pendingExtension);
        stream.Write(m_finalExtension);
        stream.Write(m_desiredRemainingSpace);
        m_encodingMethod.Save(stream);
        stream.Write(WritePath.Count);

        foreach (string path in WritePath)
            stream.Write(path);

        stream.Write(Flags.Count);

        foreach (Guid flag in Flags)
            stream.Write(flag);
    }

    /// <summary>
    /// Loads the configuration of the archive settings from the specified stream.
    /// </summary>
    /// <param name="stream">The stream from which the configuration data will be loaded.</param>
    public override void Load(Stream stream)
    {
        TestForEditable();
        byte version = stream.ReadNextByte();

        switch (version)
        {
            case 1:
                m_directoryMethod = (ArchiveDirectoryMethod)stream.ReadInt32();
                m_prefix = stream.ReadString();
                m_pendingExtension = stream.ReadString();
                m_finalExtension = stream.ReadString();
                m_desiredRemainingSpace = stream.ReadInt64();
                m_encodingMethod = new EncodingDefinition(stream);
                int cnt = stream.ReadInt32();
                WritePath.Clear();

                while (cnt > 0)
                {
                    cnt--;
                    WritePath.Add(stream.ReadString());
                }

                cnt = stream.ReadInt32();
                Flags.Clear();

                while (cnt > 0)
                {
                    cnt--;
                    Flags.Add(stream.ReadGuid());
                }

                break;

            default:
                throw new VersionNotFoundException("Unknown Version Code: " + version);
        }
    }

    /// <summary>
    /// Validates the configuration of the archive settings.
    /// </summary>
    public override void Validate()
    {
        if (WritePath.Count == 0)
            throw new Exception("Missing write paths.");
    }

    private void Initialize()
    {
        m_directoryMethod = ArchiveDirectoryMethod.TopDirectoryOnly;
        m_prefix = string.Empty;
        m_pendingExtension = ".~d2i";
        m_finalExtension = ".d2i";
        m_desiredRemainingSpace = 5 * 1024 * 1024 * 1024L; // 5GB
        m_encodingMethod = EncodingDefinition.FixedSizeCombinedEncoding;
        WritePath = new ImmutableList<string>(x =>
        {
            PathHelpers.ValidatePathName(x);

            return x;
        });
        Flags = [];
    }

    #endregion
}