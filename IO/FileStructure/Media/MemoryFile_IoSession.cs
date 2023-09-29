//******************************************************************************************************
//  MemoryFile_IoSession.cs - Gbtc
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
//  05/01/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

/// <summary>
/// Provides a in memory stream that uses pages that are pooled in the unmanaged buffer pool.
/// </summary>
internal partial class MemoryPoolFile
{
    /// <summary>
    /// An I/O session for the <see cref="MemoryPoolFile"/>.
    /// </summary>
    private class IoSession
        : BinaryStreamIoSessionBase
    {
        private readonly MemoryPoolFile m_file;

        /// <summary>
        /// Creates a new <see cref="IoSession"/>.
        /// </summary>
        /// <param name="file">The base file.</param>
        public IoSession(MemoryPoolFile file)
        {
            m_file = file;
        }

        /// <summary>
        /// Retrieves a data block based on the provided <see cref="BlockArguments"/>.
        /// </summary>
        /// <param name="args">The <see cref="BlockArguments"/> that specify the block to retrieve.</param>
        /// <exception cref="ReadOnlyException">Thrown if an attempt to write to a read-only file system is made.</exception>
        /// <remarks>
        /// This method retrieves a data block specified by the <paramref name="args"/> parameter. It checks whether the file system is
        /// read-only and whether the requested operation involves writing. If the file system is read-only and a write operation is
        /// requested, a <see cref="ReadOnlyException"/> is thrown. Otherwise, it delegates the block retrieval operation to the underlying file.
        /// </remarks>
        /// <param name="args">The <see cref="BlockArguments"/> that specify the block to retrieve.</param>
        public override void GetBlock(BlockArguments args)
        {
            if (args.IsWriting && m_file.m_isReadOnly)
                throw new ReadOnlyException("File system is read only");
            args.SupportsWriting = !m_file.m_isReadOnly;
            m_file.GetBlock(args);
        }

        /// <summary>
        /// Sets the current usage of the <see cref="BinaryStreamIoSessionBase"/> to <c>null</c>.
        /// </summary>
        public override void Clear()
        {
        }
    }
}