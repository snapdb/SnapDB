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

using Gemstone.IO;
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
            return Settings.WritePath.FirstOrDefault() ?? throw new InvalidOperationException("No write path defined");

        long remainingSpace = Settings.DesiredRemainingSpace;

        if (Settings.BalancingMethod == BalancingMethod.FillToDesired)
        {
            foreach (string path in Settings.WritePath)
            {
                FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _);

                if (freeSpace - estimatedSize > remainingSpace)
                    return path;

            }
        }

        if (Settings.BalancingMethod == BalancingMethod.FillSmallestAvailable)
        {
            long current = 0;
            string smallest = null;

            foreach (string path in Settings.WritePath)
            {
                FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _);

                if (freeSpace - estimatedSize < remainingSpace)
                    continue;

                if (freeSpace < current)
                {
                smallest = path;
                current = freeSpace;
                }
            }

            if (!string.IsNullOrEmpty(smallest))
                return smallest;
        }

        if (Settings.BalancingMethod == BalancingMethod.FillLargestAvailable)
        {
            long current = 0;
            string largest = null;

            foreach (string path in Settings.WritePath)
            {
                FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _);

                if (freeSpace - estimatedSize < remainingSpace)
                    continue;

                if (freeSpace > current)
                {
                    largest = path;
                    current = freeSpace;
                }
            }

            if(!string.IsNullOrEmpty(largest)) 
                return largest;

        }

        if (Settings.BalancingMethod == BalancingMethod.FillLargestTotal)
        {
            long current = 0;
            string largestTotal = null;

            foreach (string path in Settings.WritePath)
            {
                FilePath.GetAvailableFreeSpace(path, out long freeSpace, out long totalSize);

                if (freeSpace - estimatedSize < totalSize)
                    continue;
                if (current < totalSize)
                {
                    largestTotal = path;
                    current = totalSize;
                }
            }

            if (!string.IsNullOrEmpty(largestTotal))
                return largestTotal;
        }

        if (Settings.BalancingMethod == BalancingMethod.FillToMatchingPercentage)
        {
            // how full the current path is (percentage format)
            long currentPercentage;
            // how full the fullest one is, which every other path will be compared to
            long highestPercentage = 0;
            // the location with the highest percentage
            string fillStandard = null;

            foreach (string path in Settings.WritePath)
            {
                FilePath.GetAvailableFreeSpace(path, out long freeSpace, out long totalSize);

                currentPercentage = freeSpace/totalSize;

                if (freeSpace - estimatedSize < remainingSpace)
                    continue;

                if (currentPercentage > highestPercentage)
                {
                    fillStandard = path;
                    highestPercentage = currentPercentage;
                }

            }

            if (!string.IsNullOrEmpty(fillStandard))
                return fillStandard;
        }

        //for pct, calculate percentage as freespace/totalspace then pick the largest to fill to a point

        throw new InvalidOperationException("Out of free space");
    }

    #endregion
}