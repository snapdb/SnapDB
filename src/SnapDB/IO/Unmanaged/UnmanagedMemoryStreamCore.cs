﻿//******************************************************************************************************
//  UnmanagedMemoryStreamCore.cs - Gbtc
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
//  09/30/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Represents settings and storage for managing IntPtr arrays used for memory management.
/// </summary>
public class UnmanagedMemoryStreamCore : IDisposable
{
    #region [ Members ]

    // This class was created to allow settings update to be atomic.
    private class Settings
    {
        #region [ Members ]

        private const int ElementsPerRow = 64;

        // Constants used for array manipulation.
        private const int Mask = 63;
        private const int ShiftBits = 6;

        // Array to store IntPtr pointers for memory management.
        private nint[]?[] m_pagePointer;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
            m_pagePointer = new nint[4][];
            PageCount = 0;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Determines if adding a new page requires cloning the array to increase capacity.
        /// </summary>
        public bool AddingRequiresClone => m_pagePointer.Length * ElementsPerRow == PageCount;

        /// <summary>
        /// Gets the total number of pages stored in the memory manager.
        /// </summary>
        public int PageCount { get; private set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Gets the IntPtr pointer at the specified page index.
        /// </summary>
        /// <param name="page">The index of the page.</param>
        /// <returns>The IntPtr pointer at the specified page index.</returns>
        public nint GetPointer(int page)
        {
            return m_pagePointer[page >> ShiftBits]![page & Mask];
        }

        /// <summary>
        /// Adds a new page represented by an IntPtr pointer to the memory manager.
        /// </summary>
        /// <param name="pagePointer">The IntPtr pointer of the new page.</param>
        public void AddNewPage(nint pagePointer)
        {
            EnsureCapacity();

            int index = PageCount;
            int bigIndex = index >> ShiftBits;
            int smallIndex = index & Mask;
            m_pagePointer[bigIndex]![smallIndex] = pagePointer;

            Thread.MemoryBarrier(); // Incrementing the page count must occur after the data is correct.
            PageCount++;
        }

        /// <summary>
        /// Creates a shallow clone of the <see cref="Settings"/> instance.
        /// </summary>
        /// <returns>A shallow clone of the <see cref="Settings"/> instance.</returns>
        public Settings Clone()
        {
            return (Settings)MemberwiseClone();
        }

        /// <summary>
        /// Ensures the array capacity is sufficient to add a new page.
        /// </summary>
        private void EnsureCapacity()
        {
            if (AddingRequiresClone)
            {
                nint[]?[] oldPointer = m_pagePointer;

                m_pagePointer = new nint[m_pagePointer.Length * 2][];
                oldPointer.CopyTo(m_pagePointer, 0);
            }

            int bigIndex = PageCount >> ShiftBits;
            m_pagePointer[bigIndex] ??= new nint[ElementsPerRow];
        }

        #endregion
    }

    // The first position of this stream. This may be different from <see cref="m_firstValidPosition"/>
    // due to alignment requirements.
    private long m_firstAddressablePosition;

    // The first position that can be accessed by users of this stream.
    private long m_firstValidPosition;

    private readonly long m_invertMask;

    private List<Memory> m_memoryBlocks;

    // The size of each page.
    private readonly int m_pageSize;

    private volatile Settings m_settings;

    // The number of bits in the page size.
    private readonly int m_shiftLength;

    private readonly Lock m_syncRoot;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Create a new <see cref="UnmanagedMemoryStreamCore"/> that allocates its own unmanaged memory.
    /// </summary>
    /// <param name="allocationSize">The set definition for the allocation memory capacity.</param>
    protected UnmanagedMemoryStreamCore(int allocationSize = 4096)
    {
        if (!BitMath.IsPowerOfTwo(allocationSize) || allocationSize < 4096 || allocationSize > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(allocationSize), "Must be a power of 2 between 4KB and 1MB");

        m_shiftLength = BitMath.CountBitsSet((uint)(allocationSize - 1));
        m_pageSize = allocationSize;
        m_invertMask = ~(allocationSize - 1);
        m_settings = new Settings();
        m_syncRoot = new Lock();
        m_memoryBlocks = [];
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets if the stream has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the length of the current stream.
    /// </summary>
    public long Length => (long)m_pageSize * m_settings.PageCount;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the memory file object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        try
        {
            if (!disposing)
                return;

            foreach (Memory page in m_memoryBlocks)
                page.Dispose();
        }
        finally
        {
            m_memoryBlocks = null!;
            m_settings = null!;
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Reads from the underlying stream the requested set of data.
    /// This function is more user friendly than calling GetBlock().
    /// </summary>
    /// <param name="position">The starting position of the read.</param>
    /// <param name="pointer">An output pointer to <paramref name="position"/>.</param>
    /// <param name="validLength">The number of bytes that are valid after this position.</param>
    public void ReadBlock(long position, out nint pointer, out int validLength)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("MemoryStream");

        if (position < m_firstValidPosition)
            throw new ArgumentOutOfRangeException(nameof(position), "position is before the beginning of the stream");

        validLength = m_pageSize;
        long firstPosition = ((position - m_firstAddressablePosition) & m_invertMask) + m_firstAddressablePosition;
        pointer = GetPage(position - m_firstAddressablePosition);

        if (firstPosition < m_firstValidPosition)
        {
            pointer += (int)(m_firstValidPosition - firstPosition);
            validLength -= (int)(m_firstValidPosition - firstPosition);
            firstPosition = m_firstValidPosition;
        }

        int seekDistance = (int)(position - firstPosition);
        validLength -= seekDistance;
        pointer += seekDistance;
    }

    /// <summary>
    /// Configure the natural alignment of the data.
    /// </summary>
    /// <param name="startPosition">The first addressable position.</param>
    public void ConfigureAlignment(long startPosition)
    {
        ConfigureAlignment(startPosition, 1);
    }

    /// <summary>
    /// Configure the natural alignment of the data.
    /// </summary>
    /// <param name="startPosition">The first addressable position.</param>
    /// <param name="alignment">
    /// Forces alignment on this boundary.
    /// Alignment must be a factor of the BufferPool's page boundary.
    /// </param>
    public void ConfigureAlignment(long startPosition, int alignment)
    {
        if (startPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(startPosition), "Cannot be negative");

        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment), "Must be a positive factor of the buffer pool's page size.");

        if (alignment > m_pageSize)
            throw new ArgumentOutOfRangeException(nameof(alignment), "Cannot be greater than the buffer pool's page size.");

        if (m_pageSize % alignment != 0)
            throw new ArgumentException("Must be an even factor of the buffer pool's page size", nameof(alignment));

        m_firstValidPosition = startPosition;
        m_firstAddressablePosition = startPosition - startPosition % alignment;
    }

    /// <summary>
    /// Gets a block for the following IO session.
    /// </summary>
    /// <param name="args">The BlockArguments specifying the block's position and length.</param>

    public void GetBlock(BlockArguments args)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("MemoryStream");

        if (args.Position < m_firstValidPosition)
            throw new ArgumentOutOfRangeException(nameof(args), "position is before the beginning of the stream");

        args.Length = m_pageSize;
        args.FirstPosition = ((args.Position - m_firstAddressablePosition) & m_invertMask) + m_firstAddressablePosition;
        args.FirstPointer = GetPage(args.Position - m_firstAddressablePosition);

        if (args.FirstPosition >= m_firstValidPosition)
            return;

        args.FirstPointer += (int)(m_firstValidPosition - args.FirstPosition);
        args.Length -= (int)(m_firstValidPosition - args.FirstPosition);
        args.FirstPosition = m_firstValidPosition;
    }

    /// <summary>
    /// Retrieves the memory page associated with the specified position.
    /// </summary>
    /// <param name="position">The position for which to retrieve the memory page.</param>
    /// <returns>
    /// A pointer to the memory page that corresponds to the given position.
    /// </returns>
    /// <remarks>
    /// This method retrieves the memory page from the memory pool associated with the specified position.
    /// If the requested page does not exist, it may increase the page count to accommodate the position.
    /// </remarks>
    private nint GetPage(long position)
    {
        Settings settings = m_settings;

        int pageIndex = (int)(position >> m_shiftLength);

        if (pageIndex >= settings.PageCount)
        {
            IncreasePageCount(pageIndex + 1);
            settings = m_settings;
        }

        return settings.GetPointer(pageIndex);
    }

    /// <summary>
    /// Increases the page count of the memory pool to accommodate the specified number of pages.
    /// </summary>
    /// <param name="pageCount">The desired number of pages to be accommodated.</param>
    /// <remarks>
    /// This method increases the page count of the memory pool to ensure it can accommodate the specified number of pages.
    /// If the current settings require cloning, a clone of the settings is made before adding new pages.
    /// Each new page is allocated in memory and initialized with zeroes.
    /// </remarks>
    private void IncreasePageCount(int pageCount)
    {
        lock (m_syncRoot)
        {
            bool cloned = false;
            Settings settings = m_settings;

            if (settings.AddingRequiresClone)
            {
                cloned = true;
                settings = settings.Clone();
            }

            // If there are not enough pages in the stream, add enough.
            while (pageCount > settings.PageCount)
            {
                Memory block = new(m_pageSize);
                nint pagePointer = block.Address;
                m_memoryBlocks.Add(block);
                Memory.Clear(pagePointer, m_pageSize);
                settings.AddNewPage(pagePointer);
            }

            if (!cloned)
                return;

            Thread.MemoryBarrier(); // Make sure that all of the settings are saved before assigning.
            m_settings = settings;
        }
    }

    #endregion
}