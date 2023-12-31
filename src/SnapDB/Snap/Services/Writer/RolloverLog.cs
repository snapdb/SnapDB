﻿//******************************************************************************************************
//  RolloverLog.cs - Gbtc
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

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// The log file that describes the rollover process so if the service crashes during the rollover process,
/// it can properly be recovered from.
/// </summary>
public class RolloverLog
{
    #region [ Members ]

    private readonly RolloverLogSettings m_settings;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="RolloverLog"/>
    /// </summary>
    /// <param name="settings">the settings</param>
    /// <param name="list">the list</param>
    public RolloverLog(RolloverLogSettings settings, ArchiveList list)
    {
        m_settings = settings.CloneReadonly();
        m_settings.Validate();

        if (settings.IsFileBacked)
            foreach (string logFile in Directory.GetFiles(settings.LogPath, settings.SearchPattern))
            {
                RolloverLogFile log = new(logFile);
                if (log.IsValid)
                    log.Recover(list);

                else
                    log.Delete();
            }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a new <see cref="RolloverLogFile"/> for specified source files and a destination file.
    /// </summary>
    /// <param name="sourceFiles">The list of source file identifiers to include in the rollover log.</param>
    /// <param name="destinationFile">The identifier of the destination file for the rollover operation.</param>
    /// <returns>A new instance of <see cref="RolloverLogFile"/> representing the rollover log.</returns>
    /// <remarks>
    /// The <see cref="Create"/> method generates a new unique file name using the archive's settings, and then creates a
    /// <see cref="RolloverLogFile"/> instance to represent the rollover log for the specified source files and destination file.
    /// </remarks>
    public RolloverLogFile Create(List<Guid> sourceFiles, Guid destinationFile)
    {
        string fileName = m_settings.GenerateNewFileName();
        return new RolloverLogFile(fileName, sourceFiles, destinationFile);
    }

    #endregion
}