//******************************************************************************************************
//  PageLock.cs - Gbtc
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
//  02/09/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

internal partial class PageReplacementAlgorithm
{
    #region [ Members ]

    /// <summary>
    /// Used to hold a lock on a page to prevent it from being collected by the collection engine.
    /// </summary>
    internal abstract class PageLock : BinaryStreamIoSessionBase, IEquatable<PageLock>
    {
        #region [ Members ]

        private readonly int m_hashCode;
        private readonly PageReplacementAlgorithm m_parent;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates an unallocated block.
        /// </summary>
        protected PageLock(PageReplacementAlgorithm parent)
        {
            m_hashCode = DateTime.UtcNow.Ticks.GetHashCode();
            m_parent = parent;
            CurrentPageIndex = -1;

            lock (m_parent.m_syncRoot)
            {
                if (m_parent.m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                m_parent.m_arrayIndexLocks.Add(this);
            }
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the page index associated with the page
        /// that is cached.
        /// Returns a -1 if no page is currently being used.
        /// </summary>
        public int CurrentPageIndex { get; private set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases a lock.
        /// </summary>
        public override void Clear()
        {
            CurrentPageIndex = -1;
        }


        /// <summary>
        /// Attempts to get a sub page.
        /// </summary>
        /// <param name="position">The absolute position in the stream to get the page for.</param>
        /// <param name="location">A pointer for the page.</param>
        /// <returns><c>false</c> if the page does not exists and needs to be added.</returns>
        public bool TryGetSubPage(long position, out nint location)
        {
            lock (m_parent.m_syncRoot)
            {
                if (m_parent.m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (position < 0)
                    throw new ArgumentOutOfRangeException(nameof(position), "Cannot be negative");

                if (position > m_parent.m_maxValidPosition)
                    throw new ArgumentOutOfRangeException(nameof(position), "Position index can no longer be specified as an Int32");

                if ((position & m_parent.m_memoryPageSizeMask) != 0)
                    throw new ArgumentOutOfRangeException(nameof(position), "must lie on a page boundary");

                int positionIndex = (int)(position >> m_parent.m_memoryPageSizeShiftBits);

                if (m_parent.m_pageList.TryGetPageIndex(positionIndex, out int pageIndex))
                {
                    CurrentPageIndex = pageIndex;
                    location = m_parent.m_pageList.GetPointerToPage(pageIndex, 1);

                    return true;
                }

                location = default;

                return false;
            }
        }

        /// <summary>
        /// Gets or adds a page at the specified position within the memory pool, based on the provided parameters.
        /// </summary>
        /// <param name="position">The position within the memory pool to retrieve or add a page.</param>
        /// <param name="startOfMemoryPoolPage">The starting address of the memory pool page.</param>
        /// <param name="memoryPoolIndex">The index of the memory pool.</param>
        /// <param name="wasPageAdded">A boolean indicating whether a new page was added during the operation.</param>
        /// <returns>The address of the page within the memory pool.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the parent object has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the position is negative, exceeds the maximum valid position, or does not lie on a page boundary.</exception>
        public nint GetOrAddPage(long position, nint startOfMemoryPoolPage, int memoryPoolIndex, out bool wasPageAdded)
        {
            lock (m_parent.m_syncRoot)
            {
                if (m_parent.m_disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (position < 0)
                    throw new ArgumentOutOfRangeException(nameof(position), "Cannot be negative");

                if (position > m_parent.m_maxValidPosition)
                    throw new ArgumentOutOfRangeException(nameof(position), "Position index can no longer be specified as an Int32");

                if ((position & m_parent.m_memoryPageSizeMask) != 0)
                    throw new ArgumentOutOfRangeException(nameof(position), "must lie on a page boundary");

                int positionIndex = (int)(position >> m_parent.m_memoryPageSizeShiftBits);

                if (m_parent.m_pageList.TryGetPageIndex(positionIndex, out int pageIndex))
                {
                    CurrentPageIndex = pageIndex;
                    nint location = m_parent.m_pageList.GetPointerToPage(pageIndex, 1);
                    wasPageAdded = false;

                    return location;
                }

                CurrentPageIndex = m_parent.m_pageList.AddNewPage(positionIndex, startOfMemoryPoolPage, memoryPoolIndex);
                wasPageAdded = true;

                return startOfMemoryPoolPage;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object  is equal to the current object; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return m_hashCode;
        }

        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                CurrentPageIndex = -1; // Reset current page index.
                m_disposed = true; // Mark object as disposed.

                if (disposing)
                    lock (m_parent.m_syncRoot) // Lock the parent's synchronization root for thread safety.
                    {
                        if (!m_parent.m_disposed)
                            m_parent.m_arrayIndexLocks.Remove(this); // If parent is not already disposed, remove this instance from the parent's list of locks.
                    }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(PageLock? other)
        {
            return other is null;
        }

        #endregion
    }

    #endregion
}