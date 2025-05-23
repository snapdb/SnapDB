﻿//******************************************************************************************************
//  ArchiveListOfT_Editor.cs - Gbtc
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
//  07/14/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Storage;

namespace SnapDB.Snap.Services;

public partial class ArchiveList<TKey, TValue>
{
    #region [ Members ]

    /// <summary>
    /// Provides a way to edit an <see cref="ArchiveList{TKey,TValue}"/> since all edits must be atomic.
    /// WARNING: Instancing this class on an <see cref="ArchiveList{TKey,TValue}"/> will lock the class
    /// until <see cref="Dispose"/> is called. Therefore, keep locks to a minimum and always
    /// use a Using block.
    /// </summary>
    private class Editor : ArchiveListEditor<TKey, TValue>
    {
        #region [ Members ]

        private readonly ArchiveList<TKey, TValue> m_list;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates an editor for the ArchiveList
        /// </summary>
        /// <param name="list">the list to create the edit lock on.</param>
        public Editor(ArchiveList<TKey, TValue> list)
        {
            m_list = list;
            m_list.m_syncRoot.Enter();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Editor"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (m_disposed)
                return;
            
            try
            {
                // This will be done regardless of whether the object is finalized or disposed.
                if (!disposing)
                    return;

                m_disposed = true;
                m_list.m_listLog.SaveLogToDisk();
                m_list.m_syncRoot.Exit();
            }
            finally
            {
                m_disposed = true; // Prevent duplicate dispose.
                base.Dispose(disposing); // Call base class Dispose().
            }
        }

        /// <summary>
        /// Renews the snapshot of the archive file. This will acquire the latest
        /// read transaction so all new snapshots will use this later version.
        /// </summary>
        /// <param name="archiveId">The unique identifier of the archive to renew the snapshot for.</param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the <see cref="ArchiveList{TKey, TValue}"/> or its associated resources are disposed.
        /// </exception>
        /// <remarks>
        /// This method renews the archive snapshot associated with the specified <paramref name="archiveId"/>
        /// in the <see cref="ArchiveList{TKey, TValue}"/> by creating a new snapshot with the same data
        /// from the existing <see cref="SortedTreeTable{TKey,TValue}"/>.
        /// </remarks>
        public override void RenewArchiveSnapshot(Guid archiveId)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            m_list.m_fileSummaries[archiveId] = new ArchiveTableSummary<TKey, TValue>(m_list.m_fileSummaries[archiveId].SortedTreeTable);
        }

        /// <summary>
        /// Adds an archive file to the list with the given state information.
        /// </summary>
        /// <param name="sortedTree">Archive table to add.</param>
        public override void Add(SortedTreeTable<TKey, TValue> sortedTree)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            ArchiveTableSummary<TKey, TValue> summary = new(sortedTree);
            m_list.m_fileSummaries.Add(sortedTree.ArchiveId, summary);
        }

        /// <summary>
        /// Returns true if the archive list contains the provided file.
        /// </summary>
        /// <param name="archiveId">The unique identifier of the archive snapshot to check for.</param>
        /// <returns>
        /// <c>true</c> if an archive snapshot with the specified <paramref name="archiveId"/> is found in the list; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method checks if the <see cref="ArchiveList{TKey, TValue}"/> contains an archive snapshot with the specified <paramref name="archiveId"/>.
        /// </remarks>
        public override bool Contains(Guid archiveId)
        {
            return m_list.m_fileSummaries.ContainsKey(archiveId);
        }

        /// <summary>
        /// Removes the <paramref name="archiveId"/> from <see cref="ArchiveList{TKey,TValue}"/> and queues it for disposal.
        /// </summary>
        /// <param name="archiveId">the archive to remove</param>
        /// <returns>True if the item was removed, False otherwise.</returns>
        /// <remarks>
        /// Also unlocks the archive file.
        /// </remarks>
        public override bool TryRemoveAndDispose(Guid archiveId)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            SortedList<Guid, ArchiveTableSummary<TKey, TValue>> partitions = m_list.m_fileSummaries;
            
            if (!partitions.TryGetValue(archiveId, out ArchiveTableSummary<TKey, TValue>? partition))
                return false;

            SortedTreeTable<TKey, TValue> tree = partition.SortedTreeTable;
            partitions.Remove(archiveId);

            m_list.AddFileToDispose(tree);
            return true;
        }

        /// <summary>
        /// Removes the supplied file from the <see cref="ArchiveList{TKey,TValue}"/> and queues it for deletion.
        /// </summary>
        /// <param name="archiveId">file to remove and delete.</param>
        /// <returns>true if deleted, false otherwise</returns>
        public override bool TryRemoveAndDelete(Guid archiveId)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            SortedList<Guid, ArchiveTableSummary<TKey, TValue>> partitions = m_list.m_fileSummaries;
            
            if (!partitions.TryGetValue(archiveId, out ArchiveTableSummary<TKey, TValue>? partition))
                return false;

            SortedTreeTable<TKey, TValue> tree = partition.SortedTreeTable;
            partitions.Remove(archiveId);

            m_list.AddFileToDelete(tree);
            return true;
        }

        #endregion
    }

    #endregion
}