//******************************************************************************************************
//  WriteProcessorSettings.cs - Gbtc
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
//  10/03/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using Gemstone.IO.StreamExtensions;
using SnapDB.Immutables;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// The settings for the write processor.
/// </summary>
public class WriteProcessorSettings : SettingsBase<WriteProcessorSettings>
{
    #region [ Members ]

    private bool m_isEnabled;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// The default write processor settings.
    /// </summary>
    public WriteProcessorSettings()
    {
        m_isEnabled = false;
        PrebufferWriter = new PrebufferWriterSettings();
        FirstStageWriter = new FirstStageWriterSettings();
        StagingRollovers = new ImmutableList<CombineFilesSettings>(x =>
        {
            if (x is null)
                throw new ArgumentNullException("value", "cannot be null");

            return x;
        });
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The settings for the prebuffer.
    /// </summary>
    public PrebufferWriterSettings PrebufferWriter { get; }

    /// <summary>
    /// The settings for the first stage writer.
    /// </summary>
    public FirstStageWriterSettings FirstStageWriter { get; }

    /// <summary>
    /// Contains all of the staging rollovers.
    /// </summary>
    public ImmutableList<CombineFilesSettings> StagingRollovers { get; }

    /// <summary>
    /// Gets or sets if writing will be enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => m_isEnabled;
        set
        {
            TestForEditable();
            m_isEnabled = false;
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Saves the configuration of the file combine settings to the specified stream.
    /// </summary>
    /// <param name="stream">The stream where the configuration data will be saved.</param>
    public override void Save(Stream stream)
    {
        stream.Write((byte)1);
        stream.Write(m_isEnabled);
        PrebufferWriter.Save(stream);
        FirstStageWriter.Save(stream);
        stream.Write(StagingRollovers.Count);

        foreach (CombineFilesSettings stage in StagingRollovers)
            stage.Save(stream);
    }

    /// <summary>
    /// Loads the configuration of the file combine settings from the specified stream.
    /// </summary>
    /// <param name="stream">The stream from which the configuration data will be loaded.</param>
    public override void Load(Stream stream)
    {
        TestForEditable();
        byte version = stream.ReadNextByte();

        switch (version)
        {
            case 1:
                m_isEnabled = stream.ReadBoolean();
                PrebufferWriter.Load(stream);
                FirstStageWriter.Load(stream);
                int cnt = stream.ReadInt32();
                StagingRollovers.Clear();

                while (cnt > 0)
                {
                    cnt--;
                    CombineFilesSettings cfs = new();
                    cfs.Load(stream);
                    StagingRollovers.Add(cfs);
                }

                break;

            default:
                throw new VersionNotFoundException("Unknown Version Code: " + version);
        }
    }

    /// <summary>
    /// Validates the configuration of the file combine settings.
    /// </summary>
    public override void Validate()
    {
        if (IsEnabled)
        {
            PrebufferWriter.Validate();
            FirstStageWriter.Validate();

            foreach (CombineFilesSettings stage in StagingRollovers)
                stage.Validate();
        }
    }

    #endregion
}