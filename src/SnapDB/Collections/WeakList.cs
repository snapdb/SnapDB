//******************************************************************************************************
//  WeakList.cs - Gbtc
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
//  04/11/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Collections;

/// <summary>
/// Creates a list of items that will be weak referenced.
/// This list is thread-safe and allows enumeration while adding and removing from the list.
/// </summary>
/// <typeparam name="T">List type.</typeparam>
public class WeakList<T> : IEnumerable<T>
    where T : class
{
    /// <summary>
    /// Contains a snapshot of the data so read operations can be non-blocking.
    /// </summary>
    private class Snapshot
    {
        // This is a special case list where all items in the list are weak referenced. Only include 
        // instances that are strong referenced somewhere. 
        // For example, delegates will not work in this list unless it is strong referenced in the instance.
        // This is because a delegate is a small wrapper of (Object,Method) and is usually recreated on the fly
        // rather than stored. Therefore if the only reference to the delegate is passed to this list, it will be
        // collected at the next GC cycle.
        public WeakReference?[] Items;
        public int Size;
           
        public Snapshot(int capacity)
        {
            Items = new WeakReference[capacity];
            Size = 0;
        }

        /// <summary>
        /// Grows the snapshot, doubling the size of the number of entries.
        /// </summary>
        public Snapshot Grow()
        {
            int itemCount = 0;

            // Counts the number of entries that are valid.
            for (int x = 0; x < Items.Length; x++)
            {
                WeakReference? reference = Items[x];

                if (reference is null)
                    continue;

                if (reference.Target is T)
                    itemCount++;
                
                else
                    Items[x] = null;
            }

            // Copies the snapshot.
            int capacity = Math.Max(itemCount * 2, 8);

            Snapshot clone = new(capacity)
            {
                Items = new WeakReference[capacity],
                Size = 0
            };

            foreach (WeakReference? reference in Items)
            {
                if (reference is null)
                    continue;

                // Since checking the weak reference is slow, just assume that 
                // it still has reference. It won't hurt anything.
                if (!clone.TryAdd(reference))
                    throw new Exception("List is full");
            }

            return clone;
        }

        /// <summary>
        /// Removes all occurrences of <see cref="item"/> from the list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(T? item)
        {
            if (item is null)
                return;

            int count = Size;
            EqualityComparer<T> compare = EqualityComparer<T>.Default;

            for (int x = 0; x < count; x++)
            {
                WeakReference? reference = Items[x];

                if (reference is null)
                    continue;

                if (reference.Target is T itemCompare)
                {
                    if (compare.Equals(itemCompare, item))
                        Items[x] = null;
                }
                else
                {
                    Items[x] = null;
                }
            }
        }

        /// <summary>
        /// Attempts to add <see cref="item"/> to the list. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>true</c> if added, <c>false</c> otherwise.</returns>
        public bool TryAdd(T item)
        {
            return TryAdd(new WeakReference(item));
        }

        private bool TryAdd(WeakReference item)
        {
            if (Size >= Items.Length)
                return false;

            Items[Size] = item;
            Size++;

            return true;
        }
    }

    /// <summary>
    /// An <see cref="IEnumerator{T}"/> for <see cref="WeakList{T}"/>
    /// </summary>
    public struct Enumerator : IEnumerator<T?>
    {
        private readonly WeakReference?[] m_items;
        private readonly int m_lastItem;
        private int m_currentIndex;

        /// <summary>
        /// Creates a <see cref="Enumerator"/>.
        /// </summary>
        /// <param name="items">The weak referenced items.</param>
        /// <param name="count">The number of valid items in the list.</param>
        public Enumerator(WeakReference?[] items, int count)
        {
            m_items = items;
            m_lastItem = count - 1;
            m_currentIndex = -1;
            Current = null;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T? Current { get; private set; }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        readonly object? IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next valid item in the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next item;
        /// <c>false</c> if the end of the collection has been reached.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        /// <filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            while (m_currentIndex < m_lastItem)
            {
                m_currentIndex++; // Move to the next index in the collection.
                WeakReference? reference = m_items[m_currentIndex];

                if (reference is null)
                    continue; // Skip null references.

                if (reference.Target is T item)
                {
                    // If the weak reference can be resolved to an item of type T, set the current item and return true.
                    Current = item;
                    
                    return true;
                }

                // If the weak reference cannot be resolved, mark it as null to allow it to be garbage collected.
                m_items[m_currentIndex] = null;
            }

            // No more valid items in the collection, so return false to indicate the end of enumeration.
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            Current = null;
            m_currentIndex = -1;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Current = null;
    }

    private readonly object m_syncRoot;
    private Snapshot m_data;

    /// <summary>
    /// Creates a <see cref="WeakList{T}"/>
    /// </summary>
    public WeakList()
    {
        m_syncRoot = new object();
        m_data = new Snapshot(8);
    }

    /// <summary>
    /// Clears all of the times in the list. Method is thread safe.
    /// </summary>
    public void Clear()
    {
        lock (m_syncRoot)
        {
            m_data.Size = 0;
            Array.Clear(m_data.Items, 0, m_data.Items.Length);
        }
    }

    /// <summary>
    /// Adds the <see cref="item"/> to the list. Method is thread safe.
    /// </summary>
    /// <param name="item">Item to add.</param>
    public void Add(T item)
    {
        lock (m_syncRoot)
        {
            if (m_data.TryAdd(item))
                return;

            m_data = m_data.Grow();

            if (!m_data.TryAdd(item))
                throw new Exception("Could not grow list");
        }
    }

    /// <summary>
    /// Removes all occurrences of the <see cref="item"/> from the list. Method is thread-safe.
    /// </summary>
    /// <param name="item">Item to remove.</param>
    public void Remove(T item)
    {
        lock (m_syncRoot)
            m_data.Remove(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
    /// </returns>
    public Enumerator GetEnumerator()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        Snapshot snapshot = m_data;
        return new Enumerator(snapshot.Items, snapshot.Size);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
    /// </returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
