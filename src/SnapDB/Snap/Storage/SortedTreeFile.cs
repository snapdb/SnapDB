﻿//******************************************************************************************************
//  SortedTreeFile.cs - Gbtc
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
//  05/19/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Tree;

namespace SnapDB.Snap.Storage;

/// <summary>
/// Represents a individual self-contained archive file.
/// </summary>
/// <remarks>
/// </remarks>
public class SortedTreeFile : IDisposable
{
    #region [ Members ]

    private TransactionalFileStructure m_fileStructure;

    private readonly SortedList<SubFileName, IDisposable> m_openedFiles;

    #endregion

    #region [ Constructors ]

    private SortedTreeFile()
    {
        m_openedFiles = new SortedList<SubFileName, IDisposable>();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the size of the file.
    /// </summary>
    public long ArchiveSize => m_fileStructure.ArchiveSize;

    /// <summary>
    /// Gets the name of the file, but only the file, not the entire path.
    /// </summary>
    public string FileName
    {
        get
        {
            if (FilePath == string.Empty)
                return string.Empty;
            
            return Path.GetFileName(FilePath);
        }
    }

    /// <summary>
    /// Returns the name of the file.  Returns <see cref="String.Empty"/> if this is a memory archive.
    /// This is the name of the entire path.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Determines if the archive file has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets if the file is a memory file.
    /// </summary>
    public bool IsMemoryFile => FilePath == string.Empty;

    /// <summary>
    /// Gets the last committed read-only snapshot associated with this instance.
    /// </summary>
    /// <remarks>
    /// The <see cref="Snapshot"/> property provides access to the read-only snapshot of the associated data.
    /// This snapshot allows for querying and reading data but does not support write operations.
    /// </remarks>
    public ReadSnapshot Snapshot => m_fileStructure.Snapshot;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Closes the archive file. If there is a current transaction,
    /// that transaction is immediately rolled back and disposed.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;
        
        foreach (IDisposable d in m_openedFiles.Values)
            d.Dispose();
        
        m_openedFiles.Clear();
        m_fileStructure.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    /// Changes the extension of the current file.
    /// </summary>
    /// <param name="extension">the new extension</param>
    /// <param name="isReadOnly">If the file should be reopened as readonly</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_fileStructure.ChangeExtension(extension, isReadOnly, isSharingEnabled);
        FilePath = m_fileStructure.FileName;
    }

    /// <summary>
    /// Reopens the file with different permissions.
    /// </summary>
    /// <param name="isReadOnly">If the file should be reopened as readonly</param>
    /// <param name="isSharingEnabled">If the file should share read privileges.</param>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_fileStructure.ChangeShareMode(isReadOnly, isSharingEnabled);
    }

    /// <summary>
    /// Opens the default table for this TKey and TValue.
    /// </summary>
    /// <typeparam name="TKey">The key</typeparam>
    /// <typeparam name="TValue">The value</typeparam>
    /// <remarks>
    /// Every Key and Value have their uniquely mapped file, therefore a different file is opened if TKey and TValue are different.
    /// </remarks>
    /// <returns>null if table does not exist</returns>
    public SortedTreeTable<TKey, TValue>? OpenTable<TKey, TValue>() where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return OpenTable<TKey, TValue>(GetArchiveFileName<TKey, TValue>());
    }

    /// <summary>
    /// Opens the default table for this TKey and TValue.
    /// </summary>
    /// <typeparam name="TKey">The key</typeparam>
    /// <typeparam name="TValue">The value</typeparam>
    /// <param name="tableName">the name of an internal table</param>
    /// <remarks>
    /// Every Key and Value have their uniquely mapped file, therefore a different file is opened if TKey and TValue are different.
    /// </remarks>
    /// <returns>null if table does not exist</returns>
    public SortedTreeTable<TKey, TValue>? OpenTable<TKey, TValue>(string tableName) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return OpenTable<TKey, TValue>(GetArchiveFileName<TKey, TValue>(tableName));
    }

    /// <summary>
    /// Opens the default table for this TKey and TValue. If it does not exists,
    /// it will be created with the provided compression method.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the table.</typeparam>
    /// <typeparam name="TValue">The type of the values in the table.</typeparam>
    /// <param name="storageMethod">The encoding method used for storage.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="maxSortedTreeBlockSize">The maximum block size for the sorted tree (default is 4096).</param>
    /// <returns>An instance of <see cref="SortedTreeTable{TKey, TValue}"/>.</returns>
    public SortedTreeTable<TKey, TValue> OpenOrCreateTable<TKey, TValue>(EncodingDefinition storageMethod, string tableName, int maxSortedTreeBlockSize = 4096) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (storageMethod is null)
            throw new ArgumentNullException(nameof(storageMethod));

        SubFileName fileName = GetArchiveFileName<TKey, TValue>(tableName);
        
        return OpenOrCreateTable<TKey, TValue>(storageMethod, fileName, maxSortedTreeBlockSize);
    }

    /// <summary>
    /// Opens the default table for this TKey and TValue. If it does not exists,
    /// it will be created with the provided compression method.
    /// </summary>
    /// <typeparam name="TKey">The type parameter specifying the data type for keys.</typeparam>
    /// <typeparam name="TValue">The type parameter specifying the data type for values.</typeparam>
    /// <param name="storageMethod">The encoding method used to store data.</param>
    /// <param name="maxSortedTreeBlockSize">The maximum block size for the created <see cref="SortedTreeTable{TKey, TValue}"/>.</param>
    /// <returns>
    /// A <see cref="SortedTreeTable{TKey, TValue}"/> instance associated with the specified storage method and options.
    /// </returns>
    public SortedTreeTable<TKey, TValue> OpenOrCreateTable<TKey, TValue>(EncodingDefinition storageMethod, int maxSortedTreeBlockSize = 4096) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (storageMethod is null)
            throw new ArgumentNullException(nameof(storageMethod));

        SubFileName fileName = GetArchiveFileName<TKey, TValue>();
        
        return OpenOrCreateTable<TKey, TValue>(storageMethod, fileName, maxSortedTreeBlockSize);
    }

    /// <summary>
    /// Gets the metadata from the archive file associated with the specified key and value types.
    /// </summary>
    /// <typeparam name="TKey">The type parameter specifying the data type for keys.</typeparam>
    /// <typeparam name="TValue">The type parameter specifying the data type for values.</typeparam>
    /// <returns>Metadata extracted from archive file; otherwise, <c>null</c> if no metadata exists.</returns>
    public byte[]? GetMetadata<TKey, TValue>() where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        SubFileName fileName = GetMetadataFileName<TKey, TValue>();

        if (!m_fileStructure.Snapshot.Header.ContainsSubFile(fileName))
            return null;

        using SubFileStream file = m_fileStructure.Snapshot.OpenFile(fileName);
        using BinaryStream bs = new(file);

        return bs.ReadBytes();
    }

    /// <summary>
    /// Closes and deletes the Archive File. Also calls dispose.
    /// If this is a memory archive, it will release the memory space to the buffer pool.
    /// </summary>
    public void Delete()
    {
        Dispose();

        if (FilePath != string.Empty)
            File.Delete(FilePath);
    }

    /// <summary>
    /// Opens the table for the provided file name.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="fileName">the filename to open</param>
    /// <returns>null if table does not exist</returns>
    private SortedTreeTable<TKey, TValue>? OpenTable<TKey, TValue>(SubFileName fileName) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (m_openedFiles.TryGetValue(fileName, out IDisposable? file))
            return (SortedTreeTable<TKey, TValue>)file;
        
        if (!m_fileStructure.Snapshot.Header.ContainsSubFile(fileName))
            return null;
        
        m_openedFiles.Add(fileName, new SortedTreeTable<TKey, TValue>(m_fileStructure, fileName, this));

        return (SortedTreeTable<TKey, TValue>)m_openedFiles[fileName];
    }

    private SortedTreeTable<TKey, TValue> OpenOrCreateTable<TKey, TValue>(EncodingDefinition storageMethod, SubFileName fileName, int maxSortedTreeBlockSize) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (!m_openedFiles.ContainsKey(fileName))
        {
            if (!m_fileStructure.Snapshot.Header.ContainsSubFile(fileName))
                CreateArchiveFile<TKey, TValue>(fileName, storageMethod, maxSortedTreeBlockSize);
            
            m_openedFiles.Add(fileName, new SortedTreeTable<TKey, TValue>(m_fileStructure, fileName, this));
        }

        return (SortedTreeTable<TKey, TValue>)m_openedFiles[fileName];
    }

    private void CreateArchiveFile<TKey, TValue>(SubFileName fileName, EncodingDefinition storageMethod, int maxSortedTreeBlockSize) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (maxSortedTreeBlockSize < 1024)
            throw new ArgumentOutOfRangeException(nameof(maxSortedTreeBlockSize), "Must be greater than 1024");
        
        if (storageMethod is null)
            throw new ArgumentNullException(nameof(storageMethod));

        using TransactionalEdit trans = m_fileStructure.BeginEdit();
        using (SubFileStream fs = trans.CreateFile(fileName))
        using (BinaryStream bs = new(fs))
        {
            int blockSize = m_fileStructure.Snapshot.Header.DataBlockSize;

            while (blockSize > maxSortedTreeBlockSize)
                blockSize >>= 2;

            SortedTree<TKey, TValue> tree = SortedTree<TKey, TValue>.Create(bs, blockSize, storageMethod);
            tree.Flush();
        }

        trans.ArchiveType = FileType;
        trans.CommitAndDispose();
    }

    #endregion

    #region [ Static ]

    // {63AB3FEA-14CD-4ECA-939B-0DD23742E170}
    /// <summary>
    /// The main type of the archive file.
    /// </summary>
    internal static readonly Guid FileType = new(0x63ab3fea, 0x14cd, 0x4eca, 0x93, 0x9b, 0x0d, 0xd2, 0x37, 0x42, 0xe1, 0x70);

    // {E0FCA590-F46E-4060-8764-DFDCFC74D728}
    /// <summary>
    /// The guid where the primary archive component exists
    /// </summary>
    internal static readonly Guid PrimaryArchiveType = new(0xe0fca590, 0xf46e, 0x4060, 0x87, 0x64, 0xdf, 0xdc, 0xfc, 0x74, 0xd7, 0x28);

    // {BDDC2947-D7A2-45B2-AEF1-AF1947311BD0}
    /// <summary>
    /// The guid where the primary archive component exists
    /// </summary>
    internal static readonly Guid MetadataArchiveType = new(0xbddc2947, 0xd7a2, 0x45b2, 0xae, 0xf1, 0xaf, 0x19, 0x47, 0x31, 0x1b, 0xd0);

    /// <summary>
    /// Creates a new in memory archive file.
    /// </summary>
    /// <param name="blockSize">The number of bytes per block in the file.</param>
    /// <param name="flags">Flags to write to the file</param>
    /// <returns>The new in-memory archive file.</returns>
    public static SortedTreeFile CreateInMemory(int blockSize = 4096, params Guid[] flags)
    {
        return new SortedTreeFile
        {
            FilePath = string.Empty,
            m_fileStructure = TransactionalFileStructure.CreateInMemory(blockSize, flags)
        };
    }

    /// <summary>
    /// Creates an archive file.
    /// </summary>
    /// <param name="file">the path for the file.</param>
    /// <param name="blockSize">The number of bytes per block in the file.</param>
    /// <param name="flags">Flags to write to the file</param>
    /// <returns>
    /// The newly created <see cref="SortedTreeFile"/>.
    /// </returns>
    public static SortedTreeFile CreateFile(string file, int blockSize = 4096, params Guid[] flags)
    {
        SortedTreeFile af = new();
        file = Path.GetFullPath(file);
        af.FilePath = file;
        af.m_fileStructure = TransactionalFileStructure.CreateFile(file, blockSize, flags);
        return af;
    }

    /// <summary>
    /// Opens an archive file.
    /// </summary>
    /// <param name="file">The path to the SortedTreeFile to open.</param>
    /// <param name="isReadOnly">True if the file should be opened in read-only mode; otherwise, false.</param>
    /// <returns>
    /// A new instance of <see cref="SortedTreeFile"/> representing the opened SortedTreeFile.
    /// </returns>
    public static SortedTreeFile OpenFile(string file, bool isReadOnly)
    {
        SortedTreeFile af = new();

        file = Path.GetFullPath(file);
        af.FilePath = file;
        af.m_fileStructure = TransactionalFileStructure.OpenFile(file, isReadOnly);
        
        if (af.m_fileStructure.Snapshot.Header.ArchiveType != FileType)
            throw new Exception("Archive type is unknown");

        return af;
    }

    // Helper method to create the SubFileName for the default table.
    // Returns a unique SubFileName associated with the specified key and value types.
    private static SubFileName GetArchiveFileName<TKey, TValue>() where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        Guid keyType = new TKey().GenericTypeGuid;
        Guid valueType = new TValue().GenericTypeGuid;
        return SubFileName.Create(PrimaryArchiveType, keyType, valueType);
    }

    // Helper method to create the SubFileName for the specified table name.
    // Returns a unique SubFileName associated with the specified table name and key and value types.
    private static SubFileName GetArchiveFileName<TKey, TValue>(string fileName) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        Guid keyType = new TKey().GenericTypeGuid;
        Guid valueType = new TValue().GenericTypeGuid;
        return SubFileName.Create(fileName, keyType, valueType);
    }

    // Helper method to create the SubFileName for the default metadata.
    // Returns a unique SubFileName associated with the specified key and value types.
    private static SubFileName GetMetadataFileName<TKey, TValue>() where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        Guid keyType = new TKey().GenericTypeGuid;
        Guid valueType = new TValue().GenericTypeGuid;
        return SubFileName.Create(MetadataArchiveType, keyType, valueType);
    }

    #endregion
}