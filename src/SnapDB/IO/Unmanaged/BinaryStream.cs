//******************************************************************************************************
//  BinaryStream.cs - Gbtc
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
//  04/06/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.IO.Unmanaged;

/// <summary>
/// Provides a binary stream for reading and writing data.
/// </summary>
/// <remarks>
/// This class allows reading and writing binary data and is designed for use with little-endian processors.
/// </remarks>
public unsafe class BinaryStream : BinaryStreamPointerBase
{
    #region [ Members ]

    private readonly BlockArguments m_args;

    // Determines if this class owns the underlying stream, thus when Dispose() is called
    // the dispose of the underlying stream will also be called.
    private readonly bool m_leaveOpen;

    private BinaryStreamIoSessionBase m_mainIoSession;

    private BinaryStreamIoSessionBase m_secondaryIoSession = default!;

    private bool m_disposed;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> that is in memory only.
    /// </summary>
    public BinaryStream() : this(new MemoryPoolStream(), false)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("Only designed to run on a little endian processor.");
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> that is in memory only.
    /// </summary>
    public BinaryStream(MemoryPool pool) : this(new MemoryPoolStream(pool), false)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("Only designed to run on a little endian processor.");
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> that is in memory only.
    /// </summary>
    /// <param name="allocatesOwnMemory"><c>true</c> to allocate its own memory rather than using the <see cref="MemoryPool"/>.</param>
    public BinaryStream(bool allocatesOwnMemory) : this(CreatePool(allocatesOwnMemory), false)
    {
        if (!BitConverter.IsLittleEndian)
            throw new Exception("Only designed to run on a little endian processor.");
    }

    /// <summary>
    /// Creates a <see cref="BinaryStream"/> that is at position 0 of the provided stream.
    /// </summary>
    /// <param name="stream">The base stream to use.</param>
    /// <param name="leaveOpen">
    /// Determines if the underlying stream will automatically be disposed of when this class has its dispose method called.
    /// </param>
    public BinaryStream(ISupportsBinaryStream stream, bool leaveOpen = true)
    {
        m_args = new BlockArguments();
        m_leaveOpen = leaveOpen;
        BaseStream = stream;
        FirstPosition = 0;
        Current = null;
        First = null;
        LastRead = null;
        LastWrite = null;

        if (stream.RemainingSupportedIoSessions < 1)
            throw new Exception("Stream has run out of read sessions");

        m_mainIoSession = stream.CreateIoSession();
    }

    /// <summary>
    /// Finalizes an instance of the BinaryStream class.
    /// </summary>
    /// <remarks>
    /// The destructor is responsible for cleaning up resources associated with the BinaryStream instance
    /// when the instance is garbage-collected. It calls the Dispose method to release resources and
    /// suppresses the finalization of this object to prevent it from being finalized again.
    /// </remarks>
    ~BinaryStream()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the underlying stream associated with the BinaryStream instance.
    /// </summary>
    /// <remarks>
    /// The BaseStream property provides access to the stream that this BinaryStream instance operates on.
    /// It can be used for reading data from and writing data to the underlying stream.
    /// </remarks>
    /// <value>The underlying stream.</value>
    public ISupportsBinaryStream? BaseStream { get; private set; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Releases and cleans up resources associated with the object.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from an explicit disposal or during finalization.</param>
    protected override void Dispose(bool disposing)
    {
        // If the object has not been disposed yet
        if (!m_disposed)
            // This will be done regardless of whether the object is finalized or disposed.
            try
            {
                // Dispose of the main I/O session
                m_mainIoSession?.Dispose();

                // Dispose of the secondary I/O session
                m_secondaryIoSession?.Dispose();

                // Dispose of the I/O stream
                if (!m_leaveOpen && BaseStream is not null)
                    BaseStream.Dispose();
            }
            finally
            {
                // Reset various fields and properties to release references.
                FirstPosition = 0;
                LastPosition = 0;
                Current = null;
                First = null;
                LastRead = null;
                LastWrite = null;
                m_mainIoSession = null!;
                m_secondaryIoSession = null!;
                BaseStream = null;
                m_disposed = true;
            }

        base.Dispose(disposing);
    }

    /// <summary>
    /// When accessing the underlying stream, a lock is placed on the data. Calling this method clears that lock.
    /// </summary>
    public void ClearLocks()
    {
        FirstPosition = Position;
        LastPosition = FirstPosition;
        Current = null;
        First = null;
        LastRead = null;
        LastWrite = null;

        m_mainIoSession?.Clear();
        m_secondaryIoSession?.Clear();
    }

    /// <summary>
    /// Updates the local buffer data.
    /// </summary>
    /// <param name="isWriting">hints to the stream if write access is desired.</param>
    public override void UpdateLocalBuffer(bool isWriting)
    {
        // If the block block is already looked up, skip this step.
        if ((isWriting && LastWrite - Current > 0) || (!isWriting && LastRead - Current > 0))
            return;

        long position = FirstPosition + (Current - First);
        m_args.Position = position;
        m_args.IsWriting = isWriting;
        PointerVersion++;
        m_mainIoSession.GetBlock(m_args);
        FirstPosition = m_args.FirstPosition;
        First = (byte*)m_args.FirstPointer;
        LastRead = First + m_args.Length;
        Current = First + (position - FirstPosition);
        LastPosition = FirstPosition + m_args.Length;
        LastWrite = m_args.SupportsWriting ? LastRead : First;
    }

    #endregion

    #region [ Static ]

    private static ISupportsBinaryStream CreatePool(bool allocatesOwnMemory)
    {
        if (allocatesOwnMemory)
            return new UnmanagedMemoryStream();

        return new MemoryPoolStream();
    }

    #endregion
}