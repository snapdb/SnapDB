//******************************************************************************************************
//  SequentialReaderStream'2.cs - Gbtc
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
//  02/14/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Text;
using Gemstone.Diagnostics;
using SnapDB.Snap.Filters;
using SnapDB.Snap.Tree;
using SnapDB.Threading;

namespace SnapDB.Snap.Services.Reader;

internal class SequentialReaderStream<TKey, TValue> : TreeStream<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    public event Action<SequentialReaderStream<TKey, TValue>> Disposed;

    private BufferedArchiveStream<TKey, TValue> m_firstTable;
    private SortedTreeScannerBase<TKey, TValue> m_firstTableScanner;

    private readonly MatchFilterBase<TKey, TValue>? m_keyMatchFilter;

    private readonly bool m_keyMatchIsUniverse;

    private readonly SeekFilterBase<TKey>? m_keySeekFilter;
    private readonly TKey m_nextArchiveStreamLowerBounds = new();
    private readonly bool m_ownsWorkerThreadSynchronization;
    private long m_pointCount;
    private readonly TKey m_readWhileUpperBounds = new();

    private ArchiveListSnapshot<TKey, TValue> m_snapshot;
    private readonly CustomSortHelper<BufferedArchiveStream<TKey, TValue>> m_sortedArchiveStreams;
    private List<BufferedArchiveStream<TKey, TValue>> m_tablesOrigList;
    private volatile bool m_timedOut;

    private TimeoutOperation m_timeout;
    private WorkerThreadSynchronization m_workerThreadSynchronization;

    #endregion

    #region [ Constructors ]

    public SequentialReaderStream(ArchiveList<TKey, TValue> archiveList, SortedTreeEngineReaderOptions? readerOptions = null, SeekFilterBase<TKey>? keySeekFilter = null, MatchFilterBase<TKey, TValue>? keyMatchFilter = null, WorkerThreadSynchronization? workerThreadSynchronization = null)
    {
        readerOptions ??= SortedTreeEngineReaderOptions.Default;
        keySeekFilter ??= new SeekFilterUniverse<TKey>();
        keyMatchFilter ??= new MatchFilterUniverse<TKey, TValue>();

        if (workerThreadSynchronization is null)
        {
            m_ownsWorkerThreadSynchronization = true;
            workerThreadSynchronization = new WorkerThreadSynchronization();
        }

        m_workerThreadSynchronization = workerThreadSynchronization;
        m_pointCount = 0;
        m_keySeekFilter = keySeekFilter;
        m_keyMatchFilter = keyMatchFilter;
        m_keyMatchIsUniverse = m_keyMatchFilter as MatchFilterUniverse<TKey, TValue> is not null;

        if (readerOptions.Timeout.Ticks > 0)
        {
            m_timeout = new TimeoutOperation();
            m_timeout.RegisterTimeout(readerOptions.Timeout, () => m_timedOut = true);
        }

        m_snapshot = archiveList.CreateNewClientResources();
        m_snapshot.UpdateSnapshot();
        m_tablesOrigList = new List<BufferedArchiveStream<TKey, TValue>>();

        for (int x = 0; x < m_snapshot.Tables.Count(); x++)
        {
            ArchiveTableSummary<TKey, TValue>? table = m_snapshot.Tables[x];

            if (table is null)
                continue;
            
            if (table.Contains(keySeekFilter.StartOfRange, keySeekFilter.EndOfRange))
                try
                {
                    m_tablesOrigList.Add(new BufferedArchiveStream<TKey, TValue>(x, table));
                }
                catch (Exception ex)
                {
                    //ToDo: Make sure firstkey.tostring doesn't ever throw an exception.
                    StringBuilder sb = new();
                    sb.AppendLine($"Archive ID {table.FileId}");
                    sb.AppendLine($"First Key {table.FirstKey.ToString()}");
                    sb.AppendLine($"Last Key {table.LastKey.ToString()}");
                    sb.AppendLine($"File Size {table.SortedTreeTable.BaseFile.ArchiveSize}");
                    sb.AppendLine($"File Name {table.SortedTreeTable.BaseFile.FilePath}");
                    s_log.Publish(MessageLevel.Error, "Error while reading file", sb.ToString(), null, ex);
                }
            else
                m_snapshot.Tables[x] = null;
        }

        m_sortedArchiveStreams = new CustomSortHelper<BufferedArchiveStream<TKey, TValue>>(m_tablesOrigList, IsLessThan);

        m_keySeekFilter.Reset();
        if (m_keySeekFilter.NextWindow())
            SeekToKey(m_keySeekFilter.StartOfFrame);
        else
            Dispose();
    }

    ~SequentialReaderStream()
    {
        Dispose(false);
    }

    #endregion

    #region [ Properties ]

    public override bool IsAlwaysSequential => true;

    public override bool NeverContainsDuplicates => true;

    #endregion

    #region [ Methods ]

    protected override void Dispose(bool disposing)
    {
        try
        {
            Disposed?.Invoke(this);
        }
        catch (Exception)
        {
        }

        Interlocked.Add(ref Stats.PointsReturned, m_pointCount);
        m_pointCount = 0;

        if (m_timeout is not null)
        {
            m_timeout.Cancel();
            m_timeout = null;
        }

        if (m_tablesOrigList is not null)
        {
            m_tablesOrigList.ForEach(x => x.Dispose());
            m_tablesOrigList = null;
            Array.Clear(m_snapshot.Tables, 0, m_snapshot.Tables.Length);
        }

        m_timedOut = true;

        if (m_snapshot is not null)
        {
            m_snapshot.Dispose();
            m_snapshot = null;
        }

        if (m_workerThreadSynchronization is not null && m_ownsWorkerThreadSynchronization)
        {
            m_workerThreadSynchronization.Dispose();
            m_workerThreadSynchronization = null;
        }
    }

    /// <summary>
    /// Provides a thread safe way to cancel a reader.
    /// </summary>
    public void CancelReader()
    {
        m_timedOut = true;
        m_workerThreadSynchronization.RequestCallback(Dispose);
    }

    protected override bool ReadNext(TKey key, TValue value)
    {
        if (!m_timedOut && m_firstTableScanner is not null)
        {
            if (m_keyMatchIsUniverse)
            {
                if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds))
                {
                    m_pointCount++;
                    if (m_pointCount > 10000)
                    {
                        m_workerThreadSynchronization.PulseSafeToCallback();
                        Interlocked.Add(ref Stats.PointsReturned, m_pointCount);
                        m_pointCount = 0;
                    }

                    return true;
                }
            }
            else
            {
                if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds, m_keyMatchFilter))
                {
                    m_pointCount++;
                    if (m_pointCount > 10000)
                    {
                        m_workerThreadSynchronization.PulseSafeToCallback();
                        Interlocked.Add(ref Stats.PointsReturned, m_pointCount);
                        m_pointCount = 0;
                    }

                    return true;
                }
            }
        }

        return ReadCatchAll(key, value);
    }

    private bool ReadCatchAll(TKey key, TValue value)
    {
        m_workerThreadSynchronization.PulseSafeToCallback();
        if (m_pointCount > 10000)
        {
            Interlocked.Add(ref Stats.PointsReturned, m_pointCount);
            m_pointCount = 0;
        }

    TryAgain:
        if (!m_timedOut)
        {
            if (m_keyMatchIsUniverse)
            {
                if (m_firstTableScanner is null)
                    return false;

                if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds) || ReadWhileFollowupActions(key, value, null))
                {
                    m_pointCount++;
                    return true;
                }

                goto TryAgain;
            }

            if (m_firstTableScanner is null)
                return false;

            if (m_firstTableScanner.ReadWhile(key, value, m_readWhileUpperBounds, m_keyMatchFilter) || ReadWhileFollowupActions(key, value, m_keyMatchFilter))
            {
                m_pointCount++;
                return true;
            }

            goto TryAgain;
        }

        Dispose();
        return false;
    }

    private bool ReadWhileFollowupActions(TKey key, TValue value, MatchFilterBase<TKey, TValue>? filter)
    {
        //There are certain followup requirements when a ReadWhile method returns false.
        //Condition 1:
        //  The end of the node has been reached. 
        //Response: 
        //  It returned false to allow for additional checks such as timeouts to occur.
        //  Do Nothing.
        //
        //Condition 2:
        //  The archive stream may no longer be in order and needs to be checked
        //Response:
        //  Resort the archive stream
        //
        //Condition 3:
        //  The end of the frame has been reached
        //Response:
        //  Advance to the next frame
        //  Also test the edge case where the current point might be equal to the end of the frame
        //      since this is an inclusive filter and ReadWhile is exclusive.
        //      If it's part of the frame, return true after Advancing the frame and the point.
        //

        //Update the cached values for the table so proper analysis can be done.
        m_firstTable.UpdateCachedValue();

        //Check Condition 1
        if (m_firstTable.CacheIsValid && m_firstTable.CacheKey.IsLessThan(m_readWhileUpperBounds))
            return false;

        //Since condition 2 and 3 can occur at the same time, verifying the sort of the Archive Stream is a good thing to do.
        VerifyArchiveStreamSortingOrder();

        if (Eos)
            return false;

        //Check if Condition 3's exception occured.
        if (m_firstTable.CacheKey.IsEqualTo(m_keySeekFilter.EndOfFrame))
        {
            //This is the exception clause. I will advance the frame, but will still need to return the current point.
            m_firstTable.Scanner.Read(key, value);
            AdvanceSeekableFilter(true, key);
            SetReadWhileUpperBoundsValue();

            return filter is null || filter.Contains(key, value);
        }

        //Check if condition 3 occured
        if (m_firstTable.CacheKey.IsGreaterThan(m_keySeekFilter.EndOfFrame))
        {
            AdvanceSeekableFilter(true, m_firstTable.CacheKey);
            SetReadWhileUpperBoundsValue();
        }

        return false;
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


    /// <summary>
    /// Does a seek operation on the current stream when there is a seek filter on the reader.
    /// </summary>
    /// <returns>
    /// True if the provided key is still valid within the next best fitting frame.
    /// </returns>
    private bool AdvanceSeekableFilter(bool isValid, TKey key)
    {
    TryAgain:
        if (m_keySeekFilter is not null && m_keySeekFilter.NextWindow())
        {
            //If the current point is a valid point. 
            //Check to see if the seek operation can be avoided.
            //or if the next available point does not exist in this window.
            if (isValid)
            {
                //If the current point is within this window
                if (key.IsGreaterThanOrEqualTo(m_keySeekFilter.StartOfFrame) && key.IsLessThanOrEqualTo(m_keySeekFilter.EndOfFrame))
                    return true;

                //If the current point is after this window, seek to the next window.
                if (key.IsGreaterThan(m_keySeekFilter.EndOfFrame))
                    goto TryAgain;
            }

            //If the current point is not valid, or is before m_startKey
            //Advance the scanner to the next window.
            SeekAllArchiveStreamsForward(m_keySeekFilter.StartOfFrame);
            return false;
        }

        Dispose();
        m_firstTableScanner = null;
        m_firstTable = null;
        return false;
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
    /// <param name="item1">The first stream to compare.</param>
    /// <param name="item2">The second stream to compare.</param>
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

        //Remove any duplicates
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
    /// Seeks the streams only in the forward direction.
    /// This means that if the current position in any stream is invalid or past this point,
    /// the stream will not seek backwards.
    /// </summary>
    /// <param name="key">the key to seek to</param>
    private void SeekAllArchiveStreamsForward(TKey key)
    {
        foreach (BufferedArchiveStream<TKey, TValue> table in m_sortedArchiveStreams.Items)
        {
            if (table.CacheIsValid && table.CacheKey.IsLessThan(key))
                table.SeekToKeyAndUpdateCacheValue(key);
        }

        //Resorts the entire list.
        m_sortedArchiveStreams.Sort();

        //Remove any duplicates
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
    /// Assums that the archiveStream's cached value is current.
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

        //Resorts the list in reverse order.
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

        // If there is a key seek filter. adjust this bounds if necessary
        if (m_keySeekFilter is null)
            return;
        
        if (m_keySeekFilter.EndOfFrame.IsLessThan(m_readWhileUpperBounds))
            m_keySeekFilter.EndOfFrame.CopyTo(m_readWhileUpperBounds);
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(SequentialReaderStream<TKey, TValue>), MessageClass.Framework);

    #endregion
}