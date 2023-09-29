﻿//******************************************************************************************************
//  NullTreeScanner.cs - Gbtc
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
//  12/01/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Represents an empty tree scanner. 
/// </summary>
/// <remarks>
/// This can be useful to return instead of null at times. Seeks will not throw exceptions and 
/// scans will yield no results.
/// To use this class. Call the static property <see cref="Instance"/>.
/// </remarks>
public class NullTreeScanner<TKey, TValue>
    : SeekableTreeStream<TKey, TValue>
    where TKey : class, new()
    where TValue : class, new()
{
    /// <summary>
    /// Returns a static instance of this class
    /// </summary>
    public static SeekableTreeStream<TKey, TValue> Instance
    {
        get;
        private set;
    }

    static NullTreeScanner()
    {
        Instance = new NullTreeScanner<TKey, TValue>();
    }

    public NullTreeScanner()
    {
        Dispose();
    }

    protected override bool ReadNext(TKey key, TValue value)
    {
        return false;
    }

    public override void SeekToKey(TKey key)
    {
    }


}