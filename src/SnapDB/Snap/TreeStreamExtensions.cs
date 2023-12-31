﻿//*****************************************************************************************************
//  TreeStreamExtensions.cs - Gbtc
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
//  09/04/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Provides extension methods for <see cref="TreeStream{TKey, TValue}"/> instances.
/// </summary>
public static class TreeStreamExtensions
{
    #region [ Static ]

    /// <summary>
    /// Parses an entire stream to count the number of items. Notice, this will
    /// enumerate the stream, and the stream will have to be reset to be enumerated again.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="stream">The stream to enumerate.</param>
    /// <returns>The number of items in the stream.</returns>
    public static long Count<TKey, TValue>(this TreeStream<TKey, TValue> stream) where TKey : class, new() where TValue : class, new()
    {
        TKey key = new();
        TValue value = new();
        long cnt = 0;
        while (stream.Read(key, value))
            cnt++;
        return cnt;
    }

    #endregion
}