﻿//******************************************************************************************************
//  SubFileStream_SimplifiedIoSession.cs - Gbtc
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
//  10/15/2014 - Steven E. Chisholm
//       Generated original version of source code.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure;

public partial class SubFileStream
{
    #region [ Members ]

    /// <summary>
    /// An IO Session for the sub file stream.
    /// </summary>
    private unsafe class SimplifiedIoSession : BinaryStreamIoSessionBase
    {
        #region [ Members ]

        private readonly int m_blockDataLength;

        private DiskIoSession m_dataIoSession;

        private readonly SubFileStream m_stream;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        public SimplifiedIoSession(SubFileStream stream)
        {
            m_stream = stream;
            m_dataIoSession = stream.m_dataReader.CreateDiskIoSession(stream.m_fileHeaderBlock, stream.SubFile);
            m_blockDataLength = m_stream.m_blockSize - FileStructureConstants.BlockFooterLength;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="IoSession"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            try
            {
                if (!disposing)
                    return;

                if (m_dataIoSession is null)
                    return;

                m_dataIoSession.Dispose();
            }
            finally
            {
                m_dataIoSession = null!;
                m_disposed = true; // Prevent duplicate dispose.
                base.Dispose(disposing); // Call base class Dispose().
            }
        }

        /// <summary>
        /// Sets the current usage of the <see cref="BinaryStreamIoSessionBase"/> to <c>null</c>.
        /// </summary>
        public override void Clear()
        {
            ObjectDisposedException.ThrowIf(IsDisposed || m_dataIoSession.IsDisposed, this);

            m_dataIoSession.Clear();
        }

        public override void GetBlock(BlockArguments args)
        {
            int blockDataLength = m_blockDataLength;
            long pos = args.Position;

            ObjectDisposedException.ThrowIf(IsDisposed || m_dataIoSession.IsDisposed, this);

            if (pos < 0)
                throw new ArgumentOutOfRangeException(nameof(args), "position cannot be negative");

            if (pos >= blockDataLength * (uint.MaxValue - 1))
                throw new ArgumentOutOfRangeException(nameof(args), "position reaches past the end of the file.");

            uint indexPosition;

            // 64-bit signed divide is twice as slow as 64-bit unsigned.
            if (pos <= uint.MaxValue)
                indexPosition = (uint)pos / (uint)blockDataLength;
            else
                indexPosition = (uint)((ulong)pos / (ulong)blockDataLength);

            args.FirstPosition = indexPosition * blockDataLength;
            args.Length = blockDataLength;

            if (args.IsWriting)
                throw new Exception("File is read only");

            // Reading
            if (indexPosition >= m_stream.SubFile!.DataBlockCount)
                throw new ArgumentOutOfRangeException(nameof(args), "position reaches past the end of the file.");

            uint physicalBlockIndex = m_stream.SubFile.DirectBlock + indexPosition;

            m_dataIoSession.Read(physicalBlockIndex, BlockType.DataBlock, indexPosition);
            args.FirstPointer = (nint)m_dataIoSession.Pointer;
            args.SupportsWriting = false;
        }

        #endregion
    }

    #endregion
}