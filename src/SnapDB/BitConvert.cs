﻿//******************************************************************************************************
//  BitMath.cs - Gbtc
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
//  06/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/14/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB;

/// <summary>
/// Contains functions that convert from floating-points to unsigned 64-bit integers,
/// and from unsigned 64-bit integers to floating-points.
/// </summary>
public static class BitConvert
{
    #region [ Static ]

    /// <summary>
    /// Converts a single-precision floating-point number to an unsigned 64-bit integer representation.
    /// </summary>
    /// <param name="value">The single-precision floating-point number to convert.</param>
    /// <returns>
    /// An unsigned 64-bit integer representation of the input single-precision floating-point number.
    /// </returns>
    /// <remarks>
    /// This method performs an unsafe conversion by treating the input float as a uint and
    /// then casting it to ulong.
    /// </remarks>
    /// <seealso cref="ToUInt64(float)"/>
    public static unsafe ulong ToUInt64(float value)
    {
        return *(uint*)&value;
    }

    /// <summary>
    /// Converts an unsigned 64-bit integer to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer to convert.</param>
    /// <returns>
    /// A single-precision floating-point number representing the input unsigned 64-bit integer.
    /// </returns>
    /// <remarks>
    /// This method performs an unsafe conversion by first casting the input ulong to a uint
    /// and then treating it as a float.
    /// </remarks>
    /// <seealso cref="ToUInt64(float)"/>
    public static unsafe float ToSingle(ulong value)
    {
        uint tmpValue = (uint)value;

        return *(float*)&tmpValue;
    }

    #endregion
}