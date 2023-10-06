//******************************************************************************************************
//  BinaryStreamIoSessionBase.cs - Gbtc
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
//  04/26/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Implementing this interface allows a binary stream to be attached to a buffer.
/// </summary>
public abstract class BinaryStreamIoSessionBase : IDisposable
{
    #region [ Properties ]

    /// <summary>
    /// Gets if the object has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="BinaryStreamIoSessionBase"/> object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="BinaryStreamIoSessionBase"/> object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> releases both managed and unmanaged resources; <c>false</c> releases only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        IsDisposed = true; // Prevent duplicate dispose.
    }

    /// <summary>
    /// Gets a block for the following I/O session.
    /// </summary>
    /// <param name="args">The block request that needs to be fulfilled by this IOSession.</param>
    public abstract void GetBlock(BlockArguments args);

    /// <summary>
    /// Sets the current usage of the <see cref="BinaryStreamIoSessionBase"/> to <c>null</c>.
    /// </summary>
    public abstract void Clear();

    #endregion
}