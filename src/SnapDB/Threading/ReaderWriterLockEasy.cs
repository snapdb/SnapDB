//******************************************************************************************************
//  ReaderWriterLockEasy.cs - Gbtc
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
//  10/09/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Threading;

/// <summary>
/// A read lock object.
/// </summary>
public struct DisposableReadLock
    : IDisposable
{
    private ReaderWriterLock m_l;
    
    /// <summary>
    /// Initializes a new instance of the DisposableReadLock class and acquires a reader lock.
    /// </summary>
    /// <param name="l">The ReaderWriterLock to acquire the lock from.</param>
    public DisposableReadLock(ReaderWriterLock l)
    {
        m_l = l;
        l.AcquireReaderLock(Timeout.Infinite);
    }

    /// <summary>
    /// Releases the acquired reader lock, if it was acquired.
    /// </summary>
    public void Dispose()
    {
        if (m_l is not null)
        {
            m_l.ReleaseReaderLock();
            m_l = null;
        }
    }
}

/// <summary>
/// A read lock object.
/// </summary>
public struct DisposableWriteLock
    : IDisposable
{
    private ReaderWriterLock m_l;
    
    /// <summary>
    /// Initializes a new instance of the DisposableWriteLock class and acquires a writer lock on the specified ReaderWriterLock.
    /// </summary>
    /// <param name="l">The ReaderWriterLock to acquire the writer lock on.</param>
    public DisposableWriteLock(ReaderWriterLock l)
    {
        m_l = l;
        l.AcquireWriterLock(Timeout.Infinite);
    }

    /// <summary>
    /// Releases the acquired writer lock on the associated ReaderWriterLock, allowing other threads to acquire locks.
    /// </summary>
    public void Dispose()
    {
        if (m_l is not null)
        {
            m_l.ReleaseWriterLock();
            m_l = null;
        }
    }
}

/// <summary>
/// A simplified implementation of a <see cref="ReaderWriterLockSlim"/>. This allows for more 
/// user friendly code to be written.
/// </summary>
public class ReaderWriterLockEasy
{
    private readonly ReaderWriterLock m_lock = new();

    /// <summary>
    /// Enters a read lock. Be sure to call within a using block.
    /// </summary>
    public DisposableReadLock EnterReadLock()
    {
        return new DisposableReadLock(m_lock);
    }

    /// <summary>
    /// Acquires a writer lock, preventing other threads from acquiring writer or reader locks until the writer lock is released.
    /// </summary>
    /// <returns>A DisposableWriteLock object that should be disposed to release the acquired writer lock.</returns>
    public DisposableWriteLock EnterWriteLock()
    {
        return new DisposableWriteLock(m_lock);
    }
}
