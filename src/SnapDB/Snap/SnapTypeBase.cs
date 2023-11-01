//******************************************************************************************************
//  SnapTypeBase.cs - Gbtc
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
//  11/01/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//     
//  09/22/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;
using SnapDB.IO.Unmanaged;

namespace SnapDB.Snap;

/// <summary>
/// Represents the base class for Snap types, providing methods for serialization, comparison, and manipulation.
/// </summary>
public abstract class SnapTypeBase
{
    #region [ Constructors ]

    /// <summary>
    /// Ensures that only <see cref="SnapTypeBase{T}"/> inherits from this class.
    /// </summary>
    protected internal SnapTypeBase()
    {
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The GUID uniquely defining this type.
    /// It is important to uniquely tie 1 type to 1 GUID.
    /// </summary>
    public abstract Guid GenericTypeGuid { get; }

    /// <summary>
    /// Gets the size of this class when serialized.
    /// </summary>
    public abstract int Size { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Sets the provided key to its minimum value.
    /// </summary>
    public abstract void SetMin();

    /// <summary>
    /// Sets the provided key to its maximum value.
    /// </summary>
    public abstract void SetMax();

    /// <summary>
    /// Clears the key.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Reads the provided key from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    public abstract void Read(BinaryStreamBase stream);

    /// <summary>
    /// Writes the provided data to the BinaryWriter.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public abstract void Write(BinaryStreamBase stream);

    /// <summary>
    /// Reads the provided key from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    public virtual unsafe void Read(Stream stream)
    {
        byte[] data = new byte[Size];
        
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, data.Length);
        
        fixed (byte* ptr = data)
            Read(ptr);
    }

    /// <summary>
    /// Writes the provided key to the stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public virtual unsafe void Write(Stream stream)
    {
        byte[] data = new byte[Size];

        fixed (byte* ptr = data)
            Write(ptr);

        stream.Write(data, 0, data.Length);
    }

    /// <summary>
    /// Reads the key from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    public virtual unsafe void Read(byte* stream)
    {
        BinaryStreamPointerWrapper reader = new(stream, Size);
        Read(reader);
    }

    /// <summary>
    /// Writes the key to the stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public virtual unsafe void Write(byte* stream)
    {
        BinaryStreamPointerWrapper writer = new(stream, Size);
        Write(writer);
    }

    /// <summary>
    /// Executes a copy command without modifying the current class.
    /// </summary>
    /// <param name="source">The source buffer to copy from.</param>
    /// <param name="destination">The destination buffer to copy to.</param>
    public virtual unsafe void MethodCopy(byte* source, byte* destination)
    {
        Memory.Copy(source, destination, Size);
    }

    #endregion
}