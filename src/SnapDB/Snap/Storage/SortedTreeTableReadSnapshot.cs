//******************************************************************************************************
//  SortedTreeTableReadSnapshot`2.cs - Gbtc
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
//  05/22/2012 - Steven E. Chisholm
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

/// <summary>
/// Provides a user with a read-only instance of an archive.
/// This class is not thread safe.
/// </summary>
/// <typeparam name="TKey">The key type used in the sorted tree table.</typeparam>
/// <typeparam name="TValue">The value type used in the sorted tree table.</typeparam>
public class SortedTreeTableReadSnapshot<TKey, TValue> : IDisposable where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private BinaryStream m_binaryStream;

    private SubFileStream m_subStream;
    private SortedTree<TKey, TValue> m_tree;

    #endregion

    #region [ Constructors ]

    internal SortedTreeTableReadSnapshot(ReadSnapshot currentTransaction, SubFileName fileName)
    {
        try
        {
            m_subStream = currentTransaction.OpenFile(fileName);
            m_binaryStream = new BinaryStream(m_subStream);
            m_tree = SortedTree<TKey, TValue>.Open(m_binaryStream);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Determines if this read snapshot has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
            try
            {
                m_binaryStream?.Dispose();
                m_subStream?.Dispose();
            }
            finally
            {
                m_subStream = null;
                m_binaryStream = null;
                m_tree = null;
                IsDisposed = true;
            }
    }

    /// <summary>
    /// Gets a reader that can be used to parse an archive file.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="SortedTreeScannerBase{TKey, TValue}"/> for scanning the entire SortedTreeTable.
    /// </returns>
    public SortedTreeScannerBase<TKey, TValue> GetTreeScanner()
    {
        return m_tree.CreateTreeScanner();
    }

    /// <summary>
    /// Returns the lower and upper bounds of the tree
    /// </summary>
    /// <param name="lowerBounds">the first key in the tree</param>
    /// <param name="upperBounds">the last key in the tree</param>
    /// <remarks>
    /// If the tree is empty, lowerBounds will be greater than upperBounds
    /// </remarks>
    public void GetKeyRange(TKey lowerBounds, TKey upperBounds)
    {
        m_tree.GetKeyRange(lowerBounds, upperBounds);
    }

    #endregion
}