﻿//******************************************************************************************************
//  WriteProcessor`2.cs - Gbtc
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
//  01/19/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using Gemstone.Diagnostics;

namespace SnapDB.Snap.Services.Writer;

/// <summary>
/// Houses all of the write operations for the historian.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value associated with the key.</typeparam>
public class WriteProcessor<TKey, TValue> : DisposableLoggingClassBase where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    private readonly FirstStageWriter<TKey, TValue> m_firstStageWriter;
    private readonly bool m_isMemoryOnly;
    private readonly PrebufferWriter<TKey, TValue> m_prebuffer;
    private readonly WriteProcessorSettings m_settings;
    private readonly List<CombineFiles<TKey, TValue>> m_stagingRollovers;
    private readonly TransactionTracker<TKey, TValue> m_transactionTracker;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="WriteProcessor{TKey,TValue}"/>.
    /// </summary>
    /// <param name="list">the master list of archive files</param>
    /// <param name="settings">the settings</param>
    /// <param name="rolloverLog">the rollover log value</param>
    public WriteProcessor(ArchiveList<TKey, TValue> list, WriteProcessorSettings settings, RolloverLog rolloverLog) : base(MessageClass.Framework)
    {
        m_settings = settings.CloneReadonly();
        m_settings.Validate();

        m_stagingRollovers = [];
        m_firstStageWriter = new FirstStageWriter<TKey, TValue>(settings.FirstStageWriter, list);
        m_isMemoryOnly = false;
        m_prebuffer = new PrebufferWriter<TKey, TValue>(settings.PrebufferWriter, m_firstStageWriter.AppendData);
        m_transactionTracker = new TransactionTracker<TKey, TValue>(m_prebuffer, m_firstStageWriter);

        foreach (CombineFilesSettings rollover in settings.StagingRollovers)
            m_stagingRollovers.Add(new CombineFiles<TKey, TValue>(rollover, list, rolloverLog));
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="WriteProcessor{TKey,TValue}"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        try
        {
            // This will be done regardless of whether the object is finalized or disposed.
            if (!disposing)
                return;
            
            // This will be done only when the object is disposed by calling Dispose().
            m_prebuffer.Stop();
            m_firstStageWriter.Stop();
            m_stagingRollovers.ForEach(x => x.Dispose());
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
            base.Dispose(disposing); // Call base class Dispose().
        }
    }

    /// <summary>
    /// Writes the provided key-value pair to the engine.
    /// </summary>
    /// <param name="key">The key to be written to the engine.</param>
    /// <param name="value">The value associated with the key to be written to the engine.</param>
    /// <returns>the transaction code so this write can be tracked.</returns>
    public long Write(TKey key, TValue value)
    {
        return m_prebuffer.Write(key, value);
    }

    /// <summary>
    /// Writes the provided stream to the engine.
    /// </summary>
    /// <param name="stream">The stream to be written to the engine.</param>
    /// <returns>The transaction code so this write can be tracked.</returns>
    public long Write(TreeStream<TKey, TValue> stream)
    {
        long sequenceId = -1;
        TKey key = new();
        TValue value = new();
        while (stream.Read(key, value))
            sequenceId = m_prebuffer.Write(key, value);
        return sequenceId;
    }

    /// <summary>
    /// Blocks until the specified point has progressed beyond the pre-stage level and can be queried by the user.
    /// </summary>
    /// <param name="transactionId">The sequence number representing the desired point that was committed.</param>
    public void SoftCommit(long transactionId)
    {
        m_transactionTracker.WaitForSoftCommit(transactionId);
    }

    /// <summary>
    /// Blocks until the specified point has been committed to the disk subsystem. If running in a In-Memory mode, will return
    /// as soon as it has been moved beyond the pre-stage level and can be queried by the user.
    /// </summary>
    /// <param name="transactionId">The sequence number representing the desired point that was committed.</param>
    public void HardCommit(long transactionId)
    {
        if (m_isMemoryOnly)
            SoftCommit(transactionId);
        else
            m_transactionTracker.WaitForHardCommit(transactionId);
    }

    #endregion
}