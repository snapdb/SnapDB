//******************************************************************************************************
//  ArchivePathHelper.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
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
//  06/23/2026 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using Gemstone.IO;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Provides shared helpers for resolving archive file directories and selecting a write directory from a set of
/// candidate paths based on available free space.
/// </summary>
/// <remarks>
/// These helpers centralize the directory layout and disk-space-aware path selection rules used by the archive
/// writers (e.g., <see cref="ArchiveInitializer{TKey,TValue}"/> and
/// <see cref="SimplifiedArchiveInitializer{TKey,TValue}"/>) so that any other component needing to place files
/// alongside the historian archive (<c>.d2</c>) files can follow identical rules.
/// </remarks>
public static class ArchivePathHelper
{
    /// <summary>
    /// Gets the directory, relative to an archive root, in which files for the specified <paramref name="time"/>
    /// are placed according to the given <paramref name="directoryMethod"/>.
    /// </summary>
    /// <param name="time">Timestamp used to derive the dated sub-directory.</param>
    /// <param name="directoryMethod">Directory structure to follow.</param>
    /// <returns>
    /// The relative directory, or an empty string for <see cref="ArchiveDirectoryMethod.TopDirectoryOnly"/>. This
    /// method performs no I/O.
    /// </returns>
    public static string GetRelativeDirectory(DateTime time, ArchiveDirectoryMethod directoryMethod)
    {
        return directoryMethod switch
        {
            ArchiveDirectoryMethod.Year => time.Year.ToString(),
            ArchiveDirectoryMethod.YearMonth => time.Year + time.Month.ToString("00"),
            ArchiveDirectoryMethod.YearThenMonth => Path.Combine(time.Year.ToString(), time.Month.ToString("00")),
            _ => string.Empty // TopDirectoryOnly
        };
    }

    /// <summary>
    /// Resolves the full directory for the specified <paramref name="rootPath"/> and <paramref name="time"/>
    /// according to the given <paramref name="directoryMethod"/>, creating the directory if it does not exist.
    /// </summary>
    /// <param name="rootPath">Root archive directory.</param>
    /// <param name="time">Timestamp used to derive the dated sub-directory.</param>
    /// <param name="directoryMethod">Directory structure to follow.</param>
    /// <returns>The full (and existing) directory path.</returns>
    public static string GetPath(string rootPath, DateTime time, ArchiveDirectoryMethod directoryMethod)
    {
        string relativeDirectory = GetRelativeDirectory(time, directoryMethod);
        string path = relativeDirectory.Length == 0 ? rootPath : Path.Combine(rootPath, relativeDirectory);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>
    /// Selects a write directory from <paramref name="paths"/> that has enough free space to store a file of the
    /// estimated size while still leaving the desired remaining space, distributing selections across the
    /// candidate paths in round-robin order.
    /// </summary>
    /// <param name="paths">Candidate write directories, in configured order. Must contain at least one entry.</param>
    /// <param name="estimatedSize">
    /// Estimated size, in bytes, of the file to be written. A negative value skips the free-space check.
    /// </param>
    /// <param name="desiredRemainingSpace">Minimum free space, in bytes, to leave on the target drive.</param>
    /// <param name="fillMethod">
    /// Determines how the starting candidate is chosen: <see cref="ArchiveDirectoryFillMethod.Sequential"/> always
    /// starts at the first path (filling paths in order), while <see cref="ArchiveDirectoryFillMethod.RoundRobin"/>
    /// rotates the starting candidate using <paramref name="roundRobinSeed"/>.
    /// </param>
    /// <param name="roundRobinSeed">
    /// A value used to rotate the starting candidate when <paramref name="fillMethod"/> is
    /// <see cref="ArchiveDirectoryFillMethod.RoundRobin"/>; ignored otherwise. Callers typically pass a per-instance
    /// counter (e.g., an interlocked increment) so that each call advances the round-robin position.
    /// </param>
    /// <returns>The selected write directory.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="paths"/> is <c>null</c> or empty, or no path has sufficient free space.
    /// </exception>
    public static string GetPathWithEnoughSpace(IList<string> paths, long estimatedSize, long desiredRemainingSpace, ArchiveDirectoryFillMethod fillMethod, int roundRobinSeed)
    {
        if (paths is null || paths.Count == 0)
            throw new InvalidOperationException("No write path defined");

        int count = paths.Count;

        // Sequential fill always starts at the first path; round-robin rotates the start using the seed
        int start = fillMethod == ArchiveDirectoryFillMethod.RoundRobin ? ((roundRobinSeed % count) + count) % count : 0;

        // No size estimate: return the selected path without a free-space check
        if (estimatedSize < 0)
            return paths[start];

        // Scan all candidate paths once, beginning at the round-robin position and wrapping around, returning the
        // first whose drive has enough free space to store the estimated file while leaving the desired remaining space
        for (int offset = 0; offset < count; offset++)
        {
            string path = paths[(start + offset) % count];

            if (FilePath.GetAvailableFreeSpace(path, out long freeSpace, out _) && freeSpace - estimatedSize > desiredRemainingSpace)
                return path;
        }

        throw new InvalidOperationException("Out of free space");
    }
}
