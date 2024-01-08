//******************************************************************************************************
//  ArchiveInitializerSettings.cs - Gbtc
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
using SnapDB.IO;

namespace SnapDB.Snap.Services.Writer;

public enum BalancingMethod
{
    /// <summary>
    /// Fills in order
    /// </summary>
    FillToDesired,
    /// <summary>
    /// Fills the one with the smallest available space
    /// </summary>
    FillSmallestAvailable,
    /// <summary>
    /// Fills the one with the most available space
    /// </summary>
    FillLargestAvailable,
    /// <summary>
    /// Fills the one with the largest total space
    /// </summary>
    FillLargestTotal,
    /// <summary>
    /// Fills to the same percentage across the board
    /// </summary>
    FillToMatchingPercentage
}

/// <summary>
/// Settings for <see cref="ArchiveInitializer{TKey,TValue}"/>.
/// </summary>
public class ArchiveInitializerSettings : SettingsBase<ArchiveInitializerSettings>
{
    #region [ Members ]

    private long m_desiredRemainingSpace;
    private BalancingMethod m_balancingMethod;
    private ArchiveDirectoryMethod m_directoryMethod;
    private EncodingDefinition m_encodingMethod;
    private string m_fileExtension;
    private bool m_isMemoryArchive;
    private string m_prefix;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="ArchiveInitializerSettings"/>.
    /// </summary>
    public ArchiveInitializerSettings()
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
    /// The method used to balance file share load.
    /// </summary>
    public BalancingMethod BalancingMethod
    {
        get => m_balancingMethod;
        set
        {
            TestForEditable();
            
            m_balancingMethod = value;
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
    public string FileExtension
    {
        get => m_fileExtension;
        set
        {
            TestForEditable();
            m_fileExtension = PathHelpers.FormatExtension(value);
        }
    }

    /// <summary>
    /// The flags that will be added to any created archive files.
    /// </summary>
    public ImmutableList<Guid> Flags { get; private set; }

    /// <summary>
    /// Gets if the archive file is a memory archive or a file archive.
    /// </summary>
    public bool IsMemoryArchive
    {
        get => m_isMemoryArchive;
        set
        {
            TestForEditable();
            m_isMemoryArchive = value;
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

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a <see cref="ArchiveInitializer{TKey,TValue}"/> that will reside in memory.
    /// </summary>
    /// <param name="encodingMethod">The encoding method to use for data storage.</param>
    /// <param name="flags">Additional flags to apply to the archive configuration.</param>
    /// <remarks>
    /// The <see cref="ConfigureInMemory"/> method allows configuring the archive to use an in-memory storage format.
    /// This method sets the <see cref="IsMemoryArchive"/> property to <c>true</c>, specifies the encoding method for
    /// data storage, and allows adding additional flags to the archive configuration. Once configured as an in-memory
    /// archive, data is stored in RAM instead of on disk, which can improve read/write performance at the cost of
    /// data persistence.
    /// </remarks>
    public void ConfigureInMemory(EncodingDefinition encodingMethod, params Guid[] flags)
    {
        TestForEditable();
        Initialize();
        IsMemoryArchive = true;
        Flags.AddRange(flags);
        EncodingMethod = encodingMethod;
    }

    /// <summary>
    /// Creates a <see cref="ArchiveInitializer{TKey,TValue}"/> that will reside on the disk.
    /// </summary>
    /// <param name="paths">The paths to place the files.</param>
    /// <param name="desiredRemainingSpace">The desired free space to leave on the disk before moving to another disk.</param>
    /// <param name="directoryMethod">The method for storing files in a directory.</param>
    /// <param name="encodingMethod">The encoding method to use for the archive file.</param>
    /// <param name="prefix">The prefix to affix to the files created.</param>
    /// <param name="extension">The extension file name.</param>
    /// <param name="flags">Flags to include in the archive that is created.</param>
    public void ConfigureOnDisk(IEnumerable<string> paths, long desiredRemainingSpace, ArchiveDirectoryMethod directoryMethod, EncodingDefinition encodingMethod, string prefix, string extension, params Guid[] flags)
    {
        TestForEditable();
        Initialize();
        IsMemoryArchive = false;
        DirectoryMethod = directoryMethod;
        FileExtension = extension;
        Flags.AddRange(flags);
        Prefix = prefix;
        WritePath.AddRange(paths);
        DesiredRemainingSpace = desiredRemainingSpace;
        EncodingMethod = encodingMethod;
    }

    /// <summary>
    /// Saves the configuration of an archive stream to the specified stream.
    /// </summary>
    /// <param name="stream">The stream where the configuration data will be saved.</param>
    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write((int)m_directoryMethod);
        stream.Write(m_isMemoryArchive);
        stream.Write(m_prefix);
        stream.Write(m_fileExtension);
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
    /// Loads the configuration of an archive stream from the specified stream.
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
                m_isMemoryArchive = stream.ReadBoolean();
                m_prefix = stream.ReadString();
                m_fileExtension = stream.ReadString();
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
    /// Validates the configuration of the archive stream.
    /// </summary>
    public override void Validate()
    {
        if (IsMemoryArchive)
            return;

        if (WritePath.Count == 0)
            throw new Exception("Missing write paths.");

        foreach (string path in WritePath)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Initializes the configuration of the archive stream with default values.
    /// </summary>
    private void Initialize()
    {
        m_directoryMethod = ArchiveDirectoryMethod.TopDirectoryOnly;
        m_isMemoryArchive = false;
        m_prefix = string.Empty;
        m_fileExtension = ".d2i";
        m_desiredRemainingSpace = 5 * 1024 * 1024 * 1024L; //5GB
        m_encodingMethod = EncodingDefinition.FixedSizeCombinedEncoding;
        m_balancingMethod = BalancingMethod.FillToDesired;
        WritePath = new ImmutableList<string>(x =>
        {
            PathHelpers.ValidatePathName(x);
            return x;
        });
        Flags = new ImmutableList<Guid>();
    }

    #endregion
}