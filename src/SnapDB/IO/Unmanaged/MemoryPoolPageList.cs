//******************************************************************************************************
//  MemoryPoolPageList.cs - Gbtc
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
//  06/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/21/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

#pragma warning disable CS0649

using System.Runtime.InteropServices;
using Gemstone;
using Gemstone.Console;
using Gemstone.Diagnostics;
using Gemstone.Units;
using SnapDB.Collections;
using SnapDB.Threading;

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Maintains a list of all of the memory allocations for the buffer pool.
/// </summary>
/// <remarks>
/// This class is not thread-safe.
/// </remarks>
internal class MemoryPoolPageList : IDisposable
{
    #region [ Members ]

    // ReSharper disable IdentifierTypo
    // ReSharper disable InconsistentNaming
    // ReSharper disable NotAccessedField.Local
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        #region [ Members ]

        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullAvailExtendedVirtual;
        public ulong ullAvailPageFile;
        public ulong ullAvailPhys;
        public ulong ullAvailVirtual;
        public ulong ullTotalPageFile;
        public ulong ullTotalPhys;
        public ulong ullTotalVirtual;

        #endregion

        #region [ Constructors ]

        // ReSharper disable once ConvertConstructorToMemberInitializers
        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }

        #endregion
    }
    // ReSharper restore NotAccessedField.Local
    // ReSharper restore InconsistentNaming
    // ReSharper restore IdentifierTypo

    /// <summary>
    /// Defines the maximum supported number of bytes that can be allocated based
    /// on the amount of RAM in the system. This is not user-configurable.
    /// </summary>
    public readonly long MemoryPoolCeiling;

    /// <summary>
    /// Defines number of bytes in RAM that a page will exactly occupy.
    /// </summary>
    public readonly int PageSize;

    // A bit array that references each page and determines if the page is free.
    // Set means page is free, cleared means page is either being used, or was never allocated from Windows.
    private readonly BitArray m_isPageFree;

    private readonly AtomicInt64 m_maximumPoolSize = new();

    // Gets the number of unmanaged memory blocks that have been allocated.
    private int m_memoryBlockAllocations;

    // Points to each windows API allocation.
    private readonly List<Memory?> m_memoryBlocks;

    // The number of bytes per Windows API allocation block.
    private readonly int m_memoryBlockSize;

    // The number of pages that exist within an unmanaged memory allocation.
    private readonly int m_pagesPerMemoryBlock;
    private readonly int m_pagesPerMemoryBlockMask;
    private readonly int m_pagesPerMemoryBlockShiftBits;
    private readonly object m_syncRoot;

    // The number of pages that have been used.
    private int m_usedPageCount;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Create a thread safe list of MemoryPool pages.
    /// </summary>
    /// <param name="pageSize">The desired page size. Must be between 4KB and 256KB.</param>
    /// <param name="maximumBufferSize">
    /// The desired maximum size of the allocation. Note: could be less if there is not enough system memory.
    /// A value of -1 will default based on available system memory.
    /// </param>
    public MemoryPoolPageList(int pageSize, long maximumBufferSize)
    {
        m_syncRoot = new object();
        PageSize = pageSize;

        long totalMemory = (long)GetTotalPhysicalMemory();
        long availableMemory = (long)GetAvailablePhysicalMemory();

        if (!Environment.Is64BitProcess)
        {
            s_log.Publish(MessageLevel.Info, "Process running in 32-bit mode. Memory Pool is Limited in size.");
            totalMemory = Math.Min(int.MaxValue, totalMemory); // Clip at 2GB
            availableMemory = Math.Min(int.MaxValue - GC.GetTotalMemory(false), availableMemory); // Clip at 2GB
        }

        m_memoryBlockSize = CalculateMemoryBlockSize(PageSize, totalMemory);
        MemoryPoolCeiling = CalculateMemoryPoolCeiling(m_memoryBlockSize, totalMemory);

        if (maximumBufferSize < 0)
        {
            // Maximum size defaults to the larger of:
            // 50% of the free ram.
            // 25% of the total system memory.
            MaximumPoolSize = Math.Max(MemoryPool.MinimumTestedSupportedMemoryFloor, availableMemory / 2);
            MaximumPoolSize = Math.Max(MaximumPoolSize, totalMemory / 4);
        }
        else
        {
            MaximumPoolSize = Math.Max(Math.Min(MemoryPoolCeiling, maximumBufferSize), MemoryPool.MinimumTestedSupportedMemoryFloor);
        }

        s_log.Publish(MessageLevel.Info, "Memory Pool Maximum Defaulted To: " + MaximumPoolSize / 1024 / 1024 + "MB of Ram");

        m_memoryBlockAllocations = 0;
        m_usedPageCount = 0;
        m_pagesPerMemoryBlock = m_memoryBlockSize / PageSize;
        m_pagesPerMemoryBlockMask = m_pagesPerMemoryBlock - 1;
        m_pagesPerMemoryBlockShiftBits = BitMath.CountBitsSet((uint)m_pagesPerMemoryBlockMask);
        m_isPageFree = new BitArray(false);
        m_memoryBlocks = new List<Memory?>();
    }

#if DEBUG
    ~MemoryPoolPageList()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Returns the number of bytes allocated by all buffer pools.
    /// This does not include any pages that have been allocated but are not in use.
    /// </summary>
    public long CurrentAllocatedSize => m_usedPageCount * (long)PageSize;

    /// <summary>
    /// Gets the number of bytes that have been allocated to this buffer pool
    /// by the OS.
    /// </summary>
    public long CurrentCapacity => m_memoryBlockAllocations * (long)m_memoryBlockSize;

    /// <summary>
    /// Gets if there is any free space.
    /// </summary>
    public long FreeSpaceBytes => Math.Max(CurrentCapacity - CurrentAllocatedSize, 0); // In case a race condition yields a negative value.

    /// <summary>
    /// Gets if the pool is currently full.
    /// </summary>
    public bool IsFull => CurrentCapacity == CurrentAllocatedSize;

    /// <summary>
    /// The maximum amount of RAM that this memory pool is configured to support.
    /// Attempting to allocate more than this will cause an out of memory exception
    /// </summary>
    public long MaximumPoolSize
    {
        get => m_maximumPoolSize.Value;
        private set => m_maximumPoolSize.Value = value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Disposes of all of the memory on the list.
    /// </summary>
    public void Dispose()
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return;

            try
            {
                foreach (Memory? block in m_memoryBlocks)
                    block?.Dispose();

                m_memoryBlockAllocations = 0;
                m_isPageFree.ClearAll();
                m_memoryBlocks.Clear();
            }
            finally
            {
                m_disposed = true; // Prevent duplicate dispose.
            }
        }
    }

    /// <summary>
    /// Requests a new block from the buffer pool.
    /// </summary>
    /// <param name="index">The index identifier of the block.</param>
    /// <param name="addressPointer">The address to the start of the block.</param>
    /// <exception cref="OutOfMemoryException">Thrown if the list is full.</exception>
    public bool TryGetNextPage(out int index, out nint addressPointer)
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            index = m_isPageFree.FindSetBit();

            if (index < 0)
            {
                index = -1;
                addressPointer = nint.Zero;

                return false;
            }

            m_usedPageCount++;
            m_isPageFree.ClearBit(index);

            int allocationIndex = index >> m_pagesPerMemoryBlockShiftBits;
            int blockOffset = index & m_pagesPerMemoryBlockMask;
            Memory? block = m_memoryBlocks[allocationIndex];

            if (block is null)
            {
                s_log.Publish(MessageLevel.Warning, MessageFlags.BugReport, "Memory Block inside Memory Pool is null. Possible race condition.");
                throw new NullReferenceException("Memory Block is null");
            }

            if (block.Address == nint.Zero)
            {
                s_log.Publish(MessageLevel.Warning, MessageFlags.BugReport, "Memory Block inside Memory Pool was released prematurely. Possible race condition.");
                throw new NullReferenceException("Memory Block is null");
            }

            addressPointer = block.Address + blockOffset * PageSize;

            index++;

            return true;
        }
    }

    /// <summary>
    /// Releases a block back to the pool so it can be re-allocated.
    /// </summary>
    /// <param name="index">The index identifier of the block.</param>
    public void ReleasePage(int index)
    {
        if (index > 0)
        {
            index--;

            lock (m_syncRoot)
            {
                if (m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (m_isPageFree.TrySetBit(index))
                {
                    // IntPtr page = GetPageAddress(index);
                    // Memory.Clear(page,PageSize);
                    m_usedPageCount--;

                    return;
                }
            }
        }

        s_log.Publish(MessageLevel.Warning, MessageFlags.BugReport, "A page has been released twice. Some code somewhere could create memory corruption");

        throw new Exception("Cannot have duplicate calls to release pages.");
    }

    /// <summary>
    /// Tries to shrink the buffer pool to the provided size.
    /// </summary>
    /// <param name="size">The size of the buffer pool.</param>
    /// <returns>The final size of the buffer pool.</returns>
    /// <remarks>The buffer pool shrinks to a size less than or equal to <paramref name="size"/>.</remarks>
    public long ShrinkMemoryPool(long size)
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (CurrentCapacity <= size)
                return CurrentCapacity;

            for (int x = 0; x < m_memoryBlocks.Capacity; x++)
            {
                if (m_memoryBlocks[x] is null)
                    continue;

                if (!m_isPageFree.AreAllBitsCleared(x * m_pagesPerMemoryBlock, m_pagesPerMemoryBlock))
                    continue;

                m_memoryBlocks[x]!.Dispose();
                m_memoryBlocks[x] = null;
                m_memoryBlockAllocations--;

                if (CurrentCapacity <= size)
                    return CurrentCapacity;
            }

            return CurrentCapacity;
        }
    }

    /// <summary>
    /// Changes the allowable buffer size.
    /// </summary>
    /// <param name="value">The number of bytes to set.</param>
    public long SetMaximumPoolSize(long value)
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            MaximumPoolSize = Math.Max(Math.Min(MemoryPoolCeiling, value), MemoryPool.MinimumTestedSupportedMemoryFloor);

            return MaximumPoolSize;
        }
    }

    /// <summary>
    /// Grows the buffer pool to have the desired size.
    /// </summary>
    public bool GrowMemoryPool(long size)
    {
        long sizeBefore;
        long sizeAfter;

        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            sizeBefore = CurrentCapacity;

            size = Math.Min(size, MaximumPoolSize);

            while (CurrentCapacity < size)
            {
                Memory memory = new(m_memoryBlockSize);
                int pageIndex = m_memoryBlocks.ReplaceFirstNullOrAdd(memory);
                m_memoryBlockAllocations++;
                m_isPageFree.EnsureCapacity((pageIndex + 1) * m_pagesPerMemoryBlock);
                m_isPageFree.SetBits(pageIndex * m_pagesPerMemoryBlock, m_pagesPerMemoryBlock);
            }

            sizeAfter = CurrentCapacity;
        }

        return sizeBefore != sizeAfter;
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(MemoryPoolPageList), MessageClass.Component);

    /// <summary>
    /// Calculates the memory pool ceiling based on the memory block size and the total physical system memory.
    /// </summary>
    /// <param name="memoryBlockSize">The size of memory blocks used by the memory pool.</param>
    /// <param name="systemTotalPhysicalMemory">The total physical memory available in the system, in bytes.</param>
    /// <returns>The calculated memory pool ceiling, rounded down to the nearest allocation size.</returns>
    private static long CalculateMemoryPoolCeiling(int memoryBlockSize, long systemTotalPhysicalMemory)
    {
        // Initialize the size with the maximum tested supported memory ceiling.
        long size = MemoryPool.MaximumTestedSupportedMemoryCeiling;

        // Calculate the physical upper limit, which is the greater of 50% of memory or all but 8 GB of RAM.
        long physicalUpperLimit = Math.Max(systemTotalPhysicalMemory / 2, systemTotalPhysicalMemory - 8 * 1024 * 1024 * 1024L);

        // Limit the size to the calculated physical upper limit.
        size = Math.Min(size, physicalUpperLimit);

        // Round down the size to the nearest allocation size (memory block size).
        size -= size % memoryBlockSize; // Rounds down to the nearest allocation size.

        // Return the calculated memory pool ceiling.
        return size;
    }

    /// <summary>
    /// Calculates the desired allocation block size to request from the OS.
    /// </summary>
    /// <param name="pageSize">The size of each page.</param>
    /// <param name="totalSystemMemory">The total amount of system memory.</param>
    /// <returns>The recommended block size.</returns>
    /// <remarks>
    /// The recommended block size is the <paramref name="totalSystemMemory"/> divided by 1000
    /// but must be a multiple of the system allocation size and the page size and cannot be larger than 1GB
    /// </remarks>
    private static int CalculateMemoryBlockSize(int pageSize, long totalSystemMemory)
    {
        long targetMemoryBlockSize = totalSystemMemory / 1000;

        targetMemoryBlockSize = Math.Min(targetMemoryBlockSize, 1024 * 1024 * 1024);
        targetMemoryBlockSize -= targetMemoryBlockSize % pageSize;
        targetMemoryBlockSize = (int)Math.Max(targetMemoryBlockSize, pageSize);
        targetMemoryBlockSize = (int)BitMath.RoundUpToNearestPowerOfTwo((ulong)targetMemoryBlockSize);

        return (int)targetMemoryBlockSize;
    }

    // Gets the total physical system memory.
    private static ulong GetTotalPhysicalMemory()
    {
        if (Common.IsPosixEnvironment)
        {
            string output = Command.Execute("awk", "'/MemTotal/ {print $2}' /proc/meminfo").StandardOutput;
            return ulong.Parse(output) * SI2.Kilo;
        }

        MEMORYSTATUSEX memStatus = new();
        return GlobalMemoryStatusEx(memStatus) ? memStatus.ullTotalPhys : 0;
    }

    // Gets the available physical system memory.
    private static ulong GetAvailablePhysicalMemory()
    {
        if (Common.IsPosixEnvironment)
        {
            string output = Command.Execute("awk", "'/MemAvailable/ {print $2}' /proc/meminfo").StandardOutput;
            return ulong.Parse(output) * SI2.Kilo;
        }

        MEMORYSTATUSEX memStatus = new();
        return GlobalMemoryStatusEx(memStatus) ? memStatus.ullAvailPhys : 0;
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In] [Out] MEMORYSTATUSEX lpBuffer);

    #endregion
}