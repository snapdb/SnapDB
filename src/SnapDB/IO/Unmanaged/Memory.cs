//******************************************************************************************************
//  Memory.cs - Gbtc
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
//  03/18/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  06/08/2012 - Steven E. Chisholm
//       Removed large page support and simplified unused and untested procedures for initial release
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.InteropServices;
using Gemstone.Diagnostics;

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// This class is used to allocate and free unmanaged memory.
/// To release memory allocated through this class, call the Dispose() method of the return value.
/// </summary>
public sealed class Memory : IDisposable
{
    #region [ Members ]

    private nint m_address;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Allocates unmanaged memory. The block is uninitialized.
    /// </summary>
    /// <param name="requestedSize">
    /// The desired number of bytes to allocate.
    /// Be sure to check the actual size in the return class.
    /// </param>
    /// <returns>The allocated memory.</returns>
    public Memory(int requestedSize)
    {
        if (requestedSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedSize), "must be greater than zero");

        m_address = Marshal.AllocHGlobal(requestedSize);
        Size = requestedSize;
    }

    /// <summary>
    /// Releases the unmanaged resources before the <see cref="Memory"/> object is reclaimed by <see cref="GC"/>.
    /// </summary>
    ~Memory()
    {
        Dispose();
        s_log.Publish(MessageLevel.Info, "Finalizer Called", GetType().FullName);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The pointer to the first byte of this unmanaged memory.
    /// Equals <see cref="IntPtr.Zero"/> if memory has been released.
    /// </summary>
    public nint Address => m_address;

    /// <summary>
    /// The number of bytes in this allocation.
    /// </summary>
    public int Size { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases all the resources used by the <see cref="Memory"/> object.
    /// </summary>
    public void Dispose()
    {
        Size = 0;
        nint value = Interlocked.Exchange(ref m_address, nint.Zero);

        if (value == nint.Zero)
            return;

        try
        {
            Marshal.FreeHGlobal(value);
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.Error, "Unexpected Exception while releasing unmanaged memory", null, null, ex);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Releases the allocated memory back to the OS.
    /// Same thing as calling Dispose().
    /// </summary>
    public void Release()
    {
        Dispose();
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(Memory), MessageClass.Component);

    /// <summary>
    /// Does a safe copy of data from one location to another.
    /// A safe copy allows for the source and destination to overlap.
    /// </summary>
    /// <param name="src">A pointer to the source location from which data will be copied.</param>
    /// <param name="dest">A pointer to the destination location where data will be copied to.</param>
    /// <param name="count">The number of bytes to copy from the source to the destination.</param>
    public static unsafe void Copy(byte* src, byte* dest, int count)
    {
        Buffer.MemoryCopy(src, dest, count, count);
    }

    /// <summary>
    /// Does a safe copy of data from one location to another.
    /// A safe copy allows for the source and destination to overlap.
    /// </summary>
    /// <param name="src">A pointer to the source location from which data will be copied.</param>
    /// <param name="dest">A pointer to the destination location where data will be copied to.</param>
    /// <param name="count">The number of bytes to copy from the source to the destination.</param>
    public static unsafe void Copy(nint src, nint dest, int count)
    {
        Buffer.MemoryCopy((byte*)src, (byte*)dest, count, count);
    }

    /// <summary>
    /// Sets the data in this buffer to all zeroes.
    /// </summary>
    /// <param name="pointer">The starting position.</param>
    /// <param name="length">The number of bytes to clear.</param>
    public static unsafe void Clear(byte* pointer, int length)
    {
        int i;

        for (i = 0; i <= length - 8; i += 8)
            *(long*)(pointer + i) = 0;

        for (; i < length; i++)
            pointer[i] = 0;
    }

    /// <summary>
    /// Sets the data in this buffer to all zeroes.
    /// </summary>
    /// <param name="pointer">The starting position.</param>
    /// <param name="length">The number of bytes to clear.</param>
    public static unsafe void Clear(nint pointer, int length)
    {
        Clear((byte*)pointer, length);
    }

    #endregion
}