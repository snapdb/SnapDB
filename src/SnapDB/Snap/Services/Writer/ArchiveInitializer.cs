//******************************************************************************************************
//  ArchiveInitializer.cs - Gbtc
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
//  07/24/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Storage;
using SnapDB.Snap.Types;
using SnapDB.Threading;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Creates new archive files based on user settings.
/// </summary>
/// <typeparam name="TKey">The key type used in the sorted tree table.</typeparam>
/// <typeparam name="TValue">The value type used in the sorted tree table.</typeparam>
public class ArchiveInitializer<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly ReaderWriterLockEasy m_lock;
    private int m_writePathSeed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="ArchiveInitializer{TKey,TValue}"/>
    /// </summary>
    /// <param name="settings"></param>
    public ArchiveInitializer(ArchiveInitializerSettings settings)
    {
        Settings = settings.CloneReadonly();
        Settings.Validate();
        m_lock = new ReaderWriterLockEasy();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets current settings.
    /// </summary>
    public ArchiveInitializerSettings Settings { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Replaces the existing settings with this new set.
    /// </summary>
    /// <param name="settings">The new settings that will replace the old settings.</param>
    public void UpdateSettings(ArchiveInitializerSettings settings)
    {
        settings = settings.CloneReadonly();
        settings.Validate();

        using (m_lock.EnterWriteLock())
            Settings = settings;
    }

    /// <summary>
    /// Creates a new <see cref="SortedTreeTable{TKey,TValue}"/> based on the settings passed to this class.
    /// Once created, it is up to he caller to make sure that this class is properly disposed of.
    /// </summary>
    /// <param name="estimatedSize">
    /// An optional estimated size (in bytes) for the archive file. Use a negative value to indicate no specific estimation.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="SortedTreeTable{TKey, TValue}"/> for archiving data.
    /// </returns>
    /// <remarks>
    /// This creates a new <see cref="SortedTreeTable{TKey, TValue}"/> instance
    /// for archiving data. It can create the table in-memory or in a file, depending on the <see cref="ServerSettings"/>
    /// configuration. If the estimatedSize is specified (non-negative), the method attempts to create the table in a file
    /// with enough space to accommodate the estimated data size.
    /// </remarks>
    public SortedTreeTable<TKey, TValue> CreateArchiveFile(long estimatedSize = -1)
    {
        using (m_lock.EnterReadLock())
        {
            if (Settings.IsMemoryArchive)
            {
                SortedTreeFile af = SortedTreeFile.CreateInMemory(4096, Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
            else
            {
                string fileName = CreateArchiveName(GetPathWithEnoughSpace(estimatedSize));
                SortedTreeFile af = SortedTreeFile.CreateFile(fileName, 4096, Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="SortedTreeTable{TKey,TValue}"/> based on the settings passed to this class.
    /// Once created, it is up to he caller to make sure that this class is properly disposed of.
    /// </summary>
    /// <param name="startKey">The key to start at.</param>
    /// <param name="endKey">The key to end at.</param>
    /// <param name="estimatedSize">
    /// An optional estimated size (in bytes) for the archive file. Use a negative value to indicate no specific estimation.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="SortedTreeTable{TKey, TValue}"/> for archiving data within the specified key range.
    /// </returns>
    public SortedTreeTable<TKey, TValue> CreateArchiveFile(TKey startKey, TKey endKey, long estimatedSize = -1)
    {
        using (m_lock.EnterReadLock())
        {
            if (Settings.IsMemoryArchive)
            {
                SortedTreeFile af = SortedTreeFile.CreateInMemory(4096, Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
            else
            {
                string fileName = CreateArchiveName(GetPathWithEnoughSpace(estimatedSize), startKey, endKey);
                SortedTreeFile af = SortedTreeFile.CreateFile(fileName, 4096, Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
        }
    }

    /// <summary>
    /// Selects a write path with enough free space, distributing selections across the configured write paths in
    /// round-robin order. See <see cref="ArchivePathHelper.GetPathWithEnoughSpace"/>.
    /// </summary>
    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    /// <param name="path">The base path where the archive file will be created.</param>
    /// <returns>
    /// A unique archive file name based on the specified path, server settings, and current timestamp.
    /// </returns>
    /// <remarks>
    /// This method generates a unique archive file name by combining the provided base
    /// path, server settings prefix, a new GUID, and the current timestamp. This ensures that the created archive file
    /// has a distinct and recognizable name.
    /// </remarks>
    private string CreateArchiveName(string path)
    {
        path = GetPath(path, DateTime.Now);
        return Path.Combine(path, Settings.Prefix.ToLower() + "-" + Guid.NewGuid() + "-" + DateTime.UtcNow.Ticks + Settings.FileExtension);
    }

    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    private string CreateArchiveName(string path, TKey startKey, TKey endKey)
    {
        if (startKey is not IHasTimestampField startTime || endKey is not IHasTimestampField endTime)
            return CreateArchiveName(path);

        if (!startTime.TryGetDateTime(out DateTime startDate) || !endTime.TryGetDateTime(out DateTime endDate))
            return CreateArchiveName(path);

        path = GetPath(path, startDate);

        return Path.Combine(path, Settings.Prefix.ToLower() + "-" + startDate.ToString("yyyy-MM-dd HH.mm.ss.fff") + "_to_" + endDate.ToString("yyyy-MM-dd HH.mm.ss.fff") + "-" + DateTime.UtcNow.Ticks + Settings.FileExtension);
    }

    private string GetPath(string rootPath, DateTime time)
    {
        return ArchivePathHelper.GetPath(rootPath, time, Settings.DirectoryMethod);
    }

    // Selects a write path with enough free space, distributing selections across the configured write paths in
    // round-robin order via a per-instance interlocked seed.
    private string GetPathWithEnoughSpace(long estimatedSize)
    {
        return ArchivePathHelper.GetPathWithEnoughSpace(Settings.WritePath, estimatedSize, Settings.DesiredRemainingSpace, Settings.FillMethod, Interlocked.Increment(ref m_writePathSeed) - 1);
    }

    #endregion
}