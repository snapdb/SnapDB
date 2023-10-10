//******************************************************************************************************
//  MemoryPool.cs - Gbtc
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
//  03/16/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Diagnostics;
using System.Text;
using Gemstone;
using Gemstone.Diagnostics;
using SnapDB.Threading;

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Determines the desired buffer pool utilization level.
/// Setting to Low will cause collection cycles to occur more often to keep the
/// utilization level to low.
/// </summary>
public enum TargetUtilizationLevels
{
    /// <summary>
    /// Collections won't occur until over 25% of the memory is consumed.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Collections won't occur until over 50% of the memory is consumed.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Collections won't occur until over 75% of the memory is consumed.
    /// </summary>
    High = 2
}

/// <summary>
/// This class allocates and pools unmanaged memory.
/// Designed to be internally thread safe.
/// </summary>
/// <remarks>
/// Be careful how this class is referenced. Deadlocks can occur
/// when registering to event <see cref="RequestCollection"/> and
/// when calling <see cref="AllocatePage"/>.
/// </remarks>
public class MemoryPool : IDisposable
{
    #region [ Members ]

    /// <summary>
    /// Requests that classes using this <see cref="MemoryPool"/> release any unused buffers.
    /// Failing to do so may result in an <see cref="OutOfMemoryException"/> to occur.
    /// </summary>
    /// <remarks>
    /// IMPORTANT NOTICE: Do not call <see cref="AllocatePage"/> via the thread
    /// that raises this event. Also, be careful about entering a lock via this thread
    /// because a potential deadlock might occur.
    /// Also, Do not remove a handler from within a lock context as the remove
    /// blocks until all events have been called. A potential for another deadlock.
    /// </remarks>
    public event EventHandler<CollectionEventArgs> RequestCollection
    {
        add
        {
            m_requestCollectionEvent.Add(new WeakEventHandler<CollectionEventArgs>(value));
            RemoveDeadEvents();
        }
        remove
        {
            m_requestCollectionEvent.RemoveAndWait(new WeakEventHandler<CollectionEventArgs>(value));
            RemoveDeadEvents();
        }
    }

    /// <summary>
    /// Represents the ceiling for the amount of memory the buffer pool can use (124GB).
    /// </summary>
    public const long MaximumTestedSupportedMemoryCeiling = 124 * 1024 * 1024 * 1024L;

    /// <summary>
    /// Represents the minimum amount of memory that the buffer pool needs to work properly (10MB).
    /// </summary>
    public const long MinimumTestedSupportedMemoryFloor = 10 * 1024 * 1024;

    /// <summary>
    /// Provides a mask that the user can apply that can be used to get the offset position of a page.
    /// </summary>
    public readonly int PageMask;

    /// <summary>
    /// Gets the number of bits that must be shifted to calculate an index of a position.
    /// This is not the same as a page index that is returned by the allocate functions.
    /// </summary>
    public readonly int PageShiftBits;

    /// <summary>
    /// Each page will be exactly this size (based on RAM).
    /// </summary>
    public readonly int PageSize;

    private long m_levelHigh;
    private long m_levelLow;

    private long m_levelNone;
    private long m_levelNormal;
    private long m_levelVeryHigh;

    private readonly MemoryPoolPageList m_pageList;

    private volatile int m_releasePageVersion;

    /// <summary>
    /// Delegates are placed in a List because
    /// in a later version, some sort of concurrent garbage collection may be implemented
    /// which means more control will need to be with the Event.
    /// </summary>
    private readonly ThreadSafeList<WeakEventHandler<CollectionEventArgs>> m_requestCollectionEvent;

    // All allocates are synchronized separately since an allocation can request a collection. 
    // This will create a queuing nature of the allocations.
    private readonly object m_syncAllocate;

    // Used for synchronizing modifications to this class.
    private readonly object m_syncRoot;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="MemoryPool"/>.
    /// </summary>
    /// <param name="pageSize">The desired page size. Must be between 4KB and 256KB.</param>
    /// <param name="maximumBufferSize">The desired maximum size of the allocation. Note: could be less if there is not enough system memory.</param>
    /// <param name="utilizationLevel">Specifies the desired utilization level of the allocated space.</param>
    public MemoryPool(int pageSize = 64 * 1024, long maximumBufferSize = -1, TargetUtilizationLevels utilizationLevel = TargetUtilizationLevels.Low)
    {
        if (pageSize is < 4096 or > 256 * 1024)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 4KB and 256KB and a power of 2");

        if (!BitMath.IsPowerOfTwo((uint)pageSize))
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 4KB and 256KB and a power of 2");

        m_syncRoot = new object();
        m_syncAllocate = new object();
        PageSize = pageSize;
        PageMask = PageSize - 1;
        PageShiftBits = BitMath.CountBitsSet((uint)PageMask);

        m_pageList = new MemoryPoolPageList(PageSize, maximumBufferSize);
        m_requestCollectionEvent = new ThreadSafeList<WeakEventHandler<CollectionEventArgs>>();

        SetTargetUtilizationLevel(utilizationLevel);
    }

#if DEBUG
    /// <summary>
    /// Finalizes an instance of the <see cref="MemoryPool"/> class.
    /// This finalizer logs an informational message when called.
    /// </summary>
    ~MemoryPool()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Returns the number of bytes currently allocated by the buffer pool to other objects.
    /// </summary>
    public long AllocatedBytes => CurrentAllocatedSize;

    /// <summary>
    /// Returns the number of bytes allocated by all buffer pools.
    /// This does not include any pages that have been allocated but are not in use.
    /// </summary>
    public long CurrentAllocatedSize => m_pageList.CurrentAllocatedSize;

    /// <summary>
    /// Gets the number of bytes that have been allocated to this buffer pool
    /// by the OS.
    /// </summary>
    public long CurrentCapacity => m_pageList.CurrentCapacity;

    /// <summary>
    /// Gets if this pool has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The maximum amount of RAM that this memory pool is configured to support.
    /// Attempting to allocate more than this will cause an out of memory exception.
    /// </summary>
    public long MaximumPoolSize => m_pageList.MaximumPoolSize;

    /// <summary>
    /// Gets the current <see cref="TargetUtilizationLevels"/>.
    /// </summary>
    public TargetUtilizationLevels TargetUtilizationLevel { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="MemoryPool"/> object.
    /// </summary>
    public void Dispose()
    {
        lock (m_syncRoot)
        {
            if (IsDisposed)
                return;

            try
            {
                m_pageList.Dispose();
            }
            finally
            {
                IsDisposed = true; // Prevent duplicate dispose.
            }
        }
    }

    /// <summary>
    /// Requests a page from the buffered pool.
    /// If there is not a free one available, method will block
    /// and request a collection of unused pages by raising
    /// <see cref="RequestCollection"/> event.
    /// </summary>
    /// <param name="index">the index id of the page that was allocated</param>
    /// <param name="addressPointer">
    /// outputs a address that can be used
    /// to access this memory address.  You cannot call release with this parameter.
    /// Use the returned index to release pages.
    /// </param>
    /// <remarks>
    /// IMPORTANT NOTICE: Be careful when calling this method as the calling thread
    /// will block if no memory is available to have a background collection to occur.
    /// There is a possibility for a deadlock if calling this method from within a lock.
    /// The page allocated will not be initialized,
    /// so assume that the data is garbage.
    /// </remarks>
    public void AllocatePage(out int index, out nint addressPointer)
    {
        if (m_pageList.TryGetNextPage(out index, out addressPointer))
            return;

        lock (m_syncAllocate)
        {
            // m_releasePageVersion is approximately the number of times that a release page function has been called.
            // due to race conditions, the number may not be exact, but it will have at least changed.

            while (true)
            {
                int releasePageVersion = m_releasePageVersion;

                if (m_pageList.TryGetNextPage(out index, out addressPointer))
                    return;

                RequestMoreFreeBlocks();

                if (releasePageVersion == m_releasePageVersion)
                {
                    s_log.Publish(MessageLevel.Critical, MessageFlags.PerformanceIssue, "Out Of Memory", $"Memory pool has run out of memory: Current Usage: {CurrentCapacity / 1024 / 1024}MB");
                    throw new OutOfMemoryException("Memory pool is full");
                }

                // Due to a race condition, it is possible that someone else get the freed block
                // and we must request freeing again.
            }
        }
    }

    /// <summary>
    /// Releases the page back to the buffer pool for reallocation.
    /// </summary>
    /// <param name="pageIndex">A value of zero or less will return silently.</param>
    /// <remarks>
    /// The page released will not be initialized.
    /// Releasing a page is on the honor system.
    /// Re-referencing a released page will most certainly cause
    /// unexpected crashing or data corruption or any other unexplained behavior.
    /// </remarks>
    public void ReleasePage(int pageIndex)
    {
        m_pageList.ReleasePage(pageIndex);
        Interlocked.Increment(ref m_releasePageVersion);
    }

    /// <summary>
    /// Releases all of the supplied pages.
    /// </summary>
    /// <param name="pageIndexes">A collection of pages.</param>
    public void ReleasePages(IEnumerable<int> pageIndexes)
    {
        foreach (int x in pageIndexes)
            m_pageList.ReleasePage(x);

        Interlocked.Increment(ref m_releasePageVersion);
    }

    /// <summary>
    /// Changes the allowable maximum buffer size.
    /// </summary>
    /// <param name="value">The new maximum buffer size in bytes.</param>
    /// <returns>The previous maximum buffer size before the change.</returns>
    public long SetMaximumBufferSize(long value)
    {
        lock (m_syncRoot)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            long rv = m_pageList.SetMaximumPoolSize(value);
            CalculateThresholds(rv, TargetUtilizationLevel);

            s_log.Publish(MessageLevel.Info, MessageFlags.PerformanceIssue, "Pool Size Changed", $"Memory pool maximum set to: {rv >> 20}MB");

            return rv;
        }
    }

    /// <summary>
    /// Changes the utilization level.
    /// </summary>
    /// <param name="utilizationLevel">The new target utilization level to set.</param>
    public void SetTargetUtilizationLevel(TargetUtilizationLevels utilizationLevel)
    {
        lock (m_syncRoot)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            TargetUtilizationLevel = utilizationLevel;
            CalculateThresholds(MaximumPoolSize, utilizationLevel);
        }
    }

    /// <summary>
    /// Determines whether to allocate more memory or to do a collection cycle on the existing pool.
    /// </summary>
    private void RequestMoreFreeBlocks()
    {
        Stopwatch sw = new();
        sw.Start();

        StringBuilder sb = new();
        sb.AppendLine("Collection Cycle Started");

        Monitor.Enter(m_syncRoot);
        bool lockTaken = true;

        try
        {
            long size = CurrentCapacity;
            int collectionLevel = GetCollectionLevelBasedOnSize(size);
            long stopShrinkingLimit = CalculateStopShrinkingLimit(size);

            RemoveDeadEvents();

            sb.Append("Level: " + GetCollectionLevelString(collectionLevel));
            sb.AppendFormat(" Desired Size: {0}/{1}MB", stopShrinkingLimit >> 20, CurrentCapacity >> 20);
            sb.AppendLine();

            for (int x = 0; x < collectionLevel; x++)
            {
                if (CurrentAllocatedSize < stopShrinkingLimit)
                    break;

                CollectionEventArgs eventArgs = new(ReleasePage, MemoryPoolCollectionMode.Normal, 0);
                Monitor.Exit(m_syncRoot);
                lockTaken = false;

                foreach (WeakEventHandler<CollectionEventArgs> c in m_requestCollectionEvent)
                    c.TryInvoke(this, eventArgs);

                Monitor.Enter(m_syncRoot);
                lockTaken = true;

                sb.AppendFormat("Pass {0} Usage: {1}/{2}MB", x + 1, CurrentAllocatedSize >> 20, CurrentCapacity >> 20);
                sb.AppendLine();
            }

            long currentSize = CurrentAllocatedSize;
            long sizeBefore = CurrentCapacity;

            if (m_pageList.GrowMemoryPool(currentSize + (long)(0.1 * MaximumPoolSize)))
            {
                long sizeAfter = CurrentCapacity;
                Interlocked.Increment(ref m_releasePageVersion);

                sb.AppendFormat("Grew buffer pool {0}MB -> {1}MB", sizeBefore >> 20, sizeAfter >> 20);
                sb.AppendLine();
            }

            if (m_pageList.FreeSpaceBytes < 0.05 * MaximumPoolSize)
            {
                int pagesToBeReleased = (int)((0.05 * MaximumPoolSize - m_pageList.FreeSpaceBytes) / PageSize);

                sb.Append($"* Emergency Collection Occurring. Attempting to release {pagesToBeReleased} pages.");
                sb.AppendLine();

                s_log.Publish(MessageLevel.Warning, MessageFlags.PerformanceIssue, "Pool Emergency", $"Memory pool is reaching an Emergency level. Desiring Pages To Release: {pagesToBeReleased}");

                CollectionEventArgs eventArgs = new(ReleasePage, MemoryPoolCollectionMode.Emergency, pagesToBeReleased);

                Monitor.Exit(m_syncRoot);
                lockTaken = false;

                foreach (WeakEventHandler<CollectionEventArgs> c in m_requestCollectionEvent)
                {
                    if (eventArgs.DesiredPageReleaseCount == 0)
                        break;

                    c.TryInvoke(this, eventArgs);
                }

                Monitor.Enter(m_syncRoot);
                lockTaken = true;

                if (eventArgs.DesiredPageReleaseCount > 0)
                {
                    sb.Append($"** Critical Collection Occurring. Attempting to release {pagesToBeReleased} pages.");
                    sb.AppendLine();

                    s_log.Publish(MessageLevel.Warning, MessageFlags.PerformanceIssue, "Pool Critical", $"Memory pool is reaching an Critical level. Desiring Pages To Release: {eventArgs.DesiredPageReleaseCount}");

                    eventArgs = new CollectionEventArgs(ReleasePage, MemoryPoolCollectionMode.Critical, eventArgs.DesiredPageReleaseCount);

                    Monitor.Exit(m_syncRoot);
                    lockTaken = false;

                    foreach (WeakEventHandler<CollectionEventArgs> c in m_requestCollectionEvent)
                    {
                        if (eventArgs.DesiredPageReleaseCount == 0)
                            break;
                        c.TryInvoke(this, eventArgs);
                    }

                    Monitor.Enter(m_syncRoot);
                    lockTaken = true;
                }
            }

            sw.Stop();
            sb.AppendFormat("Elapsed Time: {0:0.0}ms", sw.Elapsed.TotalMilliseconds);
            s_log.Publish(MessageLevel.Info, "Memory Pool Collection Occurred", sb.ToString());

            RemoveDeadEvents();
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(m_syncRoot);
        }
    }

    /// <summary>
    /// Searches the collection events and removes any events that have been collected by
    /// the garbage collector.
    /// </summary>
    private void RemoveDeadEvents()
    {
        m_requestCollectionEvent.RemoveIf(obj => !obj.IsAlive);
    }

    /// <summary>
    /// Gets the number of collection rounds base on the size.
    /// </summary>
    /// <param name="size"></param>
    private int GetCollectionLevelBasedOnSize(long size)
    {
        if (size < m_levelNone)
            return 0;

        if (size < m_levelLow)
            return 1;

        if (size < m_levelNormal)
            return 2;

        if (size < m_levelHigh)
            return 3;

        if (size < m_levelVeryHigh)
            return 4;

        return 5;
    }

    private string GetCollectionLevelString(int iterations)
    {
        return iterations switch
        {
            0 => "0 (None)",
            1 => "1 (Low)",
            2 => "2 (Normal)",
            3 => "3 (High)",
            4 => "4 (Very High)",
            5 => "5 (Critical)",
            _ => iterations + " (Unknown)"
        };
    }

    private void CalculateThresholds(long maximumBufferSize, TargetUtilizationLevels levels)
    {
        switch (levels)
        {
            case TargetUtilizationLevels.Low:
                m_levelNone = (long)(0.1 * maximumBufferSize);
                m_levelLow = (long)(0.25 * maximumBufferSize);
                m_levelNormal = (long)(0.50 * maximumBufferSize);
                m_levelHigh = (long)(0.75 * maximumBufferSize);
                m_levelVeryHigh = (long)(0.90 * maximumBufferSize);
                break;

            case TargetUtilizationLevels.Medium:
                m_levelNone = (long)(0.25 * maximumBufferSize);
                m_levelLow = (long)(0.50 * maximumBufferSize);
                m_levelNormal = (long)(0.75 * maximumBufferSize);
                m_levelHigh = (long)(0.85 * maximumBufferSize);
                m_levelVeryHigh = (long)(0.95 * maximumBufferSize);
                break;

            case TargetUtilizationLevels.High:
                m_levelNone = (long)(0.5 * maximumBufferSize);
                m_levelLow = (long)(0.75 * maximumBufferSize);
                m_levelNormal = (long)(0.85 * maximumBufferSize);
                m_levelHigh = (long)(0.95 * maximumBufferSize);
                m_levelVeryHigh = (long)(0.97 * maximumBufferSize);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(levels));
        }
    }

    /// <summary>
    /// Calculates the limit at which memory pool shrinking should stop.
    /// </summary>
    /// <param name="size">The current size of the memory pool.</param>
    /// <returns>
    /// The limit at which memory pool shrinking should stop, ensuring it's at least 5% of the maximum pool size
    /// or 15% less than the current size, whichever is greater.
    /// </returns>
    /// <remarks>
    /// This method calculates the limit at which memory pool shrinking should stop based on the current size.
    /// The stop limit is set to be at least 5% of the maximum pool size or 15% less than the current size,
    /// whichever is greater.
    /// </remarks>
    private long CalculateStopShrinkingLimit(long size)
    {
        // Once the size has been reduced by 15% of Memory but no less than 5% of memory.
        long stopShrinkingLimit = size - (long)(MaximumPoolSize * 0.15);

        return Math.Max(stopShrinkingLimit, (long)(MaximumPoolSize * 0.05));
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(MemoryPool), MessageClass.Component);

    #endregion
}