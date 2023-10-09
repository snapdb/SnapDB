﻿//******************************************************************************************************
//  ImmutableList.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/24/2016 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Collections;

namespace SnapDB.Immutables;

/// <summary>
/// A list that can be modified until <see cref="ImmutableObjectBase{T}.IsReadOnly"/> is set to <c>true</c>. Once this occurs,
/// the list itself can no longer be modified.  Remember, this does not cause objects contained in this class to be Immutable
/// unless they implement <see cref="IImmutableObject"/>.
/// </summary>
/// <typeparam name="T">List type.</typeparam>
public class ImmutableList<T> : ImmutableObjectBase<ImmutableList<T?>>, IList<T?>
{
    #region [ Members ]

    private readonly Func<T?, T>? m_formatter;
    private readonly bool m_isISupportsReadonlyType;
    private List<T?> m_list;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new <see cref="ImmutableList{TKey}"/>.
    /// </summary>
    /// <param name="formatter">Allows items to be formatted when inserted into a list.</param>
    public ImmutableList(Func<T?, T>? formatter = null)
    {
        m_formatter = formatter;
        m_isISupportsReadonlyType = typeof(IImmutableObject).IsAssignableFrom(typeof(T));
        m_list = new List<T?>();
    }

    /// <summary>
    /// Creates a new <see cref="ImmutableList{TKey}"/>.
    /// </summary>
    public ImmutableList(int capacity, Func<T?, T>? formatter = null)
    {
        m_formatter = formatter;
        m_isISupportsReadonlyType = typeof(IImmutableObject).IsAssignableFrom(typeof(T));
        m_list = new List<T?>(capacity);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the number of elements contained in the <see cref="ICollection"/>.
    /// </summary>
    /// <returns>The number of elements contained in the <see cref="ICollection"/>.</returns>
    public int Count => m_list.Count;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <returns>The element at the specified index.</returns>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    public T? this[int index]
    {
        get => m_list[index];
        set
        {
            TestForEditable();

            if (m_formatter is null)
                m_list[index] = value;
            else
                m_list[index] = m_formatter(value);
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="List{T}"/>.
    /// </summary>
    /// <param name="collection">
    /// The collection whose elements should be added to the end of the <see cref="List{T}"/>.
    /// The collection itself cannot be <c>null</c>, but it can contain elements that are <c>null</c>.
    /// </param>
    public void AddRange(IEnumerable<T?> collection)
    {
        TestForEditable();

        if (m_formatter is not null)
        {
            foreach (T? item in collection)
                m_list.Add(m_formatter(item));

            return;
        }

        m_list.AddRange(collection);
    }

    /// <summary>
    /// Requests that member fields be set to "read-only".
    /// </summary>
    protected override void SetMembersAsReadOnly()
    {
        if (!m_isISupportsReadonlyType)
            return;

        for (int x = 0; x < m_list.Count; x++)
        {
            IImmutableObject? item = (IImmutableObject?)m_list[x];

            if (item is not null)
                item.IsReadOnly = true;
        }
    }

    /// <summary>
    /// Request that member fields be cloned and marked as editable.
    /// </summary>
    protected override void CloneMembersAsEditable()
    {
        if (m_isISupportsReadonlyType)
        {
            List<T?> oldList = m_list;
            m_list = new List<T?>(oldList.Count);

            for (int x = 0; x < oldList.Count; x++)
            {
                IImmutableObject? item = (IImmutableObject?)oldList[x];

                if (item is null)
                    m_list.Add(default);
                else
                    m_list.Add((T)item.CloneEditable());
            }
        }
        else
        {
            m_list = new List<T?>(m_list);
        }
    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return m_list.GetEnumerator();
    }

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)m_list).GetEnumerator();
    }

    /// <summary>Adds an item to the <see cref=ICollection"/>.</summary>
    /// <param name="item">The object to add to the <see cref="ICollection"/>.</param>
    /// <exception cref="NotSupportedException">The <see cref="ICollection"/> is read-only.</exception>
    public void Add(T? item)
    {
        TestForEditable();

        m_list.Add(m_formatter is null ? item : m_formatter(item));
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        TestForEditable();
        m_list.Clear();
    }

    /// <summary>
    /// Determines whether the <see cref="ICollection"/> contains a specific value.
    /// </summary>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="ICollection"/>; otherwise, <c>false</c>.</returns>
    /// <param name="item">The object to locate in the <see cref="ICollection"/>.</param>
    public bool Contains(T? item)
    {
        return m_list.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
    /// </summary>
    /// <param name="array">
    /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>.
    /// The <see cref="Array"/> must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(T?[] array, int arrayIndex)
    {
        m_list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="ICollection"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if <paramref name="item"/> was successfully removed from the <see cref="ICollection"/>;
    /// otherwise, <c>false</c>. This method also returns false if <paramref name="item"/> is not found in the original <see cref="ICollection"/>.
    /// </returns>
    /// <param name="item">The object to remove from the <see cref="ICollection"/>.</param>
    public bool Remove(T? item)
    {
        TestForEditable();
        return m_list.Remove(item);
    }

    /// <summary>
    /// Determines the index of a specific item in the <see cref="IList"/>.
    /// </summary>
    /// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
    /// <param name="item">The object to locate in the <see cref="IList"/>.</param>
    public int IndexOf(T? item)
    {
        return m_list.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item to the <see cref="IList"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="IList"/>.</param>
    public void Insert(int index, T? item)
    {
        TestForEditable();
        m_list.Insert(index, m_formatter is null ? item : m_formatter(item));
    }

    /// <summary>
    /// Removes the <see cref="IList"/> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        TestForEditable();
        m_list.RemoveAt(index);
    }

    #endregion
}