//******************************************************************************************************
//  MemoryFile.cs - Gbtc
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
//  02/01/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Provides a in memory stream that uses pages that are pooled in the unmanaged buffer pool.
/// </summary>
internal partial class MemoryPoolFile
    : MemoryPoolStreamCore, IDiskMediumCoreFunctions
{
    #region [ Members ]

    // A Reusable I/O session for all BinaryStreams.
    private readonly IoSession m_ioSession;

    private bool m_isReadOnly;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Create a new <see cref="MemoryPoolFile"/>.
    /// </summary>
    public MemoryPoolFile(MemoryPool pool)
        : base(pool)
    {
        m_ioSession = new IoSession(this);
        m_isReadOnly = false;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates an <see cref="BinaryStreamIoSessionBase"/> for the current <see cref="MemoryStream"/>.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="BinaryStreamIoSessionBase"/> for the current <see cref="MemoryStream"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown if the <see cref="MemoryStream"/> has been disposed and cannot create a new session.</exception>
    /// <remarks>
    /// This method creates and returns a new <see cref="BinaryStreamIoSessionBase"/> instance associated with the current
    /// <see cref="MemoryStream"/>. If the <see cref="MemoryStream"/> has been disposed (IsDisposed is <c>true</c>), it throws an
    /// <see cref="ObjectDisposedException"/> indicating that the stream has been disposed and cannot create a new session.
    /// </remarks>
    public BinaryStreamIoSessionBase CreateIoSession()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("MemoryStream");

        return m_ioSession;
    }

    public string FileName => string.Empty;

    /// <summary>
    /// Commits changes made to the <see cref="MemoryStream"/> to a <see cref="FileHeaderBlock"/>.
    /// </summary>
    /// <param name="headerBlock">The <see cref="FileHeaderBlock"/> to which the changes are committed.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the <see cref="MemoryStream"/> has been disposed and cannot commit changes.
    /// </exception>
    /// <remarks>
    /// This method commits any changes made to the current <see cref="MemoryStream"/> to the specified
    /// <see cref="FileHeaderBlock"/>. If the <see cref="MemoryStream"/> has been disposed (IsDisposed is true),
    /// it throws an <see cref="ObjectDisposedException"/> indicating that the stream has been disposed and cannot commit changes.
    /// </remarks>
    public void CommitChanges(FileHeaderBlock headerBlock)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("MemoryStream");
    }

    /// <summary>
    /// Rolls back any changes made to the <see cref="MemoryStream"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the <see cref="MemoryStream"/> has been disposed and cannot perform a rollback.
    /// </exception>
    /// <remarks>
    /// This method rolls back any changes made to the current <see cref="MemoryStream"/>. If the <see cref="MemoryStream"/> 
    /// has been disposed (IsDisposed is <c>true</c>), it throws an <see cref="ObjectDisposedException"/> indicating that the 
    /// stream has been disposed and cannot perform a rollback.
    /// </remarks>
    public void RollbackChanges()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("MemoryStream");
    }

    /// <summary>
    /// Changes the extension of the current file or resource.
    /// </summary>
    /// <param name="extension">The new file extension to set.</param>
    /// <param name="isReadOnly">Specifies whether the file/resource should be treated as read-only.</param>
    /// <param name="isSharingEnabled">Specifies whether sharing of the file or resource should be enabled.</param>
    /// <remarks>
    /// This method changes the extension of the current file or resource to the specified <paramref name="extension"/>.
    /// It allows you to modify read-only and sharing settings by providing the <paramref name="isReadOnly"/> and
    /// <paramref name="isSharingEnabled"/> parameters. However, the actual implementation of this method should be added.
    /// </remarks>
    public void ChangeExtension(string extension, bool isReadOnly, bool isSharingEnabled)
    {
        m_isReadOnly = isReadOnly;
    }

    /// <summary>
    /// Changes the sharing mode of the current file or resource.
    /// </summary>
    /// <param name="isReadOnly">Specifies whether the file or resource should be treated as read-only.</param>
    /// <param name="isSharingEnabled">Specifies whether sharing of the file or resource should be enabled.</param>
    /// <remarks>
    /// This method allows you to modify read-only and sharing settings for the current file or resource.
    /// By providing the <paramref name="isReadOnly"/> and <paramref name="isSharingEnabled"/> parameters,
    /// you can control how the file or resource is accessed and shared. However, the actual implementation
    /// of this method should be added.
    /// </remarks>
    public void ChangeShareMode(bool isReadOnly, bool isSharingEnabled)
    {
        m_isReadOnly = isReadOnly;
    }

    #endregion
}