//******************************************************************************************************
//  KeyValueStreamExtension.cs - Gbtc
//
//  Copyright © 2023, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using SnapDB.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Tree.Generic;

public static class KeyValueStreamExtension
{
    #region [ Static ]

    public static TreeStreamSequential<TKey, TValue> TestSequential<TKey, TValue>(this TreeStream<TKey, TValue> stream) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return new TreeStreamSequential<TKey, TValue>(stream);
    }

    #endregion
}

/// <summary>
/// This class will throw exceptions if the bahavior of a KeyValueStream is not sequential.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class TreeStreamSequential<TKey, TValue> : TreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly TreeStream<TKey, TValue> m_baseStream;
    private readonly TKey m_baseStreamCurrentKey;
    private readonly TValue m_baseStreamCurrentValue;

    private bool m_baseStreamIsValid;
    private readonly TKey m_currentKey;
    private readonly TValue m_currentValue;
    private bool m_isEndOfStream;
    private bool m_isValid;

    #endregion

    #region [ Constructors ]

    public TreeStreamSequential(TreeStream<TKey, TValue> baseStream)
    {
        m_isEndOfStream = false;
        m_baseStream = baseStream;
        m_isValid = false;
        m_currentKey = new TKey();
        m_currentValue = new TValue();
        m_baseStreamCurrentKey = new TKey();
        m_baseStreamCurrentValue = new TValue();
        m_baseStreamIsValid = false;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Advances the stream to the next value.
    /// If before the beginning of the stream, advances to the first value
    /// </summary>
    /// <returns>True if the advance was successful. False if the end of the stream was reached.</returns>
    protected override bool ReadNext(TKey key, TValue value)
    {
        if (m_isEndOfStream)
        {
            if (m_baseStream.Read(key, value))
                throw new Exception("Data exists past the end of the stream");
            if (m_baseStream.Eos)
                throw new Exception("Should not be valid");
            return false;
        }

        m_baseStreamIsValid = m_baseStream.Read(m_baseStreamCurrentKey, m_baseStreamCurrentValue);
        if (m_baseStreamIsValid)
        {
            if (!m_baseStreamIsValid)
                throw new Exception("Should be valid");
            if (m_isValid)
                if (m_currentKey.IsGreaterThanOrEqualTo(m_baseStreamCurrentKey)) // CurrentKey.IsGreaterThanOrEqualTo(m_baseStream.CurrentKey))
                    throw new Exception("Stream is not sequential");

            m_isValid = true;
            m_baseStreamCurrentKey.CopyTo(m_currentKey); //m_baseStream.CurrentKey.CopyTo(CurrentKey);
            m_baseStreamCurrentValue.CopyTo(m_currentValue); // m_baseStream.CurrentValue.CopyTo(CurrentValue);
            return true;
        }

        m_baseStreamIsValid = m_baseStream.Read(m_baseStreamCurrentKey, m_baseStreamCurrentValue);
        if (m_baseStreamIsValid)
            throw new Exception("Data exists past the end of the stream");
        if (m_baseStreamIsValid)
            throw new Exception("Should not be valid");

        m_isEndOfStream = true;
        m_isValid = false;
        return false;
    }

    #endregion
}