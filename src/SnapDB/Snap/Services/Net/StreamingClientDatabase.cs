//******************************************************************************************************
//  StreamingClientDatabase`2.cs - Gbtc
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
//  12/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.Snap.Filters;
using SnapDB.Snap.Services.Reader;
using SnapDB.Snap.Streaming;

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// A socket based client that extends connecting to a database.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class StreamingClientDatabase<TKey, TValue> : ClientDatabaseBase<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    /// <summary>
    /// Handles bulk writing to a streaming interface.
    /// </summary>
    public class BulkWriting : IDisposable
    {
        #region [ Members ]

        private readonly StreamingClientDatabase<TKey, TValue> m_client;
        private readonly StreamEncodingBase<TKey, TValue> m_encodingMode;
        private readonly RemoteBinaryStream m_stream;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        internal BulkWriting(StreamingClientDatabase<TKey, TValue> client)
        {
            if (client.m_writer is not null)
                throw new Exception("Duplicate call to StartBulkWriting");

            m_client = client;
            m_client.m_writer = this;
            m_stream = m_client.m_stream;
            m_encodingMode = m_client.m_encodingMode;

            m_stream.Write((byte)ServerCommand.Write);
            m_encodingMode.ResetEncoder();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_client.m_writer = null;
            m_disposed = true;

            m_encodingMode.WriteEndOfStream(m_stream);
            m_stream.Flush();
        }

        /// <summary>
        /// Writes to the encoded stream.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Write(TKey key, TValue value)
        {
            m_encodingMode.Encode(m_stream, key, value);
        }

        #endregion
    }

    private class PointReader : TreeStream<TKey, TValue>
    {
        #region [ Members ]

        private bool m_completed;

        private readonly StreamEncodingBase<TKey, TValue> m_encodingMethod;
        private readonly Action m_onComplete;
        private readonly RemoteBinaryStream m_stream;

        #endregion

        #region [ Constructors ]

        public PointReader(StreamEncodingBase<TKey, TValue> encodingMethod, RemoteBinaryStream stream, Action onComplete)
        {
            m_onComplete = onComplete;
            m_encodingMethod = encodingMethod;
            m_stream = stream;
            encodingMethod.ResetEncoder();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Cancels the read operation.
        /// </summary>
        public void Cancel()
        {
            // TODO: Actually cancel the stream.
            TKey key = new();
            TValue value = new();

            if (m_completed)
                return;

            // Flush the rest of the data off of the receive queue.
            while (m_encodingMethod.TryDecode(m_stream, key, value))
            {
                // CurrentKey.ReadCompressed(m_client.m_stream, CurrentKey);
                // CurrentValue.ReadCompressed(m_client.m_stream, CurrentValue);
            }

            Complete();
        }

        /// <summary>
        /// Advances the stream to the next value.
        /// If before the beginning of the stream, advances to the first value
        /// </summary>
        /// <returns>True if the advance was successful. False if the end of the stream was reached.</returns>
        protected override bool ReadNext(TKey key, TValue value)
        {
            if (!m_completed && m_encodingMethod.TryDecode(m_stream, key, value))
                return true;

            Complete();
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Cancel();

            base.Dispose(disposing);
        }

        private void Complete()
        {
            if (!m_completed)
            {
                m_completed = true;
                m_onComplete();
                string exception;
                ServerResponse command = (ServerResponse)m_stream.ReadUInt8();
                switch (command)
                {
                    case ServerResponse.UnhandledException:
                        exception = m_stream.ReadString();
                        throw new Exception("Server UnhandledException: \n" + exception);
                    case ServerResponse.ErrorWhileReading:
                        exception = m_stream.ReadString();
                        throw new Exception("Server Error While Reading: \n" + exception);

                    case ServerResponse.CanceledRead:
                        break;
                    case ServerResponse.ReadComplete:
                        break;
                    default:
                        throw new Exception("Unknown server response: " + command);
                }
            }
        }

        #endregion
    }

    private StreamEncodingBase<TKey, TValue> m_encodingMode = default!;

    private readonly Action m_onDispose;
    private PointReader? m_reader;
    private readonly RemoteBinaryStream m_stream;
    private readonly TKey m_tmpKey;
    private readonly TValue m_tmpValue;
    private BulkWriting? m_writer;
    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a streaming wrapper around a database.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="onDispose"></param>
    /// <param name="info"></param>
    public StreamingClientDatabase(RemoteBinaryStream stream, Action onDispose, DatabaseInfo info)
    {
        Info = info;
        m_tmpKey = new TKey();
        m_tmpValue = new TValue();
        m_onDispose = onDispose;
        m_stream = stream;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets if has been disposed.
    /// </summary>
    public override bool IsDisposed => m_disposed;

    /// <summary>
    /// Gets basic information about the current Database.
    /// </summary>
    public override DatabaseInfo Info { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Defines the encoding method to use for the server.
    /// </summary>
    /// <param name="encoding"></param>
    public void SetEncodingMode(EncodingDefinition encoding)
    {
        m_encodingMode = Library.CreateStreamEncoding<TKey, TValue>(encoding);
        m_stream.Write((byte)ServerCommand.SetEncodingMethod);
        encoding.Save(m_stream);
        m_stream.Flush();

        ServerResponse command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception("Server UnhandledException: \n" + exception);
            case ServerResponse.UnknownEncodingMethod:
                throw new Exception("Server does not recognize encoding method");
            case ServerResponse.EncodingMethodAccepted:
                break;
            default:
                throw new Exception("Unknown server response: " + command);
        }
    }


    /// <summary>
    /// Reads data from the SortedTreeEngine with the provided read options and server side filters.
    /// </summary>
    /// <param name="readerOptions">read options supplied to the reader. Can be null.</param>
    /// <param name="keySeekFilter">a seek based filter to follow. Can be null.</param>
    /// <param name="keyMatchFilter">a match based filer to follow. Can be null.</param>
    /// <returns>A stream that will read the specified data.</returns>
    public override TreeStream<TKey, TValue> Read(SortedTreeEngineReaderOptions? readerOptions, SeekFilterBase<TKey>? keySeekFilter, MatchFilterBase<TKey, TValue>? keyMatchFilter)
    {
        if (m_reader is not null)
            throw new Exception("Sockets do not support concurrent readers. Dispose of old reader.");

        m_stream.Write((byte)ServerCommand.Read);

        if (keySeekFilter is null)
        {
            m_stream.Write(false);
        }
        else
        {
            m_stream.Write(true);
            m_stream.Write(keySeekFilter.FilterType);
            keySeekFilter.Save(m_stream);
        }

        if (keyMatchFilter is null)
        {
            m_stream.Write(false);
        }
        else
        {
            m_stream.Write(true);
            m_stream.Write(keyMatchFilter.FilterType);
            keyMatchFilter.Save(m_stream);
        }

        if (readerOptions is null)
        {
            m_stream.Write(false);
        }
        else
        {
            m_stream.Write(true);
            readerOptions.Save(m_stream);
        }

        m_stream.Flush();

        ServerResponse command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception("Server UnhandledException: \n" + exception);
            case ServerResponse.UnknownOrCorruptSeekFilter:
                throw new Exception("Server does not recognize the seek filter");
            case ServerResponse.UnknownOrCorruptMatchFilter:
                throw new Exception("Server does not recognize the match filter");
            case ServerResponse.UnknownOrCorruptReaderOptions:
                throw new Exception("Server does not recognize the reader options");
            case ServerResponse.SerializingPoints:
                break;
            case ServerResponse.ErrorWhileReading:
                exception = m_stream.ReadString();
                throw new Exception("Server Error While Reading: \n" + exception);
            default:
                throw new Exception("Unknown server response: " + command);
        }

        m_reader = new PointReader(m_encodingMode, m_stream, () => m_reader = null);

        return m_reader;
    }


    /// <summary>
    /// Writes the tree stream to the database.
    /// </summary>
    /// <param name="stream">all of the key/value pairs to add to the database.</param>
    public override void Write(TreeStream<TKey, TValue> stream)
    {
        if (m_reader is not null)
            throw new Exception("Sockets do not support writing while a reader is open. Dispose of reader.");

        m_stream.Write((byte)ServerCommand.Write);
        m_encodingMode.ResetEncoder();
        while (stream.Read(m_tmpKey, m_tmpValue))
            m_encodingMode.Encode(m_stream, m_tmpKey, m_tmpValue);
        m_encodingMode.WriteEndOfStream(m_stream);
        m_stream.Flush();
    }

    /// <summary>
    /// Writes an individual key/value to the sorted tree store.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public override void Write(TKey key, TValue value)
    {
        if (m_reader is not null)
            throw new Exception("Sockets do not support writing while a reader is open. Dispose of reader.");

        m_stream.Write((byte)ServerCommand.Write);
        m_encodingMode.ResetEncoder();
        m_encodingMode.Encode(m_stream, key, value);
        m_encodingMode.WriteEndOfStream(m_stream);
        m_stream.Flush();
    }

    /// <summary>
    /// Due to the blocking nature of streams, this helper class can substantially
    /// improve the performance of writing streaming points to the historian.
    /// </summary>
    /// <returns></returns>
    public BulkWriting StartBulkWriting()
    {
        return new BulkWriting(this);
    }

    /// <summary>
    /// Loads the provided files from all of the specified paths.
    /// </summary>
    /// <param name="paths">all of the paths of archive files to attach. These can either be a path, or an individual file name.</param>
    public override void AttachFilesOrPaths(IEnumerable<string> paths)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Enumerates all of the files attached to the database.
    /// </summary>
    public override List<ArchiveDetails> GetAllAttachedFiles()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Detaches the list of files from the database.
    /// </summary>
    /// <param name="files">The file IDs that need to be detached.</param>
    public override void DetachFiles(List<Guid> files)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes the list of files from the database.
    /// </summary>
    /// <param name="files">The files that need to be deleted.</param>
    public override void DeleteFiles(List<Guid> files)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Forces a soft commit on the database. A soft commit
    /// only commits data to memory. This allows other clients to read the data.
    /// While soft committed, this data could be lost during an unexpected shutdown.
    /// Soft commits usually occur within microseconds.
    /// </summary>
    public override void SoftCommit()
    {
        //throw new NotImplementedException();
    }

    /// <summary>
    /// Forces a commit to the disk subsystem. Once this returns, the data will not
    /// be lost due to an application crash or unexpected shutdown.
    /// Hard commits can take 100ms or longer depending on how much data has to be committed.
    /// This requires two consecutive hardware cache flushes.
    /// </summary>
    public override void HardCommit()
    {
        //throw new NotImplementedException();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public override void Dispose()
    {
        if (m_disposed)
            return;

        m_disposed = true;

        m_reader?.Dispose();

        m_stream.Write((byte)ServerCommand.DisconnectDatabase);
        m_stream.Flush();
        m_onDispose();

        ServerResponse command = (ServerResponse)m_stream.ReadUInt8();

        switch (command)
        {
            case ServerResponse.UnhandledException:
                string exception = m_stream.ReadString();
                throw new Exception("Server UnhandledException: \n" + exception);
            case ServerResponse.DatabaseDisconnected:
                break;
            default:
                throw new Exception("Unknown server response: " + command);
        }
    }

    #endregion
}