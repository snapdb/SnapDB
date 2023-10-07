//******************************************************************************************************
//  SortedTreeUInt32.cs - Gbtc
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
//  04/12/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;

namespace SnapDB.Snap.Types;

public class SnapUInt32 : SnapTypeBase<SnapUInt32>
{
    #region [ Members ]

    public uint Value;

    #endregion

    #region [ Constructors ]

    public SnapUInt32()
    {
    }

    public SnapUInt32(uint value)
    {
        Value = value;
    }

    #endregion

    #region [ Properties ]

    public override Guid GenericTypeGuid =>
        // {03F4BD3A-D9CF-4358-B175-A9D38BE6715A}
        new(0x03f4bd3a, 0xd9cf, 0x4358, 0xb1, 0x75, 0xa9, 0xd3, 0x8b, 0xe6, 0x71, 0x5a);

    public override int Size => 4;

    #endregion

    #region [ Methods ]

    public override void CopyTo(SnapUInt32 destination)
    {
        destination.Value = Value;
    }

    public override int CompareTo(SnapUInt32 other)
    {
        return Value.CompareTo(other.Value);
    }

    public override unsafe int CompareTo(byte* stream)
    {
        return Value.CompareTo(*(uint*)stream);
    }

    public override void SetMin()
    {
        Value = uint.MinValue;
    }

    /// <summary>
    /// Sets the SnapUInt32 value to the maximum possible value (UInt32.MaxValue).
    /// </summary>
    public override void SetMax()
    {
        Value = uint.MaxValue;
    }

    /// <summary>
    /// Clears (sets to zero) the SnapUInt32 value.
    /// </summary>
    public override void Clear()
    {
        Value = 0;
    }

    /// <summary>
    /// Reads a SnapUInt32 value from the specified binary stream.
    /// </summary>
    /// <param name="stream">The binary stream from which to read the SnapUInt32 value.</param>
    public override void Read(BinaryStreamBase stream)
    {
        Value = stream.ReadUInt32();
    }

    /// <summary>
    /// Writes the SnapUInt32 value to the specified binary stream.
    /// </summary>
    /// <param name="stream">The binary stream to which to write the SnapUInt32 value.</param>
    public override void Write(BinaryStreamBase stream)
    {
        stream.Write(Value);
    }

    // Read(byte*)
    // Write(byte*)
    // IsLessThan(T)
    // IsEqualTo(T)
    // IsGreaterThan(T)
    // IsLessThanOrEqualTo(T)
    // IsBetween(T,T)

    /// <summary>
    /// Reads a SnapUInt32 value from the specified byte stream pointer.
    /// </summary>
    /// <param name="stream">A pointer to the byte stream from which to read the SnapUInt32 value.</param>
    public override unsafe void Read(byte* stream)
    {
        Value = *(uint*)stream;
    }

    /// <summary>
    /// Writes the SnapUInt32 value to the specified byte stream pointer.
    /// </summary>
    /// <param name="stream">A pointer to the byte stream to which to write the SnapUInt32 value.</param>
    public override unsafe void Write(byte* stream)
    {
        *(uint*)stream = Value;
    }

    /// <summary>
    /// Determines whether the current SnapUInt32 is less than the specified SnapUInt32.
    /// </summary>
    /// <param name="right">The SnapUInt32 to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current SnapUInt32 is less than the specified SnapUInt32; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsLessThan(SnapUInt32 right)
    {
        return Value < right.Value;
    }

    /// <summary>
    /// Determines whether the current SnapUInt32 is equal to the specified SnapUInt32.
    /// </summary>
    /// <param name="right">The SnapUInt32 to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current SnapUInt32 is equal to the specified SnapUInt32; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsEqualTo(SnapUInt32 right)
    {
        return Value == right.Value;
    }

    /// <summary>
    /// Determines whether the current SnapUInt32 is greater than the specified SnapUInt32.
    /// </summary>
    /// <param name="right">The SnapUInt32 to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current SnapUInt32 is greater than the specified SnapUInt32; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsGreaterThan(SnapUInt32 right)
    {
        return Value > right.Value;
    }

    /// <summary>
    /// Determines whether the current SnapUInt32 is greater than or equal to the specified SnapUInt32.
    /// </summary>
    /// <param name="right">The SnapUInt32 to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current SnapUInt32 is greater than or equal to the specified SnapUInt32; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsGreaterThanOrEqualTo(SnapUInt32 right)
    {
        return Value >= right.Value;
    }

    /// <summary>
    /// Determines whether the current SnapUInt32 is between the specified lower and upper bounds (inclusive lower bound, exclusive upper bound).
    /// </summary>
    /// <param name="lowerBounds">The lower bounds (inclusive).</param>
    /// <param name="upperBounds">The upper bounds (exclusive).</param>
    /// <returns>
    /// <c>true</c> if the current SnapUInt32 is between the specified lower and upper bounds; otherwise, <c>false</c>.
    /// </returns>
    public override bool IsBetween(SnapUInt32 lowerBounds, SnapUInt32 upperBounds)
    {
        return lowerBounds.Value <= Value && Value < upperBounds.Value;
    }

    /// <summary>
    /// Creates and returns a new instance of SnapCustomMethodsUInt32 to provide custom methods for SnapUInt32 values.
    /// </summary>
    /// <returns>A new instance of SnapCustomMethodsUInt32.</returns>
    public override SnapTypeCustomMethods<SnapUInt32> CreateValueMethods()
    {
        return new SnapCustomMethodsUInt32();
    }

    #endregion
}