﻿//******************************************************************************************************
//  BlockArguments.cs - Gbtc
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
/// A set of fields that are passed to a <see cref="BinaryStreamIoSessionBase.GetBlock"/> method to get results.
/// </summary>
public class BlockArguments
{
    #region [ Members ]

    /// <summary>
    /// The pointer for the first byte of the block.
    /// </summary>
    public nint FirstPointer;

    /// <summary>
    /// The position that corresponds to <see cref="FirstPointer"/>.
    /// </summary>
    public long FirstPosition;

    /// <summary>
    /// Indicates if the stream plans to write to this block.
    /// </summary>
    public bool IsWriting;

    /// <summary>
    /// The length of the block.
    /// </summary>
    public int Length;

    /// <summary>
    /// The block returned must contain this position.
    /// </summary>
    public long Position;

    /// <summary>
    /// Notifies the calling class if this block supports
    /// writing without requiring this function to be called again if <see cref="IsWriting"/> was set to <c>false</c>.
    /// </summary>
    public bool SupportsWriting;

    #endregion
}