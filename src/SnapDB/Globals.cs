﻿//******************************************************************************************************
//  Globals.cs - Gbtc
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
//  06/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;
using SnapDB.IO.Unmanaged;

[assembly: InternalsVisibleTo("SnapDB.UnitTests")]
[assembly: InternalsVisibleTo("GSF.SortedTreeStore.Test")]
[assembly: InternalsVisibleTo("openHistorian.Adapters")]
[assembly: InternalsVisibleTo("openHistorian.PerformanceTests")]
[assembly: InternalsVisibleTo("ArchiveIntegrityChecker")]

namespace SnapDB;

/// <summary>
/// Maintains the static global classes for the historian.
/// </summary>
public static class Globals
{
    #region [ Static ]

    /// <summary>
    /// A global Memory Pool that uses 64KB pages.
    /// </summary>
    public static MemoryPool MemoryPool;

    static Globals()
    {
        MemoryPool = new MemoryPool();
    }

    #endregion
}