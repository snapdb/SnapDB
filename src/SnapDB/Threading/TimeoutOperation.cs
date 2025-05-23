﻿//******************************************************************************************************
//  TimeoutOperation.cs - Gbtc
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
//  01/05/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//*****************************************************************************************************

namespace SnapDB.Threading;

/// <summary>
/// Represents an operation with a timeout that can execute a callback.
/// </summary>
public class TimeoutOperation
{
    #region [ Members ]

    private Action? m_callback;
    private RegisteredWaitHandle? m_registeredHandle;
    private ManualResetEvent? m_resetEvent;

    // ToDo: Figure out how to allow for a weak referenced callback.

    private readonly Lock m_syncRoot = new();

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Registers a timeout callback to be executed at specified intervals.
    /// </summary>
    /// <param name="interval">The time interval between callback executions.</param>
    /// <param name="callback">The callback action to be executed.</param>
    /// <exception cref="Exception">Thrown if a duplicate registration is attempted.</exception>
    public void RegisterTimeout(TimeSpan interval, Action callback)
    {
        lock (m_syncRoot)
        {
            if (m_callback is not null)
                throw new Exception("Duplicate calls are not permitted");

            m_callback = callback;
            m_resetEvent = new ManualResetEvent(false);
            m_registeredHandle = ThreadPool.RegisterWaitForSingleObject(m_resetEvent, BeginRun, null, interval, true);
        }
    }

    /// <summary>
    /// Cancels a previously registered timeout callback.
    /// </summary>
    public void Cancel()
    {
        lock (m_syncRoot)
        {
            if (m_registeredHandle is not null)
            {
                m_registeredHandle.Unregister(null);
                m_resetEvent?.Dispose();
                m_resetEvent = null;
                m_registeredHandle = null;
                m_callback = null;
            }
        }
    }

    private void BeginRun(object? state, bool isTimeout)
    {
        lock (m_syncRoot)
        {
            if (m_registeredHandle is null)
                return;

            m_registeredHandle.Unregister(null);
            m_resetEvent?.Dispose();
            m_callback?.Invoke();
            m_resetEvent = null;
            m_registeredHandle = null;
            m_callback = null;
        }
    }

    #endregion
}