﻿//******************************************************************************************************
//  SubFileStream.cs - Gbtc
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
//  12/10/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  06/01/2012 - Steven E. Chisholm
//       Removed the inheritance from System.IO.Stream since it wasn't used.
//       And prevented concurrent access to this class
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Provides a file stream that can be used to open a file and does all of the background work
/// required to translate virtual position data into physical ones.
/// </summary>
public sealed partial class SubFileStream : ISupportsBinaryStream
{
    #region [ Members ]

    private readonly int m_blockSize;

    private readonly DiskIo m_dataReader;

    private readonly FileHeaderBlock m_fileHeaderBlock;

    private BinaryStreamIoSessionBase? m_ioStream1;

    private BinaryStreamIoSessionBase? m_ioStream2;

    private readonly bool m_isReadOnly;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates an SubFileStream.
    /// </summary>
    /// <param name="dataReader">The location to read from.</param>
    /// <param name="subFile">The file to read.</param>
    /// <param name="fileHeaderBlock">The FileAllocationTable.</param>
    /// <param name="isReadOnly">Determines if the stream allows editing.</param>
    internal SubFileStream(DiskIo dataReader, SubFileHeader? subFile, FileHeaderBlock fileHeaderBlock, bool isReadOnly)
    {
        if (dataReader is null)
            throw new ArgumentNullException(nameof(dataReader));

        if (subFile is null)
            throw new ArgumentNullException(nameof(subFile));

        if (fileHeaderBlock is null)
            throw new ArgumentNullException(nameof(fileHeaderBlock));

        if (!isReadOnly)
        {
            if (dataReader.IsReadOnly)
                throw new ArgumentException("This parameter cannot be read only when opening for writing", nameof(dataReader));

            if (fileHeaderBlock.IsReadOnly)
                throw new ArgumentException("This parameter cannot be read only when opening for writing", nameof(fileHeaderBlock));

            if (subFile.IsReadOnly)
                throw new ArgumentException("This parameter cannot be read only when opening for writing", nameof(subFile));
        }

        if (isReadOnly)
        {
            if (!fileHeaderBlock.IsReadOnly)
                throw new ArgumentException("This parameter must be read only when opening for reading", nameof(fileHeaderBlock));

            if (!subFile.IsReadOnly)
                throw new ArgumentException("This parameter must be read only when opening for reading", nameof(subFile));
        }

        m_blockSize = dataReader.BlockSize;
        m_dataReader = dataReader;
        SubFile = subFile;
        m_fileHeaderBlock = fileHeaderBlock;
        m_isReadOnly = isReadOnly;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Determines if the file system has been disposed yet.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets if this file was opened in readonly mode.
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return m_isReadOnly;
        }
    }

    /// <summary>
    /// Gets the number of available simultaneous read/write sessions.
    /// </summary>
    /// <remarks>
    /// This value is used to determine if a binary stream can be cloned to improve read, write, and copy performance.
    /// </remarks>
    public int RemainingSupportedIoSessions
    {
        get
        {
            int count = 0;

            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (m_ioStream1 is null || m_ioStream1.IsDisposed)
                count++;

            if (m_ioStream2 is null || m_ioStream2.IsDisposed)
                count++;

            return count;
        }
    }

    /// <summary>
    /// The file used by the stream.
    /// </summary>
    internal SubFileHeader? SubFile { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        try
        {
            if (m_ioStream1 is not null)
            {
                m_ioStream1.Dispose();
                m_ioStream1 = null;
            }

            if (m_ioStream2 is not null)
            {
                m_ioStream2.Dispose();
                m_ioStream2 = null;
            }
        }
        finally
        {
            IsDisposed = true; // Prevent duplicate dispose.
        }
    }

    private void ClearIndexNodeCache(IoSession caller, IndexParser mostRecentParser)
    {
        if (m_fileHeaderBlock.IsSimplifiedFileFormat)
            return;

        if (m_ioStream1 is not null && !m_ioStream1.IsDisposed && m_ioStream1 != caller)
            ((IoSession)m_ioStream1).ClearIndexCache(mostRecentParser);

        if (m_ioStream2 is not null && !m_ioStream2.IsDisposed && m_ioStream2 != caller)
            ((IoSession)m_ioStream2).ClearIndexCache(mostRecentParser);
    }


    /// <summary>
    /// Acquire an IO Session.
    /// </summary>
    BinaryStreamIoSessionBase ISupportsBinaryStream.CreateIoSession()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (RemainingSupportedIoSessions == 0)
            throw new Exception("There are not any remaining IO Sessions");

        BinaryStreamIoSessionBase session;

        if (m_fileHeaderBlock.IsSimplifiedFileFormat)
            session = new SimplifiedIoSession(this);
        else
            session = new IoSession(this);

        if (m_ioStream1 is null || m_ioStream1.IsDisposed)
            m_ioStream1 = session;
        else
            m_ioStream2 = session;

        return session;
    }

    #endregion
}