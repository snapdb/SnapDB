//******************************************************************************************************
//  ReadSnapshot.cs - Gbtc
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
//  12/02/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO.FileStructure.Media;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// Acquires a snapshot of the file system to browse in an isolated manner.
/// This is read only and will also block the main file from being deleted.
/// Therefore it is important to release this lock so the file can be deleted after a rollover.
/// </summary>
public class ReadSnapshot
{
    #region [ Members ]

    // The underlying disk IO instance used to read snapshot data
    private readonly DiskIo m_dataReader;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a readonly copy of a transaction.
    /// </summary>
    /// <param name="dataReader"><see cref="DiskIo"/> data reader.</param>
    internal ReadSnapshot(DiskIo dataReader)
    {
        if (dataReader is null)
            throw new ArgumentNullException(nameof(dataReader));

        Header = dataReader.LastCommittedHeader;
        m_dataReader = dataReader;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the header of the file structure.
    /// </summary>
    public FileHeaderBlock Header { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Opens an ArchiveFileStream that can be used to read the file passed to this function.
    /// </summary>
    /// <param name="fileIndex">The index of the file to open.</param>
    public SubFileStream OpenFile(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= Header.Files.Count)
            throw new ArgumentOutOfRangeException(nameof(fileIndex), "The file index provided could not be found in the header.");

        return new SubFileStream(m_dataReader, Header.Files[fileIndex], Header, true);
    }

    /// <summary>
    /// Opens an ArchiveFileStream that can be used to read/write to the file passed to this function.
    /// </summary>
    public SubFileStream OpenFile(SubFileName fileName)
    {
        for (int x = 0; x < Header.Files.Count; x++)
        {
            SubFileHeader? file = Header.Files[x];

            if (file?.FileName == fileName)
                return OpenFile(x);
        }

        throw new Exception("File does not exist");
    }

    #endregion
}