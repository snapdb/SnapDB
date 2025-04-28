//******************************************************************************************************
//  ThreadSafeList_Enumerator.cs - Gbtc
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
//  01/26/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Threading;

public partial class ThreadSafeList<T>
{
    #region [ Members ]

    /// <summary>
    /// The Enumerator for a <see cref="ThreadSafeList{T}"/>.
    /// </summary>
    private class Enumerator : IEnumerator<T>
    {
        #region [ Members ]

        private Iterator m_iterator;
        private T m_nextItem;
        private bool m_nextItemExists;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the Enumerator class with the provided iterator.
        /// </summary>
        /// <param name="iterator">The iterator to associate with the enumerator.</param>
        public Enumerator(Iterator iterator)
        {
            m_iterator = iterator;
            m_nextItemExists = false;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current
        {
            get
            {
                if (!m_nextItemExists)
                    throw new InvalidOperationException("Past the end of the array, or never called MoveNext()");

                return m_nextItem;
            }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current => Current;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_nextItemExists)
                    m_iterator.UnsafeUnregisterItem();

                m_disposed = true;
                m_nextItemExists = false;
                m_nextItem = default;
                m_iterator = null;
            }
        }


        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            if (m_nextItemExists)
                m_iterator.UnsafeUnregisterItem();

            m_nextItemExists = m_iterator.UnsafeTryGetNextItem(out m_nextItem);
            return m_nextItemExists;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public void Reset()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            if (m_nextItemExists)
                m_iterator.UnsafeUnregisterItem();

            m_nextItem = default;
            m_nextItemExists = false;
            m_iterator.Reset();
        }

        #endregion
    }

    #endregion
}