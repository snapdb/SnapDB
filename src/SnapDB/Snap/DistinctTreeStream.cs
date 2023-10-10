//******************************************************************************************************
//  DistinctTreeStream'2.cs - Gbtc
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
//  09/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Represents a stream that filters out duplicate entries from a base <see cref="TreeStream{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the stream.</typeparam>
/// <typeparam name="TValue">The type of values in the stream.</typeparam>
public class DistinctTreeStream<TKey, TValue> : TreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly TreeStream<TKey, TValue> m_baseStream;
    private bool m_isLastValueValid;
    private readonly TKey m_lastKey;
    private readonly TValue m_lastValue;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="DistinctTreeStream{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="baseStream">The base stream to filter duplicates from.</param>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="baseStream"/> is not sequential.</exception>
    public DistinctTreeStream(TreeStream<TKey, TValue> baseStream)
    {
        if (!baseStream.IsAlwaysSequential)
            throw new ArgumentException("Must be sequential access", nameof(baseStream));

        m_lastKey = new TKey();
        m_lastValue = new TValue();
        m_isLastValueValid = false;
        m_baseStream = baseStream;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets a value indicating whether this stream is always sequential, which is true for this implementation.
    /// </summary>
    public override bool IsAlwaysSequential => true;

    /// <summary>
    /// Gets a value indicating that this stream never contains duplicate items, which is true for this implementation.
    /// </summary>
    public override bool NeverContainsDuplicates => true;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Disposes of managed resources used by this stream.
    /// </summary>
    /// <param name="disposing">True if disposing of managed resources, false if finalizing.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            m_baseStream.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// Callback method invoked when the end of the stream is reached.
    /// </summary>
    protected override void EndOfStreamReached()
    {
    }

    /// <summary>
    /// Reads the next key-value pair from the stream and handles duplicates.
    /// </summary>
    /// <param name="key">The key to read into.</param>
    /// <param name="value">The value to read into.</param>
    /// <returns><c>true</c> if a key-value pair was successfully read, <c>false</c> if the end of the stream is reached.</returns>
    protected override bool ReadNext(TKey key, TValue value)
    {
    TryAgain:
        if (!m_baseStream.Read(key, value))
            return false;

        if (m_isLastValueValid && key.IsEqualTo(m_lastKey))
            goto TryAgain;

        m_isLastValueValid = true;
        key.CopyTo(m_lastKey);
        value.CopyTo(m_lastValue);

        return true;
    }

    #endregion
}