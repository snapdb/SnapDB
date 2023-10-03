﻿//******************************************************************************************************
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
public class ArchiveInitializer<TKey, TValue>
    where TKey : SnapTypeBase<TKey>, new()
    where TValue : SnapTypeBase<TValue>, new()
{
    private readonly ReaderWriterLockEasy m_lock;

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

    /// <summary>
    /// Gets current settings.
    /// </summary>
    public ArchiveInitializerSettings Settings { get; private set; }

    /// <summary>
    /// Replaces the existing settings with this new set.
    /// </summary>
    /// <param name="settings"></param>
    public void UpdateSettings(ArchiveInitializerSettings settings)
    {
        settings = settings.CloneReadonly();
        settings.Validate();

        using (m_lock.EnterWriteLock())
        {
            Settings = settings;
        }
    }

    /// <summary>
    /// Creates a new <see cref="SortedTreeTable{TKey,TValue}"/> based on the settings passed to this class.
    /// Once created, it is up to he caller to make sure that this class is properly disposed of.
    /// </summary>
    /// <param name="estimatedSize">The estimated size of the file. -1 to ignore this feature and write to the first available directory.</param>
    /// <returns></returns>
    public SortedTreeTable<TKey, TValue> CreateArchiveFile(long estimatedSize = -1)
    {
        using (m_lock.EnterReadLock())
        {
            if (Settings.IsMemoryArchive)
            {
                SortedTreeFile af = SortedTreeFile.CreateInMemory(blockSize: 4096, flags: Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
            else
            {
                string fileName = CreateArchiveName(GetPathWithEnoughSpace(estimatedSize));
                SortedTreeFile af = SortedTreeFile.CreateFile(fileName, blockSize: 4096, flags: Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
        }

    }

    /// <summary>
    /// Creates a new <see cref="SortedTreeTable{TKey,TValue}"/> based on the settings passed to this class.
    /// Once created, it is up to he caller to make sure that this class is properly disposed of.
    /// </summary>
    /// <param name="startKey">the first key in the archive file</param>
    /// <param name="endKey">the last key in the archive file</param>
    /// <param name="estimatedSize">The estimated size of the file. -1 to ignore this feature and write to the first available directory.</param>
    /// <returns></returns>
    public SortedTreeTable<TKey, TValue> CreateArchiveFile(TKey startKey, TKey endKey, long estimatedSize = -1)
    {
        using (m_lock.EnterReadLock())
        {
            if (Settings.IsMemoryArchive)
            {
                SortedTreeFile af = SortedTreeFile.CreateInMemory(blockSize: 4096, flags: Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
            else
            {
                string fileName = CreateArchiveName(GetPathWithEnoughSpace(estimatedSize), startKey, endKey);
                SortedTreeFile af = SortedTreeFile.CreateFile(fileName, blockSize: 4096, flags: Settings.Flags.ToArray());
                return af.OpenOrCreateTable<TKey, TValue>(Settings.EncodingMethod);
            }
        }
    }

    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    /// <returns></returns>
    private string CreateArchiveName(string path)
    {
        path = GetPath(path, DateTime.Now);
        return Path.Combine(path, Settings.Prefix.ToLower() + "-" + Guid.NewGuid() + "-" + DateTime.UtcNow.Ticks + Settings.FileExtension);
    }

    /// <summary>
    /// Creates a new random file in one of the provided folders in a round robin fashion.
    /// </summary>
    /// <returns></returns>
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
        switch (Settings.DirectoryMethod)
        {
            case ArchiveDirectoryMethod.TopDirectoryOnly:
                break;
            case ArchiveDirectoryMethod.Year:
                rootPath = Path.Combine(rootPath, time.Year.ToString());
                break;
            case ArchiveDirectoryMethod.YearMonth:
                rootPath = Path.Combine(rootPath, time.Year.ToString() + time.Month.ToString("00"));
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
            return Settings.WritePath.FirstOrDefault() ?? throw new InvalidOperationException("No write path defined");
        
        long remainingSpace = Settings.DesiredRemainingSpace;
        
        foreach (string path in Settings.WritePath)
        {
            FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _);

            if (freeSpace - estimatedSize > remainingSpace)
                return path;
        }
        
        throw new InvalidOperationException("Out of free space");
    }
}