﻿//******************************************************************************************************
//  BufferedFile_IoStream.cs - Gbtc
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

using Gemstone.Diagnostics;
using SnapDB.IO.Unmanaged;

namespace SnapDB.IO.FileStructure.Media;

internal partial class BufferedFile
{
    #region [ Members ]

    /// <summary>
    /// The <see cref="BinaryStreamIoSessionBase"/> utilized by the <see cref="BufferedFile"/>.
    /// </summary>
    private class IoSession : PageReplacementAlgorithm.PageLock
    {
        #region [ Members ]

        /// <summary>
        /// The base stream
        /// </summary>
        private readonly BufferedFile m_stream;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="IoSession"/>
        /// </summary>
        /// <param name="stream">The base class.</param>
        /// <param name="pageReplacement">The page replacement algorithm</param>
        internal IoSession(BufferedFile stream, PageReplacementAlgorithm pageReplacement) : base(pageReplacement)
        {
            m_stream = stream;
            m_stream.m_queue.Open();
        }

    #if DEBUG
        /// <summary>
        /// Releases the unmanaged resources before the <see cref="IoSession"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~IoSession()
        {
            s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
            Dispose(false);
        }
    #endif

        #endregion

        #region [ Methods ]

        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                m_disposed = true;

                if (disposing)
                    m_stream.m_queue.Close();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets a block for the following I/O session.
        /// </summary>
        /// <param name="args">The <see cref="BlockArguments"/> to use to read and write to a block.</param>
        public override void GetBlock(BlockArguments args)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            m_stream.GetBlock(this, args);
        }

        #endregion

        #region [ Static ]

        private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(IoSession), MessageClass.Component);

        #endregion
    }

    #endregion
}