//******************************************************************************************************
//  ThreadSafeList.cs - Gbtc
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
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Threading;

/// <summary>
/// This list allows for iterating through the list
/// while object can be removed from the list. Once an object has been
/// removed, is garenteed not to be called again by a seperate thread.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public partial class ThreadSafeList<T> : IEnumerable<T>
{
    #region [ Members ]

    private class Wrapper
    {
        #region [ Members ]

        public readonly T Item;
        public int ReferencedCount;

        #endregion

        #region [ Constructors ]

        public Wrapper(T item)
        {
            Item = item;
        }

        #endregion
    }

    private readonly SortedList<long, Wrapper> m_list;
    private long m_sequenceNumber;
    private readonly Lock m_syncRoot;
    private long m_version;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="ThreadSafeList{T}"/>.
    /// </summary>
    public ThreadSafeList()
    {
        m_syncRoot = new Lock();
        m_list = new SortedList<long, Wrapper>();
        m_sequenceNumber = 0;
        m_version = 0;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Adds the supplied item to the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        lock (m_syncRoot)
        {
            m_list.Add(m_sequenceNumber, new Wrapper(item));
            m_sequenceNumber++;
            m_version++;
        }
    }

    /// <summary>
    /// Removes an item from the list.
    /// This method will block until the item has successfully been removed
    /// and will no longer show up in the iterator.
    /// DO NOT call this function from within a ForEach loop as it will block indefinately
    /// since the for each loop reads all items.
    /// </summary>
    /// <param name="item">The item to remove from the list.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAndWait(T item)
    {
        SpinWait wait = new();
        Wrapper itemToRemove = null;
        lock (m_syncRoot)
        {
            for (int x = 0; x < m_list.Count; x++)
            {
                if (m_list.Values[x].Item.Equals(item))
                {
                    itemToRemove = m_list.Values[x];
                    m_list.RemoveAt(x);
                    m_version++;
                    break;
                }
            }
        }

        if (itemToRemove is null)
            return false;

        while (Interlocked.CompareExchange(ref itemToRemove.ReferencedCount, -1, 0) is not 0)
            wait.SpinOnce();

        return true;
    }

    /// <summary>
    /// Removes an item from the list.
    /// </summary>
    /// <param name="item">The item to remove from the list.</param>
    /// <returns><c>true</c> if the item is successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(T item)
    {
        Wrapper itemToRemove = null;
        lock (m_syncRoot)
        {
            for (int x = 0; x < m_list.Count; x++)
            {
                if (m_list.Values[x].Item.Equals(item))
                {
                    itemToRemove = m_list.Values[x];
                    m_list.RemoveAt(x);
                    m_version++;
                    break;
                }
            }
        }

        return itemToRemove is not null;
    }

    /// <summary>
    /// Removes the specified item if the lambda expression is true.
    /// </summary>
    /// <param name="condition">A condition delegate used to determine which items to remove.</param>
    public void RemoveIf(Func<T, bool> condition)
    {
        lock (m_syncRoot)
        {
            for (int x = 0; x < m_list.Count; x++)
            {
                if (condition(m_list.Values[x].Item))
                {
                    m_list.RemoveAt(x);
                    m_version++;
                }
            }
        }
    }

    /// <summary>
    /// Calls a foreach iterator on the supplied action.
    /// </summary>
    /// <param name="action">The action to perform on each element of the list.</param>
    public void ForEach(Action<T> action)
    {
        foreach (T item in this)
            action(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator"/> that can be used to
    /// iterate through the collection.
    /// </returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(new Iterator(this));
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}