﻿//******************************************************************************************************
//  SortedTreeTable`2_Editor.cs - Gbtc
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
//  05/19/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/19/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Tree;

namespace SnapDB.Snap.Storage;

public partial class SortedTreeTable<TKey, TValue>
{
    #region [ Members ]

    /// <summary>
    /// A single instance editor that is used
    /// to modifiy an archive file.
    /// </summary>
    private class Editor : SortedTreeTableEditor<TKey, TValue>
    {
        #region [ Members ]

        private BinaryStream m_binaryStream1;

        private readonly TransactionalEdit m_currentTransaction;
        private SortedTreeTable<TKey, TValue> m_sortedTreeFile;
        private SubFileStream m_subStream;
        private SortedTree<TKey, TValue> m_tree;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        internal Editor(SortedTreeTable<TKey, TValue> sortedTreeFile)
        {
            m_sortedTreeFile = sortedTreeFile;
            m_currentTransaction = m_sortedTreeFile.m_fileStructure.BeginEdit();
            m_subStream = m_currentTransaction.OpenFile(sortedTreeFile.m_fileName);
            m_binaryStream1 = new BinaryStream(m_subStream);
            m_tree = SortedTree<TKey, TValue>.Open(m_binaryStream1);
            m_tree.AutoFlush = false;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Editor"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
                try
                {
                    // This will be done regardless of whether the object is finalized or disposed.
                    if (disposing)
                        Rollback();
                    // This will be done only when the object is disposed by calling Dispose().
                }
                finally
                {
                    m_disposed = true; // Prevent duplicate dispose.
                    base.Dispose(disposing); // Call base class Dispose().
                }
        }

        /// <summary>
        /// Commits the edits to the current archive file and disposes of this class.
        /// </summary>
        public override void Commit()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            GetKeyRange(m_sortedTreeFile.FirstKey, m_sortedTreeFile.LastKey);

            if (m_tree is not null)
            {
                m_tree.Flush();
                m_tree = null;
            }

            if (m_binaryStream1 is not null)
            {
                m_binaryStream1.Dispose();
                m_binaryStream1 = null;
            }

            if (m_subStream is not null)
            {
                m_subStream.Dispose();
                m_subStream = null;
            }

            m_currentTransaction.CommitAndDispose();
            InternalDispose();
        }

        /// <summary>
        /// Rolls back all edits that are made to the archive file and disposes of this class.
        /// </summary>
        public override void Rollback()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            m_tree = null;
            if (m_binaryStream1 is not null)
            {
                m_binaryStream1.Dispose();
                m_binaryStream1 = null;
            }

            if (m_subStream is not null)
            {
                m_subStream.Dispose();
                m_subStream = null;
            }

            m_currentTransaction.RollbackAndDispose();
            InternalDispose();
        }

        /// <summary>
        /// Gets the lower and upper bounds of this tree.
        /// </summary>
        /// <param name="firstKey">The first key in the tree</param>
        /// <param name="lastKey">The final key in the tree</param>
        /// <remarks>
        /// If the tree contains no data. <paramref name="firstKey"/> is set to it's maximum value
        /// and <paramref name="lastKey"/> is set to it's minimum value.
        /// </remarks>
        public override void GetKeyRange(TKey firstKey, TKey lastKey)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            m_tree.GetKeyRange(firstKey, lastKey);
        }

        /// <summary>
        /// Adds a single point to the archive file.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">the value</param>
        public override void AddPoint(TKey key, TValue value)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            m_tree.TryAdd(key, value);
        }

        /// <summary>
        /// Adds all of the points to this archive file.
        /// </summary>
        /// <param name="stream"></param>
        public override void AddPoints(TreeStream<TKey, TValue> stream)
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            m_tree.TryAddRange(stream);
        }

        /// <summary>
        /// Opens a tree scanner for this archive file
        /// </summary>
        /// <summary>
        /// Gets a new instance of <see cref="SortedTreeScannerBase{TKey, TValue}"/> to scan the entire range of the SortedTreeTable.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SortedTreeScannerBase{TKey, TValue}"/> for scanning the entire range.
        /// </returns>
        public override SortedTreeScannerBase<TKey, TValue> GetRange()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);

            return m_tree.CreateTreeScanner();
        }

        private void InternalDispose()
        {
            m_disposed = true;
            m_sortedTreeFile.m_activeEditor = null;
            m_sortedTreeFile = null;
        }

        #endregion
    }

    #endregion
}