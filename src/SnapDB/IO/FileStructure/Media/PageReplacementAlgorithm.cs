//******************************************************************************************************
//  PageReplacementAlgorithm.cs - Gbtc
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
//  02/01/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;
using Gemstone.Diagnostics;
using SnapDB.Collections;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// A page replacement algorithm that utilizes a quasi LRU algorithm. This class is thread safe.
/// </summary>
/// <remarks>
/// This class is used by <see cref="BufferedFile"/> to decide which pages should be replaced.
/// </remarks>
internal partial class PageReplacementAlgorithm : IDisposable
{
    #region [ Members ]

    /// <summary>
    /// Contains the currently active I/O sessions.
    /// </summary>
    private readonly WeakList<PageLock> m_arrayIndexLocks;

    private readonly long m_maxValidPosition;
    private readonly int m_memoryPageSizeMask;
    private readonly int m_memoryPageSizeShiftBits;

    /// <summary>
    /// Contains a list of all the memory pages.
    /// </summary>
    /// <remarks>These items in the list are not in any particular order.</remarks>
    private readonly PageList m_pageList;

    private readonly Lock m_syncRoot;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="PageReplacementAlgorithm"/> class with the specified memory pool.
    /// </summary>
    /// <param name="pool">The memory pool to be used for page replacement.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified memory pool has a page size less than 4096.</exception>
    /// <exception cref="ArgumentException">Thrown when the page size of the specified memory pool is not a power of 2.</exception>
    public PageReplacementAlgorithm(MemoryPool pool)
    {
        if (pool.PageSize < 4096)
            throw new ArgumentOutOfRangeException(nameof(pool), "PageSize Must be greater than 4096");

        if (!BitMath.IsPowerOfTwo(pool.PageSize))
            throw new ArgumentException("PageSize Must be a power of 2", nameof(pool));

        m_maxValidPosition = (int.MaxValue - 1) * (long)pool.PageSize; // Max position 

        m_syncRoot = new Lock();
        m_memoryPageSizeMask = pool.PageSize - 1;
        m_memoryPageSizeShiftBits = BitMath.CountBitsSet((uint)m_memoryPageSizeMask);
        m_pageList = new PageList(pool);
        m_arrayIndexLocks = new WeakList<PageLock>();
    }

#if DEBUG
    ~PageReplacementAlgorithm()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
        // Don't dispose since only the page list contains data that must be released.
    }
#endif

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="PageReplacementAlgorithm"/> object.
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
            return;

        lock (m_syncRoot)
        {
            try
            {
                m_pageList.Dispose();
            }
            finally
            {
                GC.SuppressFinalize(this);
                m_disposed = true; // Prevent duplicate dispose.
            }
        }
    }

    // Two Methods Exist in PageLock subclass:
    // TryGetSubPage
    // GetOrAddPage

    /// <summary>
    /// Attempts to add the page to this <see cref="PageReplacementAlgorithm"/>.
    /// Fails if the page already exists.
    /// </summary>
    /// <param name="position">The absolute position that the page references</param>
    /// <param name="locationOfPage">The pointer to the page</param>
    /// <param name="memoryPoolIndex">The index value of the memory pool page so it can be released back to the memory pool.</param>
    /// <returns><c>true</c> if the page was added to the class; <c>false</c> if the page already exists and the data was not replaced.</returns>
    public bool TryAddPage(long position, nint locationOfPage, int memoryPoolIndex)
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Cannot be negative");

            if (position > m_maxValidPosition)
                throw new ArgumentOutOfRangeException(nameof(position), "Position index can no longer be specified as an Int32");

            if ((position & m_memoryPageSizeMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(position), "must lie on a page boundary");

            int positionIndex = (int)(position >> m_memoryPageSizeShiftBits);

            if (m_pageList.TryGetPageIndex(positionIndex, out int _))
                return false;

            m_pageList.AddNewPage(positionIndex, locationOfPage, memoryPoolIndex);

            return true;
        }
    }

    /// <summary>
    /// Performs memory pool collection, releasing unused pages based on the specified collection parameters.
    /// </summary>
    /// <param name="e">The collection event arguments specifying the collection operation details.</param>
    /// <returns>The number of pages collected and released.</returns>
    public int DoCollection(CollectionEventArgs e)
    {
        lock (m_syncRoot)
        {
            if (m_disposed)
                return 0;

            HashSet<int> pages = new(m_arrayIndexLocks.Select(pageLock => pageLock.CurrentPageIndex));

            return m_pageList.DoCollection(1, pages, e);
        }
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(PageReplacementAlgorithm), MessageClass.Component);

    #endregion
}