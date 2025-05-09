﻿//******************************************************************************************************
//  UnionReader'2.cs - Gbtc
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
//  02/16/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Tree;

namespace SnapDB.Snap.Services.Reader;

internal class UnionReader<TKey, TValue> : TreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private BufferedArchiveStream<TKey, TValue>? m_firstTable;
    private SortedTreeScannerBase<TKey, TValue>? m_firstTableScanner;

    private readonly TKey m_nextArchiveStreamLowerBounds = new();
    private readonly TKey m_readWhileUpperBounds = new();
    private readonly CustomSortHelper<BufferedArchiveStream<TKey, TValue>> m_sortedArchiveStreams;

    private List<BufferedArchiveStream<TKey, TValue>> m_tablesOrigList;

    #endregion

    #region [ Constructors ]

    public UnionReader(List<ArchiveTableSummary<TKey, TValue>> tables)
    {
        m_tablesOrigList = [];

        foreach (ArchiveTableSummary<TKey, TValue> table in tables)
            m_tablesOrigList.Add(new BufferedArchiveStream<TKey, TValue>(0, table));

        m_sortedArchiveStreams = new CustomSortHelper<BufferedArchiveStream<TKey, TValue>>(m_tablesOrigList, IsLessThan);

        m_readWhileUpperBounds.SetMin();
        SeekToKey(m_readWhileUpperBounds);
    }

    #endregion

    #region [ Properties ]

    public override bool IsAlwaysSequential => true;

    public override bool NeverContainsDuplicates => true;

    #endregion

    #region [ Methods ]

    protected override void Dispose(bool disposing)
    {
        if (m_tablesOrigList is not null)
        {
            m_tablesOrigList.ForEach(x => x.Dispose());
            m_tablesOrigList = null!;
        }

        base.Dispose(disposing);
    }

    protected override bool ReadNext(TKey key, TValue value)
    {
        if (m_firstTableScanner is null)
            return ReadCatchAll(key, value);
        
        if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds))
            return true;

        return ReadCatchAll(key, value);
    }

    private bool ReadCatchAll(TKey key, TValue value)
    {
    TryAgain:
        if (m_firstTableScanner is null)
            return false;
        if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds))
            return true;
        ReadWhileFollowupActions();
        goto TryAgain;
    }

    private void ReadWhileFollowupActions()
    {
        // There are certain followup requirements when a ReadWhile method returns false.
        // Condition 1:
        //   The end of the node has been reached. 
        // Response: 
        //   It returned false to allow for additional checks such as timeouts to occur.
        //   Do Nothing.
        //
        // Condition 2:
        //   The archive stream may no longer be in order and needs to be checked
        // Response:
        //   Resort the archive stream
        //
        // Condition 3:
        //   The end of the frame has been reached
        // Response:
        //   Advance to the next frame
        //   Also test the edge case where the current point might be equal to the end of the frame
        //       since this is an inclusive filter and ReadWhile is exclusive.
        //       If it's part of the frame, return true after Advancing the frame and the point.
        //

        // Update the cached values for the table so proper analysis can be done.
        m_firstTable!.UpdateCachedValue();

        //Check Condition 1
        if (m_firstTable.CacheIsValid && m_firstTable.CacheKey.IsLessThan(m_readWhileUpperBounds))
            return;

        //Since condition 2 and 3 can occur at the same time, verifying the sort of the Archive Stream is a good thing to do.
        VerifyArchiveStreamSortingOrder();
    }


    //-------------------------------------------------------------

    /// <summary>
    /// Will verify that the stream is in the proper order and remove any duplicates that were found.
    /// May be called after every single read, but better to be called
    /// when a ReadWhile function returns false.
    /// </summary>
    private void VerifyArchiveStreamSortingOrder()
    {
        if (Eos)
            return;

        m_sortedArchiveStreams[0].UpdateCachedValue();

        if (m_sortedArchiveStreams.Items.Length > 1)
        {
            //If list is no longer in order
            int compare = CompareStreams(m_sortedArchiveStreams[0], m_sortedArchiveStreams[1]);
            if (compare == 0 && m_sortedArchiveStreams[0].CacheIsValid)
            {
                //If a duplicate entry is found, advance the position of the duplicate entry
                RemoveDuplicatesFromList();
                SetReadWhileUpperBoundsValue();
            }

            if (compare > 0)
            {
                m_sortedArchiveStreams.SortAssumingIncreased(0);
                m_firstTable = m_sortedArchiveStreams[0];
                m_firstTableScanner = m_firstTable.Scanner;
                SetReadWhileUpperBoundsValue();
            }

            if (compare == 0 && !m_sortedArchiveStreams[0].CacheIsValid)
            {
                Dispose();
                m_firstTable = null;
                m_firstTableScanner = null;
            }
        }
        else
        {
            if (!m_sortedArchiveStreams[0].CacheIsValid)
            {
                Dispose();
                m_firstTable = null;
                m_firstTableScanner = null;
            }
        }
    }

    private bool IsLessThan(BufferedArchiveStream<TKey, TValue> item1, BufferedArchiveStream<TKey, TValue> item2)
    {
        if (!item1.CacheIsValid && !item2.CacheIsValid)
            return false;
        
        if (!item1.CacheIsValid)
            return false;
        
        if (!item2.CacheIsValid)
            return true;
        
        return item1.CacheKey.IsLessThan(item2.CacheKey); // item1.CurrentKey.CompareTo(item2.CurrentKey);
    }

    /// <summary>
    /// Compares two Archive Streams together for proper sorting.
    /// </summary>
    /// <returns>
    /// A value indicating the relative order of the streams based on their cache keys:
    /// - Less than 0 if <paramref name="item1"/> is less than <paramref name="item2"/>.
    /// - Greater than 0 if <paramref name="item1"/> is greater than <paramref name="item2"/>.
    /// - 0 if <paramref name="item1"/> and <paramref name="item2"/> are equal or their caches are invalid.
    /// </returns>
    /// <remarks>
    /// The <see cref="CompareStreams"/> method is used to compare two <see cref="BufferedArchiveStream{TKey,TValue}"/>
    /// instances based on their cache keys. If both streams have invalid caches, they are considered equal.
    /// </remarks>
    private int CompareStreams(BufferedArchiveStream<TKey, TValue> item1, BufferedArchiveStream<TKey, TValue> item2)
    {
        if (!item1.CacheIsValid && !item2.CacheIsValid)
            return 0;

        if (!item1.CacheIsValid)
            return 1;

        if (!item2.CacheIsValid)
            return -1;

        return item1.CacheKey.CompareTo(item2.CacheKey); // item1.CurrentKey.CompareTo(item2.CurrentKey);
    }

    /// <summary>
    /// Does an unconditional seek operation to the provided key.
    /// </summary>
    /// <param name="key"></param>
    private void SeekToKey(TKey key)
    {
        foreach (BufferedArchiveStream<TKey, TValue> table in m_sortedArchiveStreams.Items)
            table.SeekToKeyAndUpdateCacheValue(key);

        m_sortedArchiveStreams.Sort();

        // Remove any duplicates
        RemoveDuplicatesIfExists();

        if (m_sortedArchiveStreams.Items.Length > 0)
        {
            m_firstTable = m_sortedArchiveStreams[0];
            m_firstTableScanner = m_firstTable.Scanner;
        }
        else
        {
            m_firstTable = null;
            m_firstTableScanner = null;
        }

        SetReadWhileUpperBoundsValue();
    }

    /// <summary>
    /// Checks the first 2 Archive Streams for a duplicate entry. If one exists, then removes the duplicate and resorts the list.
    /// </summary>
    private void RemoveDuplicatesIfExists()
    {
        if (m_sortedArchiveStreams.Items.Length <= 1)
            return;

        // If a duplicate entry is found, advance the position of the duplicate entry
        if (CompareStreams(m_sortedArchiveStreams[0], m_sortedArchiveStreams[1]) == 0 && m_sortedArchiveStreams[0].CacheIsValid)
            RemoveDuplicatesFromList();
    }

    /// <summary>
    /// Call this function when the same point exists in multiple archive files. It will
    /// read past the duplicate point in all other archive files and then resort the tables.
    /// Assumes that the archiveStream's cached value is current.
    /// </summary>
    private void RemoveDuplicatesFromList()
    {
        int lastDuplicateIndex = -1;

        for (int index = 1; index < m_sortedArchiveStreams.Items.Length; index++)
        {
            if (CompareStreams(m_sortedArchiveStreams[0], m_sortedArchiveStreams[index]) == 0)
            {
                m_sortedArchiveStreams[index].SkipToNextKeyAndUpdateCachedValue();
                lastDuplicateIndex = index;
            }
            else
            {
                break;
            }
        }

        // Resorts the list in reverse order.
        for (int j = lastDuplicateIndex; j > 0; j--)
            m_sortedArchiveStreams.SortAssumingIncreased(j);

        SetReadWhileUpperBoundsValue();
    }

    /// <summary>
    /// Sets the read while upper bounds value.
    /// Which is the lesser of
    /// The first point in the adjacent table or
    /// The end of the current seek window.
    /// </summary>
    private void SetReadWhileUpperBoundsValue()
    {
        if (m_sortedArchiveStreams.Items.Length > 1 && m_sortedArchiveStreams[1].CacheIsValid)
            m_sortedArchiveStreams[1].CacheKey.CopyTo(m_nextArchiveStreamLowerBounds);
        else
            m_nextArchiveStreamLowerBounds.SetMax();

        m_nextArchiveStreamLowerBounds.CopyTo(m_readWhileUpperBounds);
    }

    #endregion
}