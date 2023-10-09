//******************************************************************************************************
//  SecurityExtensions.cs - Gbtc
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
//  08/23/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;
using Gemstone.ArrayExtensions;

namespace SnapDB.Security;

/// <summary>
/// Provides extension methods for enhancing security-related operations.
/// </summary>
public static class SecurityExtensions
{
    #region [ Static ]

    /// <summary>
    /// Compares two byte arrays securely, preventing timing attacks.
    /// </summary>
    /// <param name="a">The first byte array to compare.</param>
    /// <param name="b">The second byte array to compare.</param>
    /// <returns><c>true</c> if both arrays are equal; otherwise, false.</returns>
    /// <remarks>
    /// If a or b is <c>null</c>, function returns immediately with a <c>false</c>.
    /// Certain cryptographic attacks can occur by comparing the amount of time it
    /// takes to do certain operations. Comparing two byte arrays is one example.
    /// Therefore this method should take constant time to do a comparison of two arrays.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static bool SecureEquals(this byte[] a, byte[] b)
    {
        if (a is null || b is null)
            return false;
        int difference = a.Length ^ b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
            difference |= a[i] ^ b[i];
        return difference == 0;
    }

    /// <summary>
    /// Does a time constant comparison of the two byte arrays.
    /// </summary>
    /// <param name="a">The first byte array to compare.</param>
    /// <param name="b">The second byte array to compare.</param>
    /// <param name="bPosition">The start position of the <paramref name="b"/> byte array.</param>
    /// <param name="bLength">The length of the portion to compare in <paramref name="b"/>.</param>
    /// <returns><c>true</c> if both arrays are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// If a or b is <c>null</c>, function returns immediately with a <c>false</c>.
    /// Certain cryptographic attacks can occur by comparing the amount of time it
    /// takes to do certain operations. Comparing two byte arrays is one example.
    /// Therefore this method should take constant time to do a comparison of two arrays.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static bool SecureEquals(this byte[] a, byte[] b, int bPosition, int bLength)
    {
        b.ValidateParameters(bPosition, bLength);
        if (a is null || b is null)
            return false;
        int difference = a.Length ^ bLength;
        for (int ia = 0, ib = bPosition; ia < a.Length && ib < b.Length; ia++, ib++)
            difference |= a[ia] ^ b[ib];
        return difference == 0;
    }

    /// <summary>
    /// Does a secure time constant comparison of the two GUIDs.
    /// </summary>
    /// <param name="a">The first GUID to compare.</param>
    /// <param name="b">The second GUID to compare.</param>
    /// <returns><c>true</c> if both GUIDs are equal; otherwise, false.</returns>
    /// <remarks>
    /// Certain cryptographic attacks can occur by comparing the amount of time it
    /// takes to do certain operations. Comparing two byte arrays is one example.
    /// Therefore this method should take constant time to do a comparison of two GUIDs.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static unsafe bool SecureEquals(this Guid a, Guid b)
    {
        int* lpa = (int*)&a;
        int* lpb = (int*)&b;
        int difference = lpa[0] ^ lpb[0];
        difference |= lpa[1] ^ lpb[1];
        difference |= lpa[2] ^ lpb[2];
        difference |= lpa[3] ^ lpb[3];

        return difference == 0;
    }

    #endregion
}