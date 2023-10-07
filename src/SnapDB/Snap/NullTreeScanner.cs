//******************************************************************************************************
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
/// Represents a specialized implementation of SeekableTreeStream that acts as a null stream, providing no data and always returning false on reads.
/// </summary>
/// <typeparam name="TKey">The type of keys in the stream (must be a reference type).</typeparam>
/// <typeparam name="TValue">The type of values in the stream (must be a reference type).</typeparam>
public class NullTreeScanner<TKey, TValue> : SeekableTreeStream<TKey, TValue> where TKey : class, new() where TValue : class, new()
{
    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="NullTreeScanner{TKey, TValue}"/> class.
    /// </summary>
    public NullTreeScanner()
    {
        Dispose();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Seeks to the specified key (not implemented, as this is a null stream).
    /// </summary>
    /// <param name="key">The key to seek to (not used).</param>
    public override void SeekToKey(TKey key)
    {
    }

    /// <summary>
    /// Reads the next key-value pair (always returns false since this is a null stream).
    /// </summary>
    /// <param name="key">The key to read (not used).</param>
    /// <param name="value">The value to read (not used).</param>
    /// <returns>Always returns false, indicating the end of the stream.</returns>
    protected override bool ReadNext(TKey key, TValue value)
    {
        return false;
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Static constructor to initialize the static instance of the <see cref="NullTreeScanner{TKey, TValue}"/> class.
    /// </summary>
    static NullTreeScanner()
    {
        Instance = new NullTreeScanner<TKey, TValue>();
    }

    /// <summary>
    /// Gets a static instance of the <see cref="NullTreeScanner{TKey, TValue}"/> class for convenience.
    /// </summary>
    public static SeekableTreeStream<TKey, TValue> Instance { get; private set; }

    #endregion
}