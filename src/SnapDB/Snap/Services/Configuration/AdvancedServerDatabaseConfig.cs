﻿//******************************************************************************************************
//  AdvancedServerDatabaseConfig.cs - Gbtc
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
//  10/05/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  11/25/2014 - J. Ritchie Carroll
//       Updated final staging file name to use database name as prefix instead of "stage(n)".
//
//  10/15/2019 - J. Ritchie Carroll
//       Added DesiredRemainingSpace property for configurable target disk remaining space for
//       final staging files / general code cleanup.
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.StringExtensions;
using Gemstone.Units;
using SnapDB.IO;
using SnapDB.Snap.Services.Writer;
using SnapDB.Snap.Storage;

namespace SnapDB.Snap.Services.Configuration;

/// <summary>
/// Creates a configuration for the database to utilize.
/// </summary>
/// <typeparam name="TKey">The key type used in the sorted tree table.</typeparam>
/// <typeparam name="TValue">The value type used in the sorted tree table.</typeparam>
public class AdvancedServerDatabaseConfig<TKey, TValue> : IToServerDatabaseSettings where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private string m_finalFileExtension;
    private string m_intermediateFileExtension;

    private readonly string m_mainPath;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the AdvancedServerDatabaseConfig class with the specified configuration options.
    /// </summary>
    /// <param name="databaseName">The name of the database.</param>
    /// <param name="mainPath">The main path associated with the database.</param>
    /// <param name="supportsWriting">A flag indicating whether the database supports writing.</param>
    public AdvancedServerDatabaseConfig(string databaseName, string mainPath, bool supportsWriting)
    {
        SupportsWriting = supportsWriting;
        DatabaseName = databaseName;
        m_mainPath = mainPath;
        m_intermediateFileExtension = ".d2i";
        m_finalFileExtension = ".d2";
        ImportAttachedPathsAtStartup = true;
        ImportPaths = [];
        FinalWritePaths = [];
        ArchiveEncodingMethod = EncodingDefinition.FixedSizeCombinedEncoding;
        StreamingEncodingMethods = [];
        TargetFileSize = 2L * SI2.Giga;
        DesiredRemainingSpace = 5L * SI2.Giga;
        StagingCount = 3;
        DirectoryMethod = ArchiveDirectoryMethod.TopDirectoryOnly;
        DiskFlushInterval = 10000;
        CacheFlushInterval = 100;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the default encoding methods for storing files.
    /// </summary>
    public EncodingDefinition ArchiveEncodingMethod { get; set; }

    /// <summary>
    /// The number of milliseconds before data is taken from it's cache and put in the
    /// memory file.
    /// </summary>
    /// <remarks>
    /// Must be between 1 and 1,000
    /// </remarks>
    public int CacheFlushInterval { get; set; }

    /// <summary>
    /// The name associated with the database.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Gets or sets the desired remaining drive space, in bytes, for final stage files.
    /// </summary>
    /// <remarks>
    /// Value must be between 100MB and 1TB.
    /// </remarks>
    public long DesiredRemainingSpace { get; set; }

    /// <summary>
    /// Gets the method of how the directory will be stored. Defaults to
    /// top directory only.
    /// </summary>
    public ArchiveDirectoryMethod DirectoryMethod { get; set; }

    /// <summary>
    /// The number of milliseconds before data is automatically flushed to the disk.
    /// </summary>
    /// <remarks>
    /// Must be between 1,000 ms and 60,000 ms.
    /// </remarks>
    public int DiskFlushInterval { get; set; }

    /// <summary>
    /// The extension to use for the final file
    /// </summary>
    public string FinalFileExtension
    {
        get => m_finalFileExtension;
        set => m_finalFileExtension = PathHelpers.FormatExtension(value);
    }

    /// <summary>
    /// The list of directories where final files can be placed written.
    /// If nothing is specified, the main directory is used.
    /// </summary>
    public List<string> FinalWritePaths { get; }

    /// <summary>
    /// Determines whether the server should import attached paths at startup.
    /// </summary>
    public bool ImportAttachedPathsAtStartup { get; set; }

    /// <summary>
    /// Gets all of the paths that are known by this historian.
    /// A path can be a file name or a folder.
    /// </summary>
    public List<string> ImportPaths { get; }

    /// <summary>
    /// The extension to use for the intermediate files
    /// </summary>
    public string IntermediateFileExtension
    {
        get => m_intermediateFileExtension;
        set => m_intermediateFileExtension = PathHelpers.FormatExtension(value);
    }

    /// <summary>
    /// The number of stages.
    /// </summary>
    public int StagingCount { get; set; }

    /// <summary>
    /// Gets the supported encoding methods for streaming data. This list is in a prioritized order.
    /// </summary>
    public List<EncodingDefinition> StreamingEncodingMethods { get; }

    /// <summary>
    /// Gets if writing will be supported
    /// </summary>
    public bool SupportsWriting { get; }

    /// <summary>
    /// Gets or sets the desired size of the final stage archive files.
    /// </summary>
    /// <remarks>
    /// Value must be between 100MB and 1TB.
    /// </remarks>
    public long TargetFileSize { get; set; }

    /// <summary>
    /// Gets or sets the metadata to be written to the archive.
    /// </summary>
    public byte[]? Metadata { get; set; }

    #endregion

    #region [ Methods ]

    private void ToWriteProcessorSettings(WriteProcessorSettings settings)
    {
        if (!SupportsWriting)
            return;

        ValidateExtension(IntermediateFileExtension, out string intermediateFilePendingExtension, out string intermediateFileFinalExtension);
        ValidateExtension(FinalFileExtension, out string finalFilePendingExtension, out string finalFileFinalExtension);

        List<string> finalPaths = [];

        if (FinalWritePaths.Count > 0)
            finalPaths.AddRange(FinalWritePaths);
        else
            finalPaths.Add(m_mainPath);

        settings.IsEnabled = true;

        // 0.1 seconds
        settings.PrebufferWriter.RolloverInterval = CacheFlushInterval;
        settings.PrebufferWriter.MaximumPointCount = 25000;
        settings.PrebufferWriter.RolloverPointCount = 25000;

        // 10 seconds
        settings.FirstStageWriter.MaximumAllowedMb = 100; // about 10 million points
        settings.FirstStageWriter.RolloverSizeMb = 100; // about 10 million points
        settings.FirstStageWriter.RolloverInterval = DiskFlushInterval; // 10 seconds
        settings.FirstStageWriter.EncodingMethod = ArchiveEncodingMethod;
        settings.FirstStageWriter.FinalSettings.ConfigureOnDisk([m_mainPath], SI2.Giga, ArchiveDirectoryMethod.TopDirectoryOnly, ArchiveEncodingMethod, "stage1", intermediateFilePendingExtension, intermediateFileFinalExtension, FileFlags.Stage1, FileFlags.IntermediateFile);
        settings.FirstStageWriter.FinalSettings.Metadata = Metadata;

        for (int stage = 2; stage <= StagingCount; stage++)
        {
            int remainingStages = StagingCount - stage;

            CombineFilesSettings rollover = new();

            if (remainingStages > 0)
                rollover.ArchiveSettings.ConfigureOnDisk([m_mainPath], SI2.Giga, ArchiveDirectoryMethod.TopDirectoryOnly, ArchiveEncodingMethod, "stage" + stage, intermediateFilePendingExtension, intermediateFileFinalExtension, FileFlags.GetStage(stage), FileFlags.IntermediateFile);
            else
                // Final staging file
                rollover.ArchiveSettings.ConfigureOnDisk(finalPaths, DesiredRemainingSpace, DirectoryMethod, ArchiveEncodingMethod, DatabaseName.ToNonNullNorEmptyString("stage" + stage).RemoveInvalidFileNameCharacters(), finalFilePendingExtension, finalFileFinalExtension, FileFlags.GetStage(stage));

            rollover.ArchiveSettings.Metadata = Metadata;

            rollover.LogPath = m_mainPath;
            rollover.ExecuteTimer = 1000;
            rollover.CombineOnFileCount = 60;
            rollover.CombineOnFileSize = TargetFileSize / (long)Math.Pow(30, remainingStages);
            rollover.MatchFlag = FileFlags.GetStage(stage - 1);
            settings.StagingRollovers.Add(rollover);
        }
    }

    private void ToArchiveListSettings(ArchiveListSettings listSettings)
    {
        ValidateExtension(IntermediateFileExtension, out string _, out string intermediateFileFinalExtension);
        ValidateExtension(FinalFileExtension, out string _, out string finalFileFinalExtension);

        listSettings.AddExtension(intermediateFileFinalExtension);
        listSettings.AddExtension(finalFileFinalExtension);

        if (!string.IsNullOrWhiteSpace(m_mainPath))
            listSettings.AddPath(m_mainPath);

        if (ImportAttachedPathsAtStartup)
        {
            listSettings.AddPaths(ImportPaths);
            listSettings.AddPaths(FinalWritePaths);
        }

        listSettings.LogSettings.LogPath = m_mainPath;
    }

    /// <summary>
    /// Converts the current instance of the database settings to a <see cref="ServerDatabaseSettings"/> object.
    /// </summary>
    /// <returns>A <see cref="ServerDatabaseSettings"/> object representing the database settings.</returns>
    public ServerDatabaseSettings ToServerDatabaseSettings()
    {
        ServerDatabaseSettings settings = new() { DatabaseName = DatabaseName };

        if (SupportsWriting)
            ToWriteProcessorSettings(settings.WriteProcessor);

        settings.SupportsWriting = SupportsWriting;
        ToArchiveListSettings(settings.ArchiveList);

        settings.RolloverLog.LogPath = m_mainPath;
        settings.KeyType = new TKey().GenericTypeGuid;
        settings.ValueType = new TValue().GenericTypeGuid;

        if (StreamingEncodingMethods.Count == 0)
            settings.StreamingEncodingMethods.Add(EncodingDefinition.FixedSizeCombinedEncoding);
        else
            settings.StreamingEncodingMethods.AddRange(StreamingEncodingMethods);

        return settings;
    }

    #endregion

    #region [ Static ]

    private static void ValidateExtension(string extension, out string pending, out string final)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Cannot be null or whitespace", nameof(extension));

        extension = extension.Trim();

        if (extension.Contains("."))
            extension = extension.Substring(extension.IndexOf('.') + 1);

        pending = ".~" + extension;
        final = "." + extension;
    }

    #endregion
}