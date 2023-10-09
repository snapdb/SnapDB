//******************************************************************************************************
//  Stats.cs - Gbtc
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
//  12/19/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/21/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Provides statistical information related to various operations.
/// </summary>
public static class Stats
{
    #region [ Static ]

    /// <summary>
    /// Checks how many times the checksum was computed. This is used to see IO amplification.
    /// It is currently a debug term that will soon disappear.
    /// </summary>
    public static long ChecksumCount;

    /// <summary>
    /// Gets or sets the count of lookup keys used.
    /// </summary>
    public static long LookupKeys;

    /// <summary>
    /// Gets or sets the count of points returned.
    /// </summary>
    public static long PointsReturned;

    /// <summary>
    /// Gets or sets the count of points scanned.
    /// </summary>
    public static long PointsScanned;

    /// <summary>
    /// Gets or sets the count of queries executed.
    /// </summary>
    public static long QueriesExecuted;

    /// <summary>
    /// Gets or sets the count of seeks requested.
    /// </summary>
    public static long SeeksRequested;

    /// <summary>
    /// Clears all statistical counters.
    /// </summary>
    public static void Clear()
    {
        LookupKeys = 0;
        PointsReturned = 0;
        PointsScanned = 0;
        QueriesExecuted = 0;
        SeeksRequested = 0;
    }

    #endregion
}