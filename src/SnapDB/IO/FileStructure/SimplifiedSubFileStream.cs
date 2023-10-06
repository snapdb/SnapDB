//******************************************************************************************************
//  SimplifiedSubFileStream.cs - Gbtc
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
//  10/16/2014 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Provides a file stream that can be used to open a file and does all of the background work
/// required to translate virtual position data into physical ones.
/// </summary>
internal sealed class SimplifiedSubFileStream : ISupportsBinaryStream
{
    #region [ Members ]

    private readonly FileHeaderBlock m_fileHeaderBlock;

    private BinaryStreamIoSessionBase? m_ioStream1;

    private readonly FileStream m_stream;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates an SimplifiedSubFileStream
    /// </summary>
    /// <param name="stream">The location to read from.</param>
    /// <param name="subFile">The file to read.</param>
    /// <param name="fileHeaderBlock">The FileAllocationTable.</param>
    internal SimplifiedSubFileStream(FileStream stream, SubFileHeader? subFile, FileHeaderBlock fileHeaderBlock)
    {
        if (subFile is null)
            throw new ArgumentNullException(nameof(subFile));

        if (fileHeaderBlock is null)
            throw new ArgumentNullException(nameof(fileHeaderBlock));

        if (subFile.DirectBlock == 0)
            throw new Exception("Must assign subFile.DirectBlock");

        if (fileHeaderBlock.IsReadOnly)
            throw new ArgumentException("This parameter cannot be read only when opening for writing", nameof(fileHeaderBlock));

        if (subFile.IsReadOnly)
            throw new ArgumentException("This parameter cannot be read only when opening for writing", nameof(subFile));

        m_stream = stream ?? throw new ArgumentNullException(nameof(stream));
        SubFile = subFile;
        m_fileHeaderBlock = fileHeaderBlock;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Determines if the file system has been disposed yet.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets if this file was opened in "read only" mode.
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return false;
        }
    }

    /// <summary>
    /// Gets the number of available simultaneous read or write sessions.
    /// </summary>
    /// <remarks>
    /// This value is used to determine if a binary stream can be cloned
    /// to improve read, write, or copy performance.
    /// </remarks>
    public int RemainingSupportedIoSessions
    {
        get
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            int count = 0;

            if (m_ioStream1 is not null && !m_ioStream1.IsDisposed)
                return count;

            m_ioStream1 = null;
            return ++count;
        }
    }

    /// <summary>
    /// Gets the file used by the stream.
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
            if (m_ioStream1 is null)
                return;

            m_ioStream1.Dispose();
            m_ioStream1 = null;
        }
        finally
        {
            IsDisposed = true; // Prevent duplicate dispose.
        }
    }


    // Acquire an IO Session.
    BinaryStreamIoSessionBase ISupportsBinaryStream.CreateIoSession()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (RemainingSupportedIoSessions == 0)
            throw new Exception("There are not any remaining IO Sessions");

        m_ioStream1 = new SimplifiedSubFileStreamIoSession(m_stream, SubFile, m_fileHeaderBlock);

        return m_ioStream1;
    }

    #endregion
}