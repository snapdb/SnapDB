﻿//******************************************************************************************************
//  UnmanagedMemoryStream.cs - Gbtc
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
//  09/30/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Provides a in memory stream that allocates its own unmanaged memory.
/// </summary>
public partial class UnmanagedMemoryStream : UnmanagedMemoryStreamCore, ISupportsBinaryStream
{
    #region [ Constructors ]

    /// <summary>
    /// Create a new <see cref="UnmanagedMemoryStream"/> that allocates its own unmanaged memory.
    /// </summary>
    /// <param name="allocationSize">The set definition for the size capacity of memory allocations.</param>
    public UnmanagedMemoryStream(int allocationSize = 4096) : base(allocationSize)
    {
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets if the stream can be written to.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the number of available simultaneous read and write sessions.
    /// </summary>
    /// <remarks>
    /// This value is used to determine if a binary stream can be cloned
    /// to improve read, write, and copy performance.
    /// </remarks>
    int ISupportsBinaryStream.RemainingSupportedIoSessions => int.MaxValue;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Creates a new binary from an IO session.
    /// </summary>
    /// <returns>A new <see cref="BinaryStreamBase"/> instance of type <see cref="BinaryStream"/>.</returns>
    public BinaryStreamBase CreateBinaryStream()
    {
        return new BinaryStream(this);
    }

    /// <summary>
    /// Acquire an IO Session.
    /// </summary>
    /// <returns>A new <see cref="BinaryStreamIoSessionBase"/> instance of type <see cref="BinaryStreamIoSessionBase"/>.</returns>
    public BinaryStreamIoSessionBase CreateIoSession()
    {
        return new IoSession(this);
    }

    #endregion
}