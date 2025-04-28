//******************************************************************************************************
//  MemoryPoolStreamCore.cs - Gbtc
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
//  02/10/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Provides a dynamically sizing sequence of unmanaged data.
/// </summary>
public class MemoryPoolStreamCore : IDisposable
{
    #region [ Members ]

    // This class was created to allow settings update to be atomic.
    private class Settings
    {
        #region [ Members ]

        private const int ElementsPerRow = 1024;

        private const int Mask = 1023;
        private const int ShiftBits = 10;

        private int[]?[] m_pageIndex = new int[4][];
        private nint[][] m_pagePointer = new nint[4][];

        #endregion

        #region [ Properties ]

        public bool AddingRequiresClone => m_pagePointer.Length * ElementsPerRow == PageCount;

        public int PageCount { get; private set; }

        #endregion

        #region [ Methods ]

        public nint GetPointer(int page)
        {
            return m_pagePointer[page >> ShiftBits][page & Mask];
        }

        /// <summary>
        /// Adds a new page to the collection of pages.
        /// </summary>
        /// <param name="pagePointer">A pointer to the new page to be added.</param>
        /// <param name="pageIndex">The index of the new page.</param>
        public void AddNewPage(nint pagePointer, int pageIndex)
        {
            // Ensure that there is enough capacity to add a new page.
            EnsureCapacity();

            // Calculate the index for the new page.
            int index = PageCount;
            int bigIndex = index >> ShiftBits; // Calculate the outer array index.
            int smallIndex = index & Mask; // Calculate the inner array index.

            // Assign the page index and page pointer to their respective arrays.
            m_pageIndex[bigIndex]![smallIndex] = pageIndex;
            m_pagePointer[bigIndex][smallIndex] = pagePointer;

            // Ensure memory visibility: Incrementing the page count must occur after the data is correct.
            Thread.MemoryBarrier();

            // Increment the page count to reflect the addition of the new page.
            PageCount++;
        }

        public Settings Clone()
        {
            return (Settings)MemberwiseClone();
        }

        /// <summary>
        /// Returns all of the buffer pool page indexes used by this class.
        /// </summary>
        public IEnumerable<int> GetAllPageIndexes()
        {
            for (int x = 0; x < PageCount; x++)
                yield return GetIndex(x);
        }

        private int GetIndex(int page)
        {
            return m_pageIndex[page >> ShiftBits]![page & Mask];
        }

        private void EnsureCapacity()
        {
            if (AddingRequiresClone)
            {
                int[]?[] oldIndex = m_pageIndex;
                nint[][] oldPointer = m_pagePointer;
                m_pageIndex = new int[m_pageIndex.Length * 2][];
                m_pagePointer = new nint[m_pagePointer.Length * 2][];
                oldIndex.CopyTo(m_pageIndex, 0);
                oldPointer.CopyTo(m_pagePointer, 0);
            }

            int bigIndex = PageCount >> ShiftBits;

            if (m_pageIndex[bigIndex] is null)
            {
                m_pageIndex[bigIndex] = new int[ElementsPerRow];
                m_pagePointer[bigIndex] = new nint[ElementsPerRow];
            }
        }

        #endregion
    }

    // The first position of this stream. This may be different from <see cref="m_firstValidPosition"/>
    // due to alignment requirements.
    private long m_firstAddressablePosition;

    // The first position that can be accessed by users of this stream.
    private long m_firstValidPosition;

    private readonly long m_invertMask;

    // The size of each page.
    private readonly int m_pageSize;

    // The buffer pool to utilize.
    private MemoryPool m_pool;

    private volatile Settings m_settings;

    // The number of bits in the page size.
    private readonly int m_shiftLength;

    private readonly Lock m_syncRoot;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="MemoryPoolStreamCore"/> using the default <see cref="MemoryPool"/>.
    /// </summary>
    public MemoryPoolStreamCore() : this(Globals.MemoryPool)
    {
    }

    /// <summary>
    /// Create a new <see cref="MemoryPoolStreamCore"/>.
    /// </summary>
    /// <param name="pool">The memory pool to associate with the stream.</param>

    public MemoryPoolStreamCore(MemoryPool pool)
    {
        m_pool = pool;
        m_shiftLength = pool.PageShiftBits;
        m_pageSize = pool.PageSize;
        m_invertMask = ~(pool.PageSize - 1);
        m_settings = new Settings();
        m_syncRoot = new Lock();
    }

    /// <summary>
    /// Releases the unmanaged resources before the <see cref="MemoryPoolStreamCore"/> object is reclaimed by <see cref="GC"/>.
    /// </summary>
    ~MemoryPoolStreamCore()
    {
        Dispose(false);
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
        GC.SuppressFinalize(this);
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
            if (disposing && !m_pool.IsDisposed)
                m_pool.ReleasePages(m_settings.GetAllPageIndexes());
        }
        finally
        {
            m_pool = null!;
            m_settings = null!;
            IsDisposed = true;
        }
    }

    // TODO: Consider removing these methods

    /// <summary>
    /// Reads from the underlying stream the requested set of data.
    /// This function is more user friendly than calling GetBlock().
    /// </summary>
    /// <param name="position">The starting position of the read.</param>
    /// <param name="pointer">Ann output pointer to <paramref name="position"/>.</param>
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
    /// Copies data from the current position of the BinaryStream to a specified memory location.
    /// </summary>
    /// <param name="position">The position in the BinaryStream from which to start copying.</param>
    /// <param name="dest">The destination memory location where data will be copied.</param>
    /// <param name="length">The number of bytes to copy.</param>
    /// <remarks>
    /// This method reads data from the BinaryStream starting at the specified position and copies it
    /// to the destination memory location pointed to by the <paramref name="dest"/> parameter. If the
    /// requested length exceeds the valid data available, it copies as much data as possible.
    /// </remarks>
    public void CopyTo(long position, nint dest, int length)
    {
    TryAgain:

        ReadBlock(position, out nint src, out int validLength);

        if (validLength < length)
        {
            Memory.Copy(src, dest, validLength);
            length -= validLength;
            dest += validLength;
            position += validLength;

            goto TryAgain;
        }

        Memory.Copy(src, dest, length);
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

        if (args.FirstPosition < m_firstValidPosition)
        {
            args.FirstPointer += (int)(m_firstValidPosition - args.FirstPosition);
            args.Length -= (int)(m_firstValidPosition - args.FirstPosition);
            args.FirstPosition = m_firstValidPosition;
        }
    }

    /// <summary>
    /// Returns the page that corresponds to the absolute position.
    /// This function will also auto-grow the stream.
    /// </summary>
    /// <param name="position">The position to use to calculate the page to retrieve</param>
    /// <returns>The memory page as a native pointer (nint).</returns>
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
    /// Increases the size of the Memory Stream and updated the settings if needed
    /// </summary>
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

            //If there are not enough pages in the stream, add enough.
            while (pageCount > settings.PageCount)
            {
                m_pool.AllocatePage(out int pageIndex, out nint pagePointer);
                Memory.Clear(pagePointer, m_pool.PageSize);
                settings.AddNewPage(pagePointer, pageIndex);
            }

            if (cloned)
            {
                Thread.MemoryBarrier(); // make sure that all of the settings are saved before assigning
                m_settings = settings;
            }
        }
    }

    #endregion
}