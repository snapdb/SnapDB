//******************************************************************************************************
//  PageList.cs - Gbtc
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

using SnapDB.Collections;
using SnapDB.IO.Unmanaged;
using Gemstone.Diagnostics;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Contains a list of page meta data. Provides a simplified way to interact with this list.
/// This class is not thread safe.
/// </summary>
internal sealed unsafe class PageList
    : IDisposable
{
    private static readonly LogPublisher Log = Logger.CreatePublisher(typeof(PageList), MessageClass.Component);


    #region [ Members ]

    /// <summary>
    /// The internal data stored about each page. This is address information, Position information
    /// </summary>
    private struct InternalPageMetaData
    {
        /// <summary>
        /// The pointer to the page.
        /// </summary>
        public byte* LocationOfPage;

        /// <summary>
        /// The index assigned by the <see cref="MemoryPool"/>.
        /// </summary>
        public int MemoryPoolIndex;

        /// <summary>
        /// The number of times this page has been referenced.
        /// </summary>
        public int ReferencedCount;
    }

    /// <summary>
    /// Note: Memory pool must not be used to allocate memory since this is a blocking method.
    /// Otherwise, there exists the potential to deadlock.
    /// </summary>
    private readonly MemoryPool m_memoryPool;

    /// <summary>
    /// Contains all of the pages that are cached for the file stream.
    /// Map is PositionIndex,PageIndex
    /// </summary>
    private readonly GenericSortedList<int, int> m_pageIndexLookupByPositionIndex;

    /// <summary>
    /// A list of all pages that have been cached.
    /// </summary>
    private NullableLargeArray<InternalPageMetaData> m_listOfPages;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new PageMetaDataList.
    /// </summary>
    /// <param name="memoryPool">The buffer pool to utilize if any unmanaged memory needs to be created.</param>
    public PageList(MemoryPool memoryPool)
    {
        m_memoryPool = memoryPool;
        m_listOfPages = new NullableLargeArray<InternalPageMetaData>();
        m_pageIndexLookupByPositionIndex = new GenericSortedList<int, int>();
    }

    ~PageList()
    {
        Log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
        Dispose();
    }

    #endregion

    #region [ Properties ]

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Converts a number from its position index into a page index.
    /// </summary>
    /// <param name="positionIndex">the position divided by the page size.</param>
    /// <param name="pageIndex">the page index</param>
    /// <returns><c>true</c> if found, <c>false</c> if not found.</returns>
    public bool TryGetPageIndex(int positionIndex, out int pageIndex)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        return m_pageIndexLookupByPositionIndex.TryGetValue(positionIndex, out pageIndex);
    }

    /// <summary>
    /// Adds a new page to the cache and associates it with the specified position index.
    /// </summary>
    /// <param name="positionIndex">The position index to associate with the new page.</param>
    /// <param name="locationOfPage">A pointer to the location of the new page in memory.</param>
    /// <param name="memoryPoolIndex">The memory pool index to which the page belongs.</param>
    /// <returns>The index of the newly added page in the cache.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the cache is disposed.</exception>
    /// <remarks>
    /// This method adds a new page to the cache and associates it with the specified
    /// <paramref name="positionIndex"/>. It also tracks the memory pool index and the
    /// location of the page in memory. The method returns the index of the newly added
    /// page in the cache.
    /// </remarks>
    public int AddNewPage(int positionIndex, IntPtr locationOfPage, int memoryPoolIndex)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        InternalPageMetaData cachePage;
        cachePage.MemoryPoolIndex = memoryPoolIndex;
        cachePage.LocationOfPage = (byte*)locationOfPage;
        cachePage.ReferencedCount = 0;
        int pageIndex = m_listOfPages.AddValue(cachePage);
        m_pageIndexLookupByPositionIndex.Add(positionIndex, pageIndex);

        return pageIndex;
    }


    /// <summary>
    /// Gets a pointer to the memory location of a cached page and optionally increments its reference count.
    /// </summary>
    /// <param name="pageIndex">The index of the cached page.</param>
    /// <param name="incrementReferencedCount">
    /// The value by which to increment the reference count of the cached page. Use 0 to retrieve the pointer without incrementing.
    /// </param>
    /// <returns>
    /// A pointer to the memory location of the cached page.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown if the cache is disposed.</exception>
    /// <remarks>
    /// This method retrieves a pointer to the memory location of a cached page identified by its <paramref name="pageIndex"/>.
    /// Optionally, you can specify <paramref name="incrementReferencedCount"/> to increment the reference count of the page.
    /// If the reference count exceeds <see cref="int.MaxValue"/> or goes below 0, it's clamped to the respective boundary.
    /// </remarks>
    public IntPtr GetPointerToPage(int pageIndex, int incrementReferencedCount)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        InternalPageMetaData metaData = m_listOfPages.GetValue(pageIndex);
        if (incrementReferencedCount > 0)
        {
            long newValue = metaData.ReferencedCount + (long)incrementReferencedCount;

            if (newValue > int.MaxValue)
            {
                metaData.ReferencedCount = int.MaxValue;
            }

            else if (newValue < 0)
            {
                metaData.ReferencedCount = 0;
            }

            else
            {
                metaData.ReferencedCount = (int)newValue;
            }

            m_listOfPages.OverwriteValue(pageIndex, metaData);
        }

        return (IntPtr)metaData.LocationOfPage;
    }

    /// <summary>
    /// Performs memory pool collection, releasing unused pages based on the specified collection parameters.
    /// </summary>
    /// <param name="shiftLevel">The number of bits to shift the reference count right before evaluating for collection.</param>
    /// <param name="excludedList">
    /// A set of page indices to exclude from collection. Pages in this set will not be released, even if their reference count is zero.
    /// </param>
    /// <param name="e">The collection event arguments containing collection mode and desired release count.</param>
    /// <returns>The number of pages actually collected and released.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the cache is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="shiftLevel"/> is negative.</exception>
    /// <remarks>
    /// This method performs memory pool collection by iterating through cached pages, shifting their reference counts right by <paramref name="shiftLevel"/> bits,
    /// and releasing pages whose reference count becomes zero, excluding those in <paramref name="excludedList"/>.
    /// The number of pages collected is limited by <paramref name="e"/>.DesiredPageReleaseCount if the collection mode is Emergency or Critical.
    /// </remarks>
    public int DoCollection(int shiftLevel, HashSet<int> excludedList, CollectionEventArgs e)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (shiftLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(shiftLevel), "must be non negative");

        int collectionCount = 0;
        int maxCollectCount = -1;
        if (e.CollectionMode is MemoryPoolCollectionMode.Emergency or MemoryPoolCollectionMode.Critical)
        {
            maxCollectCount = e.DesiredPageReleaseCount;
        }

        for (int x = 0; x < m_pageIndexLookupByPositionIndex.Count; x++)
        {
            int pageIndex = m_pageIndexLookupByPositionIndex.Values[x];

            InternalPageMetaData block = m_listOfPages.GetValue(pageIndex);
            block.ReferencedCount >>= shiftLevel;
            m_listOfPages.OverwriteValue(pageIndex, block);
            if (block.ReferencedCount == 0)
            {
                if (maxCollectCount != collectionCount)
                {
                    if (!excludedList.Contains(pageIndex))
                    {
                        collectionCount++;
                        m_pageIndexLookupByPositionIndex.RemoveAt(x);
                        x--;
                        m_listOfPages.SetNull(pageIndex);
                        e.ReleasePage(block.MemoryPoolIndex);
                    }
                }
            }
        }

        return collectionCount;
    }

    /// <summary>
    /// Releases all the resources used by the <see cref="PageList"/> object.
    /// </summary>
    public void Dispose()
    {
        if (!m_disposed)
        {
            try
            {
                if (!m_memoryPool.IsDisposed)
                {
                    m_memoryPool.ReleasePages(m_listOfPages.Select(x => x.MemoryPoolIndex));
                    m_listOfPages = null;
                }
            }

            catch (Exception ex)
            {
                Log.Publish(MessageLevel.Critical, "Unhandled exception when returning resources to the memory pool", null, null, ex);
            }

            finally
            {
                GC.SuppressFinalize(this);
                m_disposed = true; // Prevent duplicate dispose.
            }
        }
    }

    #endregion
}