//******************************************************************************************************
//  AtomicInt64.cs - Gbtc
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
//  02/16/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;

namespace SnapDB.Threading;

/// <summary>
/// Since 64 bit reads/asignments are not atomic on a 32-bit process, this class
/// wraps the <see cref="Interlocked"/> class to if using a 32-bit process to ensure
/// atomic reads and writes.
/// </summary>
public class AtomicInt64
{
    // Note: This is a class and not a struct to prevent users from copying the struct value
    // which would result in a non-atomic clone of the struct.  

    private long m_value;

/// <summary>
/// Initializes a new instance of the AtomicInt64 class with an optional initial value.
/// </summary>
/// <param name="value">The optional initial value for the AtomicInt64. Default is 0.</param>
    public AtomicInt64(long value = 0)
    {
        m_value = value;
    }

    /// <summary>
    /// Gets or sets the value of the AtomicInt64.
    /// </summary>
    /// <remarks>
    /// For 64-bit processes, the value is directly read or written.
    /// For 32-bit processes, atomic operations are used for thread-safety.
    /// </remarks>
    public long Value
    {
        /// <summary>
        /// Gets the value of the AtomicInt64.
        /// </summary>
        /// <returns>The value of the AtomicInt64.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Environment.Is64BitProcess)
                return m_value;
            
            return Interlocked.Read(ref m_value);
        }

        /// <summary>
        /// Sets the value of the AtomicInt64.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        set
        {
            if (Environment.Is64BitProcess)
                m_value = value;
            
            else
                Interlocked.Exchange(ref m_value, value);
        }
    }

    /// <summary>
    /// Implicitly converts an AtomicInt64 to a long.
    /// </summary>
    /// <param name="value">The AtomicInt64 instance to convert.</param>
    /// <returns>The long value obtained from the AtomicInt64 instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator long(AtomicInt64 value)
    {
        return value.Value;
    }

}
