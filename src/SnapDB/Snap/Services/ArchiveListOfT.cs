﻿//******************************************************************************************************
//  ArchiveList.cs - Gbtc
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

using System.Text;
using Gemstone;
using Gemstone.Diagnostics;
using Gemstone.IO;
using Gemstone.Threading;
using SnapDB.Collections;
using SnapDB.Snap.Storage;

// ReSharper disable VirtualMemberCallInConstructor
namespace SnapDB.Snap.Services;

/// <summary>
/// Manages the complete list of archive resources and the
/// associated reading and writing that goes along with it.
/// </summary>
/// <typeparam name="TKey">The key type used in the archive list.</typeparam>
/// <typeparam name="TValue">The value type used in the archive list.</typeparam>
public partial class ArchiveList<TKey, TValue> : ArchiveList where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    // Contains all the active snapshots of the archive lists.
    // This is used for determining when resources are no longer in use.
    private readonly WeakList<ArchiveListSnapshot<TKey, TValue>> m_allSnapshots;

    private readonly List<SortedTreeTable<TKey, TValue>> m_filesToDelete;
    private readonly List<SortedTreeTable<TKey, TValue>> m_filesToDispose;

    // Contains the list of all archives.
    private readonly SortedList<Guid, ArchiveTableSummary<TKey, TValue>> m_fileSummaries;

    // The log engine of the ArchiveList. This is where pending deletions or disposals are kept.
    private readonly ArchiveListLog m_listLog;

    // The scheduled task for removing items.
    private readonly ScheduledTask m_processRemovals;

    private readonly ArchiveListSettings m_settings;
    private readonly Lock m_syncRoot;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates an ArchiveList
    /// </summary>
    /// <param name="settings">The settings for the archive list. Null will revert to a default setting.</param>
    public ArchiveList(ArchiveListSettings? settings = null)
    {
        settings ??= new ArchiveListSettings();

        m_settings = settings.CloneReadonly();
        m_settings.Validate();

        m_syncRoot = new Lock();
        m_fileSummaries = new SortedList<Guid, ArchiveTableSummary<TKey, TValue>>();
        m_allSnapshots = [];
        m_listLog = new ArchiveListLog(m_settings.LogSettings);
        m_filesToDelete = [];
        m_filesToDispose = [];
        m_processRemovals = new ScheduledTask(ThreadingMode.DedicatedBackground);
        m_processRemovals.Running += ProcessRemovals_Running;
        m_processRemovals.Disposing += ProcessRemovals_Disposing;
        m_processRemovals.UnhandledException += ProcessRemovals_UnhandledException;

        AttachFileOrPath(m_settings.ImportPaths);

        HashSet<Guid> files = new(m_filesToDelete.Select(x => x.ArchiveId));
        m_listLog.ClearCompletedLogs(files);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the log source base object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!m_disposed && disposing)
        {
            ReleaseClientResources();
            m_processRemovals.Dispose();
            m_listLog.Dispose();

            lock (m_syncRoot)
            {
                foreach (ArchiveTableSummary<TKey, TValue> summary in m_fileSummaries.Values)
                    summary.SortedTreeTable.BaseFile.Dispose();
            }

            m_disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Attaches the supplied paths or files.
    /// </summary>
    /// <param name="paths">An enumerable collection of file paths or directories to attach.</param>
    /// <remarks>
    /// This method will attach files from the specified paths and subdirectories, including files matching specified extensions.
    /// If a specified path is a directory, it will be recursively searched for files with extensions defined in the settings.
    /// </remarks>
    public override void AttachFileOrPath(IEnumerable<string> paths)
    {
        List<string> attachedFiles = [];

        foreach (string path in paths)
        {
            try
            {
                void exceptionHandler(Exception ex)
                {
                    Log.Publish(MessageLevel.Error, "Unknown error occurred while attaching paths", "Path: " + path, null, ex);
                }

                if (File.Exists(path))
                    attachedFiles.Add(path);

                else if (Directory.Exists(path))
                    foreach (string extension in m_settings.ImportExtensions)
                        attachedFiles.AddRange(FilePath.GetFiles(path, "*" + extension, SearchOption.AllDirectories, exceptionHandler));
                else
                    Log.Publish(MessageLevel.Warning, "File or path does not exist", path);
            }
            catch (Exception ex)
            {
                Log.Publish(MessageLevel.Error, "Unknown error occurred while attaching paths", "Path: " + path, null, ex);
            }
        }

        LoadFiles(attachedFiles);
    }

    /// <summary>
    /// Loads the specified files into the archive list.
    /// </summary>
    /// <param name="archiveFiles">The files to be archived.</param>
    public override void LoadFiles(IEnumerable<string> archiveFiles)
    {
        if (m_disposed)
            throw new Exception("Object is disposing");

        List<SortedTreeTable<TKey, TValue>> loadedFiles = [];

        foreach (string file in archiveFiles)
        {
            try
            {
                SortedTreeFile sortedTreeFile = SortedTreeFile.OpenFile(file, true);
                SortedTreeTable<TKey, TValue>? table = sortedTreeFile.OpenTable<TKey, TValue>();

                if (table is null)
                {
                    sortedTreeFile.Dispose();
                    //archiveFile.Delete(); // TODO: Consider the consequences of deleting a file.
                }
                else
                {
                    if (m_listLog.ShouldBeDeleted(table.ArchiveId))
                    {
                        Log.Publish(MessageLevel.Warning, "File being deleted", "The supplied file is being deleted because it was part of a previous rollover that completed but the server crashed before it was properly deleted." + file);
                        table.BaseFile.Delete();
                    }
                    else
                    {
                        loadedFiles.Add(table);
                    }
                }

                Log.Publish(MessageLevel.Info, "Loading Files", "Successfully opened: " + file);
            }
            catch (Exception ex)
            {
                Log.Publish(MessageLevel.Warning, "Loading Files", "Skipping Failed File: " + file, null, ex);
            }
        }

        using ArchiveListEditor<TKey, TValue> edit = AcquireEditLock();

        if (m_disposed)
        {
            loadedFiles.ForEach(table => table.Dispose());
            throw new Exception("Object is disposing");
        }

        foreach (SortedTreeTable<TKey, TValue> file in loadedFiles)
        {
            try
            {
                edit.Add(file);
            }
            catch (Exception ex)
            {
                Log.Publish(MessageLevel.Warning, "Attaching File", "File already attached: " + file.ArchiveId, file.BaseFile.FilePath, ex);
                file.BaseFile.Dispose();
            }
        }
    }

    /// <summary>
    /// Appends the status of the files in the ArchiveList to the provided <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="status">Target status output <see cref="StringBuilder"/>.</param>
    /// <param name="maxFileListing">Maximum file listing.</param>
    public override void GetFullStatus(StringBuilder status, int maxFileListing = -1)
    {
        lock (m_syncRoot)
        {
            status.AppendLine($"Files Pending Deletion: {m_filesToDelete.Count:N0} Disposal: {m_filesToDispose.Count}");

            foreach (SortedTreeTable<TKey, TValue> file in m_filesToDelete)
            {
                status.AppendLine($"Delete - {FilePath.TrimFileName(file.BaseFile.FilePath, 40)}");
                status.AppendLine($"Is Being Used {InternalIsFileBeingUsed(file)}");
            }

            foreach (SortedTreeTable<TKey, TValue> file in m_filesToDispose)
            {
                status.AppendLine($"Dispose - {file.FirstKey} - {file.LastKey}");
                status.AppendLine($"Is Being Used {InternalIsFileBeingUsed(file)}");
            }

            status.AppendLine($"Files In Archive: {m_fileSummaries.Count:N0}{(maxFileListing > -1 ? $" - only showing last {maxFileListing:N0} files." : "")}");

            IEnumerable<ArchiveTableSummary<TKey, TValue>> summaries = maxFileListing == -1 ? m_fileSummaries.Values : m_fileSummaries.Values.Skip(Math.Max(0, m_fileSummaries.Count - maxFileListing));

            foreach (ArchiveTableSummary<TKey, TValue> file in summaries)
                status.AppendLine(file.IsEmpty ? $"Empty File - Name:{Environment.NewLine}        {FilePath.TrimFileName(file.SortedTreeTable.BaseFile.FilePath, 70)}" : $"{file.FirstKey} - {file.LastKey} Name:{Environment.NewLine}        {FilePath.TrimFileName(file.SortedTreeTable.BaseFile.FilePath, 70)}");
        }
    }

    /// <summary>
    /// Gets a complete list of all archive files.
    /// </summary>
    /// <returns>All the attached archive files.</returns>
    public override List<ArchiveDetails> GetAllAttachedFiles()
    {
        List<ArchiveDetails> attachedFiles = [];

        lock (m_syncRoot)
        {
            attachedFiles.AddRange(m_fileSummaries.Values.Select(ArchiveDetails.Create));
            return attachedFiles;
        }
    }

    /// <summary>
    /// Returns an <see cref="IDisposable"/> class that can be used to edit the contents of this list.
    /// WARNING: Make changes quickly and dispose the returned class.  All calls to this class are blocked while
    /// editing this class.
    /// </summary>
    /// <returns>
    /// An <see cref="ArchiveListEditor{TKey, TValue}"/> instance that allows editing the archive list.
    /// </returns>
    /// <remarks>
    /// Use this method to obtain a lock for making changes to the archive list. Ensure to properly dispose of the editor
    /// when done with the modifications to release the lock.
    /// </remarks>
    public new ArchiveListEditor<TKey, TValue> AcquireEditLock()
    {
        return new Editor(this);
    }

    /// <summary>
    /// Determines if the provided file is currently in use
    /// by any resource.
    /// </summary>
    /// <param name="sortedTree">The <see cref="SortedTreeTable{TKey, TValue}"/> to search for.</param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="sortedTree"/> is being used by this object; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks if the provided <paramref name="sortedTree"/> is currently in use by this object.
    /// </remarks>
    public bool IsFileBeingUsed(SortedTreeTable<TKey, TValue> sortedTree)
    {
        lock (m_syncRoot)
            return InternalIsFileBeingUsed(sortedTree);
    }

    /// <summary>
    /// Creates an object that can be used to get updated snapshots from this archive list.
    /// Client must call <see cref="IDisposable.Dispose"/> method when finished with these resources as they will not
    /// automatically be reclaimed by the garbage collector. Class will not be initialized until calling <see cref="ArchiveListSnapshot{TKey,TValue}.UpdateSnapshot"/>.
    /// </summary>
    /// <returns>
    /// A new <see cref="ArchiveListSnapshot{TKey, TValue}"/> instance for client resources.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the object is disposing.
    /// </exception>
    /// <remarks>
    /// This method creates a new <see cref="ArchiveListSnapshot{TKey, TValue}"/> instance for managing client resources.
    /// </remarks>
    public ArchiveListSnapshot<TKey, TValue> CreateNewClientResources()
    {
        ArchiveListSnapshot<TKey, TValue> resources;

        lock (m_syncRoot)
        {
            if (m_disposed)
                throw new Exception("Object is disposing");

            resources = new ArchiveListSnapshot<TKey, TValue>(ReleaseClientResources, UpdateSnapshot);
            m_allSnapshots.Add(resources);
        }

        return resources;
    }

    /// <summary>
    /// Necessary to provide shadow method of archive list.
    /// </summary>
    /// <returns>
    /// An <see cref="ArchiveListEditor{TKey, TValue}"/> instance for making edits to the list.
    /// </returns>
    /// <remarks>
    /// This method is used to acquire an edit lock for the <see cref="ArchiveList{TKey, TValue}"/>.
    /// </remarks>
    protected override ArchiveListEditor InternalAcquireEditLock()
    {
        return AcquireEditLock();
    }

    /// <summary>
    /// Queues the supplied file as a file that needs to be deleted.
    /// MUST be called from a synchronized context.
    /// </summary>
    /// <param name="file">The file to be queued for deletion.</param>
    private void AddFileToDelete(SortedTreeTable<TKey, TValue> file)
    {
        if (file.BaseFile.IsMemoryFile)
        {
            AddFileToDispose(file);
            return;
        }

        try
        {
            if (!InternalIsFileBeingUsed(file))
            {
                file.BaseFile.Delete();
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.SwallowException(ex, "Queueing retry for file deletion...");
        }

        m_listLog.AddFileToDelete(file.ArchiveId);
        m_filesToDelete.Add(file);
        m_processRemovals.Start(1000);
    }

    /// <summary>
    /// Queues the supplied file as one that needs to be disposed when no longer in use.
    /// MUST be called from a synchronized context.
    /// </summary>
    /// <param name="file">The file to be queued for disposal.</param>
    private void AddFileToDispose(SortedTreeTable<TKey, TValue> file)
    {
        if (!InternalIsFileBeingUsed(file))
        {
            file.BaseFile.Dispose();
            return;
        }

        m_filesToDispose.Add(file);
        m_processRemovals.Start(1000);
    }

    /// <summary>
    /// Gets if the specified file is being.
    /// MUST be called from a synchronized context.
    /// </summary>
    /// <param name="sortedTree">The <see cref="SortedTreeTable{TKey, TValue}"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="sortedTree"/> is being used in any active snapshots; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks if the specified <paramref name="sortedTree"/> is being used in any of the active snapshots
    /// managed by the <see cref="ArchiveList{TKey, TValue}"/>.
    /// </remarks>
    private bool InternalIsFileBeingUsed(SortedTreeTable<TKey, TValue> sortedTree)
    {
        return m_allSnapshots.Where(snapshot => !snapshot.IsDisposed).Select(snapshot => snapshot.Tables).Where(tables => tables is not null).Any(tables => tables!.Any(summary => summary is not null && summary.SortedTreeTable == sortedTree));
    }

    private void ProcessRemovals_Running(object? sender, EventArgs<ScheduledTaskRunningReason> eventArgs)
    {
        bool fileDeleted = false;

        lock (m_syncRoot)
        {
            for (int x = m_filesToDelete.Count - 1; x >= 0; x--)
            {
                SortedTreeTable<TKey, TValue> file = m_filesToDelete[x];

                if (!InternalIsFileBeingUsed(file))
                {
                    fileDeleted = true;
                    file.BaseFile.Delete();
                    m_filesToDelete.RemoveAt(x);
                }
            }

            for (int x = m_filesToDispose.Count - 1; x >= 0; x--)
            {
                SortedTreeTable<TKey, TValue> file = m_filesToDispose[x];

                if (InternalIsFileBeingUsed(file))
                    continue;
                
                file.BaseFile.Dispose();
                m_filesToDispose.RemoveAt(x);
            }

            if (fileDeleted)
            {
                HashSet<Guid> files = new(m_filesToDelete.Select(x => x.ArchiveId));
                m_listLog.ClearCompletedLogs(files);
            }

            if (m_filesToDelete.Count > 0 || m_filesToDispose.Count > 0)
                m_processRemovals.Start(1000);
        }
    }

    private void ProcessRemovals_Disposing(object? sender, EventArgs eventArgs)
    {
        lock (m_syncRoot)
        {
            // TODO: Kick all clients.
            m_filesToDelete.ForEach(x => x.BaseFile.Delete());
            m_filesToDelete.Clear();

            m_filesToDispose.ForEach(x => x.BaseFile.Dispose());
            m_filesToDispose.Clear();
        }
    }

    private static void ProcessRemovals_UnhandledException(object? sender, EventArgs<Exception> e)
    {
        LibraryEvents.OnSuppressedException(sender, e.Argument);
    }

    private void ReleaseClientResources()
    {
        List<ArchiveListSnapshot<TKey, TValue>> tablesInUse = [];

        lock (m_syncRoot)
            tablesInUse.AddRange(m_allSnapshots);

        tablesInUse.ForEach(snapshot => snapshot.Engine_BeginDropConnection());
        tablesInUse.ForEach(snapshot => snapshot.Engine_EndDropConnection());
    }

    /// <summary>
    /// Invoked by <see cref="ArchiveListSnapshot{TKey,TValue}.Dispose"/> method.
    /// </summary>
    /// <param name="archiveLists"></param>
    private void ReleaseClientResources(ArchiveListSnapshot<TKey, TValue> archiveLists)
    {
        lock (m_syncRoot)
            m_allSnapshots.Remove(archiveLists);
    }

    /// <summary>
    /// Invoked by <see cref="ArchiveListSnapshot{TKey,TValue}.UpdateSnapshot"/>.
    /// </summary>
    /// <param name="transaction"></param>
    private void UpdateSnapshot(ArchiveListSnapshot<TKey, TValue> transaction)
    {
        lock (m_syncRoot)
        {
            transaction.Tables = new ArchiveTableSummary<TKey, TValue>[m_fileSummaries.Count];
            m_fileSummaries.Values.CopyTo(transaction.Tables!, 0);
        }
    }

    #endregion
}