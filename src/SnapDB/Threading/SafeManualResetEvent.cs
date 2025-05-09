﻿//******************************************************************************************************
//  SafeManualResetEvent.cs - Gbtc
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
//  09/13/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.Diagnostics;

namespace SnapDB.Threading;

/// <summary>
/// Provides a thread safe implementation of the <see cref="ManualResetEvent"/> class.
/// </summary>
/// <remarks>
/// While <see cref="ManualResetEvent"/> is mostly thread safe, calls to Dispose
/// can cause other waiting threads to throw <see cref="ObjectDisposedException"/>.
/// This class makes disposing of the class thread safe as well.
/// Note: Not properly disposing of this class can cause all threads waiting on this
/// class to wait indefinitely.
/// </remarks>
public sealed class SafeManualResetEvent : IDisposable
{
    #region [ Members ]

    private ManualResetEvent m_resetEvent;

    private readonly Lock m_syncRoot;

    /// <summary>
    /// The number of threads waiting.
    /// </summary>
    private int m_waitingThreadCount;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="SafeManualResetEvent"/>.
    /// </summary>
    /// <param name="signaledState"><c>true</c> to set the initial state signaled; <c>false</c> to set the initial state to nonsignaled.</param>
    public SafeManualResetEvent(bool signaledState)
    {
        m_syncRoot = new Lock();
        m_resetEvent = new ManualResetEvent(signaledState);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="SafeManualResetEvent"/> object.
    /// Also signals all waiting threads and ignores all calls to <see cref="Reset"/>.
    /// </summary>
    public void Dispose()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;
            m_disposed = true;

            // If there are threads waiting, signal them to resume
            // however, do not dispose of the wait handle, 
            // allow the last waiting thread to dispose of the reset event.
            if (m_waitingThreadCount > 0)
            {
                try
                {
                    m_resetEvent.Set();
                }
                catch (Exception ex)
                {
                    s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to Dispose() threw an exception", null, ex);
                }
            }
            else
            {
                // Since no one is waiting on the reset event, it is safe
                // to dispose of the wait handle.
                try
                {
                    m_resetEvent.Dispose();
                }
                catch (Exception ex)
                {
                    s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to Dispose() threw an exception", null, ex);
                }

                m_resetEvent = null;
            }
        }
    }

    //Note: A finalizer will not properly release waiting threads
    //      because a class will never lose reference while
    //      a thread is inside on of these methods.
    //~ManualResetEventHelper(){}

    /// <summary>
    /// Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
    /// </summary>
    public void Reset()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;

            try
            {
                m_resetEvent.Reset();
            }
            catch (Exception ex)
            {
                s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to Reset() threw an exception", null, ex);
            }
        }
    }

    /// <summary>
    /// Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
    /// </summary>
    public void Set()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;
            try
            {
                m_resetEvent.Set();
            }
            catch (Exception ex)
            {
                s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to Set() threw an exception", null, ex);
            }
        }
    }

    /// <summary>
    /// Blocks the current thread until <see cref="Set"/> or <see cref="Dispose"/> is called.
    /// </summary>
    public void WaitOne()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;

            m_waitingThreadCount++;
        }

        try
        {
            m_resetEvent.WaitOne();
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to WaitOne() threw an exception", null, ex);
        }
        finally
        {
            lock (m_syncRoot)
            {
                m_waitingThreadCount--;

                // If the class was recently disposed, 
                // the last waiting thread should dispose of the wait handle.
                if (m_disposed && m_waitingThreadCount == 0)
                {
                    try
                    {
                        m_resetEvent.Dispose();
                    }
                    catch (Exception ex)
                    {
                        s_log.Publish(MessageLevel.NA, MessageFlags.BugReport, "Possible miscoordination of dispose method", "Call to WaitOne() threw an exception", null, ex);
                    }

                    m_resetEvent = null;
                }
            }
        }
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// A place to report exception logs associated with this class.
    /// </summary>
    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(SafeManualResetEvent), MessageClass.Component);

    #endregion
}