//******************************************************************************************************
//  EventTimer.cs - Gbtc
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
//  10/22/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//*****************************************************************************************************

using Gemstone;
using Gemstone.Diagnostics;
using Gemstone.Threading;

namespace SnapDB.Threading;

/// <summary>
/// A timer event that occurs on a specific interval at a specific offset.
/// This class is thread safe.
/// </summary>
public class EventTimer : DisposableLoggingClassBase
{
    #region [ Members ]

    private readonly TimeSpan m_dayOffset;
    private bool m_isRunning;

    private readonly LogStackMessages m_message;
    private readonly TimeSpan m_period;
    private bool m_stopping;
    private readonly object m_syncRoot;

    private ScheduledTask m_timer;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the EventTimer class with the specified period and day offset.
    /// </summary>
    /// <param name="period">The time interval between timer ticks.</param>
    /// <param name="dayOffset">The time offset added to each tick.</param>
    private EventTimer(TimeSpan period, TimeSpan dayOffset) : base(MessageClass.Component)
    {
        m_stopping = false;
        m_syncRoot = new object();
        m_period = period;
        m_dayOffset = dayOffset;

        m_message = LogStackMessages.Empty.Union("Event Timer Details", $"EventTimer: {m_period} in {m_dayOffset}");
        Log.InitialStackMessages.Union("Event Timer Details", $"EventTimer: {m_period} in {m_dayOffset}");
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets or sets a value indicating whether the timer is enabled.
    /// </summary>
    /// <remarks>
    /// When setting to <c>true</c>, it starts the timer; when setting to <c>false</c>, it stops the timer.
    /// </remarks>
    public bool Enabled
    {
        get => m_isRunning;
        set
        {
            if (value)
                Start();

            else
                Stop();
        }
    }

    /// <summary>
    /// Gets the time remaining until the next execution of the timer.
    /// </summary>
    /// <remarks>
    /// The property calculates the time remaining based on the timer's period and day offset.
    /// </remarks>
    public TimeSpan TimeUntilNextExecution
    {
        get
        {
            long current = DateTime.UtcNow.Ticks;
            long subtractOffset = current - m_dayOffset.Ticks;
            long remainderTicks = m_period.Ticks - subtractOffset % m_period.Ticks;
            int delay = (int)(remainderTicks / TimeSpan.TicksPerMillisecond) + 1;
            if (delay < 10)
                delay += (int)m_period.TotalMilliseconds;
            return new TimeSpan(delay * TimeSpan.TicksPerMillisecond);
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Occurs when the timer elapses.
    /// Event occurs on the ThreadPool
    /// </summary>
    public event Action Elapsed;

    /// <summary>
    /// Starts the event timer.
    /// </summary>
    /// <remarks>
    /// The method starts the event timer by creating and initializing a ScheduledTask object.
    /// </remarks>
    public void Start()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;

            if (m_isRunning)
                return;

            m_isRunning = true;
            m_timer = new ScheduledTask();
            m_timer.Running += m_timer_Running;
            RestartTimer();
        }

        Log.Publish(MessageLevel.Info, "EventTimer Started");
    }

    /// <summary>
    /// Stops the event timer.
    /// </summary>
    /// <remarks>
    /// The method stops the event timer by disposing of the underlying ScheduledTask object and
    /// resetting the timer's state.
    /// </remarks>
    public void Stop()
    {
        lock (m_syncRoot)
        {
            if (!m_isRunning)
                return;

            if (m_stopping)
                return;
            m_stopping = true;
        }

        m_timer.Dispose();
        lock (m_syncRoot)
        {
            m_timer = null;
            m_stopping = false;
            m_isRunning = false;
        }

        Log.Publish(MessageLevel.Info, "EventTimer Started");
    }

    /// <summary>
    /// Releases the resources used by the event timer and stops it.
    /// </summary>
    /// <param name="disposing">
    /// A Boolean value that determines whether the method was called from the
    /// <see cref="Dispose"/> method rather than from the finalizer.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            m_disposed = true;
            Stop();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// This timer will reliably fire the directory polling every interval.
    /// </summary>
    private void m_timer_Running(object sender, EventArgs<ScheduledTaskRunningReason> e)
    {
        //This cannot be combined with m_directoryPolling because 
        //Scheduled task does not support managing multiple conflicting timers.
        if (e.Argument == ScheduledTaskRunningReason.Disposing)
            return;

        if (m_stopping)
            return;

        if (Elapsed is not null)
            try
            {
                using (Logger.AppendStackMessages(m_message))
                {
                    Elapsed();
                }
            }
            catch (Exception ex)
            {
                Log.Publish(MessageLevel.Error, "Event Timer Exception on raising event.", null, null, ex);
            }

        if (m_stopping)
            return;

        RestartTimer();
    }

    private void RestartTimer()
    {
        long current = DateTime.UtcNow.Ticks;
        long subtractOffset = current - m_dayOffset.Ticks;
        long remainderTicks = m_period.Ticks - subtractOffset % m_period.Ticks;
        int delay = (int)(remainderTicks / TimeSpan.TicksPerMillisecond) + 1;

        if (delay < 10)
            delay += (int)m_period.TotalMilliseconds;

        m_timer.Start(delay);
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Creates a new instance of the <see cref="EventTimer"/> class with the specified period and day offset.
    /// </summary>
    /// <param name="period">The time interval between timer events.</param>
    /// <param name="dayOffset">The initial offset from the start of the day (optional).</param>
    /// <returns>A new <see cref="EventTimer"/> instance.</returns>
    public static EventTimer Create(TimeSpan period, TimeSpan dayOffset = default)
    {
        return new EventTimer(period, dayOffset);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="EventTimer"/> class with the specified period and day offset in seconds.
    /// </summary>
    /// <param name="periodInSecond">The time interval between timer events in seconds.</param>
    /// <param name="dayOffsetInSecond">The initial offset from the start of the day in seconds (optional).</param>
    /// <returns>A new <see cref="EventTimer"/> instance.</returns>
    public static EventTimer CreateSeconds(double periodInSecond, double dayOffsetInSecond = 0)
    {
        return new EventTimer(new TimeSpan((long)(periodInSecond * TimeSpan.TicksPerSecond)), new TimeSpan((long)(dayOffsetInSecond * TimeSpan.TicksPerSecond)));
    }

    /// <summary>
    /// Creates a new instance of the <see cref="EventTimer"/> class with the specified period and day offset in minutes.
    /// </summary>
    /// <param name="periodInMinutes">The time interval between timer events in minutes.</param>
    /// <param name="dayOffsetInMinutes">The initial offset from the start of the day in minutes (optional).</param>
    /// <returns>A new <see cref="EventTimer"/> instance.</returns>
    public static EventTimer CreateMinutes(double periodInMinutes, double dayOffsetInMinutes = 0)
    {
        return new EventTimer(new TimeSpan((long)(periodInMinutes * TimeSpan.TicksPerMinute)), new TimeSpan((long)(dayOffsetInMinutes * TimeSpan.TicksPerMinute)));
    }

    #endregion
}