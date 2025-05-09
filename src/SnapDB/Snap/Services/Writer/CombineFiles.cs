﻿//******************************************************************************************************
//  CombineFiles`2.cs - Gbtc
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
//  02/16/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone;
using Gemstone.Diagnostics;
using Gemstone.Threading;
using SnapDB.Snap.Services.Reader;
using SnapDB.Snap.Storage;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Represents a series of stages that an archive file progresses through
/// in order to properly condition the data.
/// </summary>
/// <typeparam name="TKey">The key type used in the sorted tree table.</typeparam>
/// <typeparam name="TValue">The value type used in the sorted tree table.</typeparam>
public class CombineFiles<TKey, TValue> : DisposableLoggingClassBase where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly ArchiveList<TKey, TValue> m_archiveList;

    private readonly SimplifiedArchiveInitializer<TKey, TValue> m_createNextStageFile;
    private readonly ManualResetEvent m_rolloverComplete;
    private readonly RolloverLog m_rolloverLog;
    private ScheduledTask m_rolloverTask;
    private readonly CombineFilesSettings m_settings;
    private readonly Lock m_syncRoot;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a stage writer.
    /// </summary>
    /// <param name="settings">the settings for this stage</param>
    /// <param name="archiveList">the archive list</param>
    /// <param name="rolloverLog">the rollover log</param>
    public CombineFiles(CombineFilesSettings settings, ArchiveList<TKey, TValue> archiveList, RolloverLog rolloverLog) : base(MessageClass.Framework)
    {
        m_settings = settings.CloneReadonly();
        m_settings.Validate();
        m_archiveList = archiveList;
        m_createNextStageFile = new SimplifiedArchiveInitializer<TKey, TValue>(settings.ArchiveSettings);
        m_rolloverLog = rolloverLog;
        m_rolloverComplete = new ManualResetEvent(false);
        m_syncRoot = new Lock();
        m_rolloverTask = new ScheduledTask(ThreadingMode.DedicatedForeground, ThreadPriority.BelowNormal);
        m_rolloverTask.Running += OnExecute;
        m_rolloverTask.UnhandledException += OnException;
        m_rolloverTask.Start(m_settings.ExecuteTimer);
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the log source base object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            m_disposed = true;

            if (disposing)
            {
                m_rolloverTask?.Dispose();
                m_rolloverTask = null;
            }
        }

        base.Dispose(disposing);
    }

    private void OnExecute(object? sender, EventArgs<ScheduledTaskRunningReason> e)
    {
        // The worker can be disposed either via the Stop() method or 
        // the Dispose() method.  If via the dispose method, then
        // don't do any cleanup.
        if (m_disposed && e.Argument == ScheduledTaskRunningReason.Disposing)
            return;

        // go ahead and schedule the next rollover since nothing
        // will happen until this function exits anyway.
        // if the task is disposing, the following line does nothing.
        m_rolloverTask.Start(m_settings.ExecuteTimer);

        lock (m_syncRoot)
        {
            if (m_disposed)
                return;

            using (ArchiveListSnapshot<TKey, TValue> resource = m_archiveList.CreateNewClientResources())
            {
                resource.UpdateSnapshot();

                List<ArchiveTableSummary<TKey, TValue>> list = [];
                List<Guid> listIds = [];

                for (int x = 0; x < resource.Tables!.Length; x++)
                {
                    ArchiveTableSummary<TKey, TValue>? table = resource.Tables[x];

                    if (table.SortedTreeTable.BaseFile.Snapshot.Header.Flags.Contains(m_settings.MatchFlag) && table.SortedTreeTable.BaseFile.Snapshot.Header.Flags.Contains(FileFlags.IntermediateFile))
                    {
                        list.Add(table);
                        listIds.Add(table.FileId);
                    }
                    else
                    {
                        resource.Tables[x] = null;
                    }
                }

                bool shouldRollover = list.Count >= m_settings.CombineOnFileCount;

                long size = 0;

                for (int x = 0; x < list.Count; x++)
                {
                    size += list[x].SortedTreeTable.BaseFile.ArchiveSize;

                    if (size > m_settings.CombineOnFileSize)
                    {
                        if (x != list.Count - 1) //If not the last entry
                            list.RemoveRange(x + 1, list.Count - x - 1);
                        break;
                    }
                }

                if (size > m_settings.CombineOnFileSize)
                    shouldRollover = true;

                if (shouldRollover)
                {
                    TKey startKey = new();
                    TKey endKey = new();
                    startKey.SetMax();
                    endKey.SetMin();

                    foreach (Guid fileId in listIds)
                    {
                        ArchiveTableSummary<TKey, TValue> table = resource.TryGetFile(fileId) ?? throw new Exception("File not found");
                        
                        if (!table.IsEmpty)
                        {
                            if (startKey.IsGreaterThan(table.FirstKey))
                                table.FirstKey.CopyTo(startKey);
                            if (endKey.IsLessThan(table.LastKey))
                                table.LastKey.CopyTo(endKey);
                        }
                    }

                    RolloverLogFile? logFile = null;

                    void createLog(Guid x)
                    {
                        logFile = m_rolloverLog.Create(listIds, x);
                    }

                    using (UnionReader<TKey, TValue> reader = new(list))
                    {
                        SortedTreeTable<TKey, TValue> dest = m_createNextStageFile.CreateArchiveFile(startKey, endKey, size, reader, createLog);

                        resource.Dispose();

                        using (ArchiveListEditor<TKey, TValue> edit = m_archiveList.AcquireEditLock())
                        {
                            // Add the newly created file.
                            edit.Add(dest);

                            foreach (ArchiveTableSummary<TKey, TValue> table in list)
                                edit.TryRemoveAndDelete(table.FileId);
                        }
                    }

                    logFile?.Delete();
                }

                resource.Dispose();
            }

            m_rolloverComplete.Set();
        }
    }

    private static void OnException(object? sender, EventArgs<Exception> e)
    {
        LibraryEvents.OnSuppressedException(sender, e.Argument);
    }

    #endregion
}