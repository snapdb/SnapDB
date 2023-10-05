//******************************************************************************************************
//  SimplifiedFileWriter.cs - Gbtc
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
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

using Gemstone.Diagnostics;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Storage;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Assists in the writing of a simplified file. This file can only be appended to
/// and it must be sequentially written.
/// </summary>
public class SimplifiedFileWriter : IDisposable
{
    #region [ Members ]

    /// <summary>
    /// Defines the file stream for this <see cref="SimplifiedFileWriter"/>.
    /// </summary>
    public FileStream Stream;

    private readonly string m_completeFileName;

    private readonly FileHeaderBlock m_fileHeaderBlock;

    private readonly string m_pendingFileName;

    private SimplifiedSubFileStream? m_subFileStream;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a simplified file writer.
    /// </summary>
    /// <param name="pendingFileName"></param>
    /// <param name="completeFileName"></param>
    /// <param name="blockSize"></param>
    /// <param name="flags"></param>
    public SimplifiedFileWriter(string pendingFileName, string completeFileName, int blockSize, params Guid[] flags)
    {
        m_pendingFileName = pendingFileName;
        m_completeFileName = completeFileName;
        m_fileHeaderBlock = FileHeaderBlock.CreateNewSimplified(blockSize, flags).CloneEditable();
        m_fileHeaderBlock.ArchiveType = SortedTreeFile.FileType;
        Stream = new FileStream(pendingFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    }

#if DEBUG
    /// <summary>
    /// Finalizes an instance of the <see cref="SimplifiedFileWriter"/> class.
    /// </summary>
    /// <remarks>
    /// This finalizer is automatically called by the garbage collector during object cleanup.
    /// It publishes an informational log message indicating that the finalizer has been called.
    /// </remarks>
    ~SimplifiedFileWriter()
    {
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }
#endif

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the GUID number for this <see cref="SimplifiedFileWriter"/>.
    /// </summary>
    public Guid ArchiveId => m_fileHeaderBlock.ArchiveId;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="SimplifiedFileWriter"/> object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <returns>The <see cref="ISupportsBinaryStream"/> representing the newly created file.</returns>
    public ISupportsBinaryStream CreateFile(SubFileName fileName)
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        CloseCurrentFile();

        SubFileHeader subFile = m_fileHeaderBlock.CreateNewFile(fileName);
        subFile.DirectBlock = m_fileHeaderBlock.LastAllocatedBlock + 1;
        m_subFileStream = new SimplifiedSubFileStream(Stream, subFile, m_fileHeaderBlock);

        return m_subFileStream;
    }

    /// <summary>
    /// Commits the changes to the disk.
    /// </summary>
    public void Commit()
    {
        if (m_disposed)
            return;

        CloseCurrentFile();

        Stream.Position = 0;
        Stream.Write(m_fileHeaderBlock.GetBytes());
        Stream.Flush(true);
        Stream.Dispose();
        Stream = null!;

        File.Move(m_pendingFileName, m_completeFileName);
        m_disposed = true;

        Dispose();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SimplifiedFileWriter"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        try
        {
            if (!disposing)
                return;

            if (m_subFileStream is not null)
            {
                m_subFileStream.Dispose();
                m_subFileStream = null;
            }

            Stream?.Dispose();
            File.Delete(m_pendingFileName);
        }
        finally
        {
            m_disposed = true; // Prevent duplicate dispose.
        }
    }

    private void CloseCurrentFile()
    {
        if (m_subFileStream is null)
            return;

        if (!m_subFileStream.IsDisposed)
            throw new Exception("The previous file must be disposed before completing this action");

        m_subFileStream = null;
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(SimplifiedFileWriter), MessageClass.Component);

    #endregion
}