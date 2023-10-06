//******************************************************************************************************
//  ArchiveInitializer`2.cs - Gbtc
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

using Gemstone.IO;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Types;
using SnapDB.Threading;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Creates new archive files based on user settings.
/// </summary>
public class SimplifiedArchiveInitializer<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly ReaderWriterLockEasy m_lock;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="ArchiveInitializer{TKey,TValue}"/>
    /// </summary>
    /// <param name="settings"></param>
    public SimplifiedArchiveInitializer(SimplifiedArchiveInitializerSettings settings)
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
    public SimplifiedArchiveInitializerSettings Settings { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Replaces the existing settings with this new set.
    /// </summary>
    /// <param name="settings"></param>
    public void UpdateSettings(SimplifiedArchiveInitializerSettings settings)
    {
        settings = settings.CloneReadonly();
        settings.Validate();
        using (m_lock.EnterWriteLock())
        {
            Settings = settings;
        }
    }

    /// <summary>
    /// Creates an archive file with specified keys, estimated size, and data.
    /// </summary>
    /// <param name="startKey">The first key of the archive file.</param>
    /// <param name="endKey">The last key of the archive file.</param>
    /// <param name="estimatedSize">The estimated size of the archive file.</param>
    /// <param name="data">The data to be written to the archive file.</param>
    /// <param name="archiveIdCallback">A callback function to handle the archive file's unique identifier.</param>
    /// <returns>A sorted tree table representing the created archive file.</returns>
    /// <remarks>
    /// The <see cref="CreateArchiveFile"/> method creates an archive file with the specified start and end keys, estimated size,
    /// and data. It uses a pending file to write the data and then renames it to the final file. The archive file is opened and
    /// returned as a sorted tree table.
    /// </remarks>
    public SortedTreeTable<TKey, TValue> CreateArchiveFile(TKey startKey, TKey endKey, long estimatedSize, TreeStream<TKey, TValue> data, Action<Guid> archiveIdCallback)
    {
        SimplifiedArchiveInitializerSettings settings = Settings;

        string pendingFile = CreateArchiveName(GetPathWithEnoughSpace(estimatedSize), startKey, endKey);
        string finalFile = Path.ChangeExtension(pendingFile, settings.FinalExtension);

        SortedTreeFileSimpleWriter<TKey, TValue>.Create(pendingFile, finalFile, 4096, archiveIdCallback, settings.EncodingMethod, data, settings.Flags.ToArray());

        return SortedTreeFile.OpenFile(finalFile, true).OpenTable<TKey, TValue>();
    }

    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    /// <param name="path">The base path for the archive file.</param>
    /// <returns>A unique archive file name.</returns>
    /// <remarks>
    /// This method generates a unique archive file name based on the specified path and includes
    /// a timestamp, a unique identifier, and the specified file extension.
    /// </remarks>
    private string CreateArchiveName(string path)
    {
        path = GetPath(path, DateTime.Now);
        return Path.Combine(path, Settings.Prefix.ToLower() + "-" + Guid.NewGuid() + "-" + DateTime.UtcNow.Ticks + Settings.PendingExtension);
    }

    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    /// <param name="path">The base path for the archive file.</param>
    /// <param name="startKey">The start key for the archive data range.</param>
    /// <param name="endKey">The end key for the archive data range.</param>
    /// <returns>A unique archive file name.</returns>
    /// <remarks>
    /// This method generates a unique archive file name based on the specified path, 
    /// start key, end key, and includes a timestamp, a unique identifier, and the specified file extension.
    /// </remarks>
    private string CreateArchiveName(string path, TKey startKey, TKey endKey)
    {
        if (startKey is not IHasTimestampField startTime || endKey is not IHasTimestampField endTime)
            return CreateArchiveName(path);

        if (!startTime.TryGetDateTime(out DateTime startDate) || !endTime.TryGetDateTime(out DateTime endDate))
            return CreateArchiveName(path);

        path = GetPath(path, startDate);
        return Path.Combine(path, Settings.Prefix.ToLower() + "-" + startDate.ToString("yyyy-MM-dd HH.mm.ss.fff") + "_to_" + endDate.ToString("yyyy-MM-dd HH.mm.ss.fff") + "-" + DateTime.UtcNow.Ticks + Settings.PendingExtension);
    }

    private string GetPath(string rootPath, DateTime time)
    {
        switch (Settings.DirectoryMethod)
        {
            case ArchiveDirectoryMethod.TopDirectoryOnly:
                break;
            case ArchiveDirectoryMethod.Year:
                rootPath = Path.Combine(rootPath, time.Year.ToString());
                break;
            case ArchiveDirectoryMethod.YearMonth:
                rootPath = Path.Combine(rootPath, time.Year + time.Month.ToString("00"));
                break;
            case ArchiveDirectoryMethod.YearThenMonth:
                rootPath = Path.Combine(rootPath, time.Year.ToString() + '\\' + time.Month.ToString("00"));
                break;
        }

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    private string GetPathWithEnoughSpace(long estimatedSize)
    {
        if (estimatedSize < 0)
            return Settings.WritePath.First();
        long remainingSpace = Settings.DesiredRemainingSpace;
        foreach (string path in Settings.WritePath)
        {
            FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _);
            if (freeSpace - estimatedSize > remainingSpace)
                return path;
        }

        throw new Exception("Out of free space");
    }

    #endregion
}