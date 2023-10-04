//******************************************************************************************************
//  CustomSortHelper'1.cs - Gbtc
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
//  10/26/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap;

/// <summary>
/// A helper class for custom sorting of items.
/// </summary>
/// <typeparam name="T">The type of items to be sorted.</typeparam>
public class CustomSortHelper<T>
{
    #region [ Members ]

    /// <summary>
    /// All of the items in this list.
    /// </summary>
    public T[] Items;

    private readonly Func<T, T, bool> m_isLessThan;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new custom sort helper and presorts the list.
    /// </summary>
    /// <param name="items">The collection of items to be sorted.</param>
    /// <param name="isLessThan">A function that determines if one item is less than another.</param>
    public CustomSortHelper(IEnumerable<T> items, Func<T, T, bool> isLessThan)
    {
        Items = items.ToArray();
        m_isLessThan = isLessThan;
        Sort();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Indexer to get or set the specified item in the list.
    /// </summary>
    /// <param name="index">The index of the item to access.</param>
    /// <returns>The item at the specified index.</returns>
    public T this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Resorts the entire list using an insertion sort routine.
    /// </summary>
    public void Sort()
    {
        // A insertion sort routine.

        // Skip first item in list since it will always be sorted correctly
        for (int itemToInsertIndex = 1; itemToInsertIndex < Items.Length; itemToInsertIndex++)
        {
            T itemToInsert = Items[itemToInsertIndex];

            int currentIndex = itemToInsertIndex - 1;
            //While the current item is greater than itemToInsert, shift the value
            while (currentIndex >= 0 && m_isLessThan(itemToInsert, Items[currentIndex]))
            {
                Items[currentIndex + 1] = Items[currentIndex];
                currentIndex--;
            }

            Items[currentIndex + 1] = itemToInsert;
        }
    }

    /// <summary>
    /// Resorts only the item at the specified index assuming:
    /// 1) all other items are properly sorted
    /// 2) this item's value increased.
    /// </summary>
    /// <param name="index">The index of the item to resort.</param>
    public void SortAssumingIncreased(int index)
    {
        T itemToMove = Items[index];
        int currentIndex = index + 1;
        while (currentIndex < Items.Length && m_isLessThan(Items[currentIndex], itemToMove))
        {
            Items[currentIndex - 1] = Items[currentIndex];
            currentIndex++;
        }

        Items[currentIndex - 1] = itemToMove;
    }

    #endregion
}