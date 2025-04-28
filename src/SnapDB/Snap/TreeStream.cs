//******************************************************************************************************
//  TreeStream'2.cs - Gbtc
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
//  09/15/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// Represents a stream of KeyValues.
/// </summary>
/// <typeparam name="TKey">The key associated with the point.</typeparam>
/// <typeparam name="TValue">The value associated with the point.</typeparam>
public abstract class TreeStream<TKey, TValue> : IDisposable where TKey : class, new() where TValue : class, new()
{
    #region [ Members ]

    private bool m_disposed;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Boolean indicating that the end of the stream has been read or class has been disposed.
    /// </summary>
    public bool Eos { get; private set; }

    /// <summary>
    /// Gets if the stream is always in sequential order. Do not return true unless it is guaranteed that
    /// the data read from this stream is sequential.
    /// </summary>
    public virtual bool IsAlwaysSequential => false;

    /// <summary>
    /// Gets if the stream will never return duplicate keys. Do not return true unless it is guaranteed that
    /// the data read from this stream will never contain duplicates.
    /// </summary>
    public virtual bool NeverContainsDuplicates => false;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!m_disposed)
            try
            {
                Dispose(true);
            }
            finally
            {
                Eos = true;
                m_disposed = true;
            }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="TreeStream{TKey,TValue}"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        Eos = true;
    }

    /// <summary>
    /// Advances the stream to the next value.
    /// If before the beginning of the stream, advances to the first value
    /// </summary>
    /// <param name="key">The key to be advanced to.</param>
    /// <param name="value">The value associated with the key to be advanced to.</param>
    /// <returns><c>true</c> if the advance was successful; otherwise, <c>false</c> if the end of the stream was reached.</returns>
    public bool Read(TKey key, TValue value)
    {
        if (Eos || !ReadNext(key, value))
        {
            EndOfStreamReached();
            return false;
        }

        return true;
    }

#if !SQLCLR
    /// <summary>
    /// Advances the stream to the next value. 
    /// If before the beginning of the stream, advances to the first value
    /// </summary>
    /// <returns>True if the advance was successful. False if the end of the stream was reached.</returns>
    public async ValueTask<bool> ReadAsync(TKey key, TValue value)
    {
        if (Eos || !await new ValueTask<bool>(ReadNext(key, value)))
        {
            EndOfStreamReached();
            return false;
        }

        return true;
    }
#endif

    /// <summary>
    /// Occurs when the end of the stream has been reached. The default behavior is to call Dispose.
    /// </summary>
    protected virtual void EndOfStreamReached()
    {
        Dispose();
    }

    /// <summary>
    /// Sets the end-of-stream (EOS) flag, indicating whether the stream has reached its end.
    /// </summary>
    /// <param name="value">The value to set for the EOS flag.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if <paramref name="value"/> is <c>false</c> and this class
    /// has already been disposed.
    /// </exception>
    protected void SetEos(bool value)
    {
        ObjectDisposedException.ThrowIf(m_disposed && !value, this);
        Eos = value;
    }

    /// <summary>
    /// Advances the stream to the next value.
    /// If before the beginning of the stream, advances to the first value
    /// </summary>
    /// <param name="key">The key to be advanced to.</param>
    /// <param name="value">The value associated with the key to be advanced to.</param>
    /// <returns>
    /// <c>true</c> if the advance was successful; <c>false</c> if the end of the stream was reached.
    /// </returns>
    protected abstract bool ReadNext(TKey key, TValue value);

    #endregion
}