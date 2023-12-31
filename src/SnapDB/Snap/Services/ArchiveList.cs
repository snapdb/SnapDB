﻿//******************************************************************************************************
//  ArchiveList.cs - Gbtc
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

using System.Text;
using Gemstone.Diagnostics;

namespace SnapDB.Snap.Services;

/// <summary>
/// Manages the complete list of archive resources and the
/// associated reading and writing that goes along with it.
/// </summary>
public abstract class ArchiveList : DisposableLoggingClassBase
{
    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="ArchiveList"/>
    /// </summary>
    protected ArchiveList() : base(MessageClass.Framework)
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Attaches the supplied paths or files.
    /// </summary>
    /// <param name="paths">The collection of path strings representing file names or directories to enumerate.</param>
    /// <returns>Void.</returns>
    public abstract void AttachFileOrPath(IEnumerable<string> paths);

    /// <summary>
    /// Loads the specified files into the archive list.
    /// </summary>
    /// <param name="archiveFiles">The files to be archived.</param>
    public abstract void LoadFiles(IEnumerable<string> archiveFiles);

    /// <summary>
    /// Appends the status of the files in the ArchiveList to the provided <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="status">Target status output <see cref="StringBuilder"/>.</param>
    /// <param name="maxFileListing">Maximum file listing.</param>
    public abstract void GetFullStatus(StringBuilder status, int maxFileListing = -1);

    /// <summary>
    /// Gets a complete list of all archive files.
    /// </summary>
    /// <returns>All of the attached archive files.</returns>
    public abstract List<ArchiveDetails> GetAllAttachedFiles();

    /// <summary>
    /// Returns an <see cref="IDisposable"/> class that can be used to edit the contents of this list.
    /// WARNING: Make changes quickly and dispose the returned class.  All calls to this class are blocked while
    /// editing this class.
    /// </summary>
    /// <returns>An instance of ArchiveListEditor representing the acquired edit lock.</returns>
    public ArchiveListEditor AcquireEditLock()
    {
        return InternalAcquireEditLock();
    }

    /// <summary>
    /// Necessary to provide shadow method of <see cref="AcquireEditLock"/>.
    /// </summary>
    /// <returns>An instance of ArchiveListEditor representing the acquired edit lock.</returns>
    protected abstract ArchiveListEditor InternalAcquireEditLock();

    #endregion

    ///// <summary>
    ///// Creates an object that can be used to get updated snapshots from this <see cref="ArchiveList{TKey,TValue}"/>.
    ///// Client must call <see cref="IDisposable.Dispose"/> method when finished with these resources as they will not 
    ///// automatically be reclaimed by the garbage collector. Class will not be initiallized until calling <see cref="ArchiveListSnapshot{TKey,TValue}.UpdateSnapshot"/>.
    ///// </summary>
    //public abstract ArchiveListSnapshot<TKey, TValue> CreateNewClientResources();

    ///// <summary>
    ///// Determines if the provided file is currently in use
    ///// by any resource. 
    ///// </summary>
    ///// <param name="sortedTree"> file to search for.</param>
    //public abstract bool IsFileBeingUsed(SortedTreeTable<TKey, TValue> sortedTree);
}