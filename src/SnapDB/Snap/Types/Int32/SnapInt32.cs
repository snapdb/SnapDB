//******************************************************************************************************
//  SortedTreeInt32.cs - Gbtc
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

/// <summary>
/// Represents a 32-bit integer value that can be serialized.
/// </summary>
public class SnapInt32 : SnapTypeBase<SnapInt32>
{
    #region [ Members ]

    /// <summary>
    /// Gets or sets the integer value.
    /// </summary>
    public int Value;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapInt32"/> class.
    /// </summary>
    public SnapInt32()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapInt32"/> class with the specified value.
    /// </summary>
    /// <param name="value">The integer value to initialize with.</param>
    public SnapInt32(int value)
    {
        Value = value;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the globally unique identifier (GUID) representing the SnapInt32 data type.
    /// </summary>
    public override Guid GenericTypeGuid =>
        // {9DCCEEBA-D191-49CC-AF03-118C0D7D221A}
        new(0x9dcceeba, 0xd191, 0x49cc, 0xaf, 0x03, 0x11, 0x8c, 0x0d, 0x7d, 0x22, 0x1a);

    /// <summary>
    /// Gets the size of the SnapInt32 data type in bytes.
    /// </summary>
    public override int Size => 4;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Copies the value of this SnapInt32 to the specified destination SnapInt32.
    /// </summary>
    /// <param name="destination">The destination SnapInt32 to copy to.</param>
    public override void CopyTo(SnapInt32 destination)
    {
        destination.Value = Value;
    }

    /// <summary>
    /// Compares this SnapInt32 to another SnapInt32 and returns an integer that indicates their relative values.
    /// </summary>
    /// <param name="other">The SnapInt32 to compare with.</param>
    /// <returns>A signed integer that indicates the relative values of this instance and the other SnapInt32.</returns>
    public override int CompareTo(SnapInt32 other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// Compares this SnapInt32 to a memory stream and returns an integer that indicates their relative values.
    /// </summary>
    /// <param name="stream">A pointer to the memory stream to compare with.</param>
    /// <returns>A signed integer that indicates the relative values of this instance and the memory stream.</returns>
    public override unsafe int CompareTo(byte* stream)
    {
        return Value.CompareTo(*(int*)stream);
    }

    /// <summary>
    /// Sets the value of this SnapInt32 to the minimum possible value.
    /// </summary>
    public override void SetMin()
    {
        Value = int.MinValue;
    }

    /// <summary>
    /// Sets the value of this SnapInt32 to the maximum possible value.
    /// </summary>
    public override void SetMax()
    {
        Value = int.MaxValue;
    }

    /// <summary>
    /// Clears the value of this SnapInt32, setting it to 0.
    /// </summary>
    public override void Clear()
    {
        Value = 0;
    }

    /// <summary>
    /// Reads the value of this SnapInt32 from a binary stream.
    /// </summary>
    /// <param name="stream">The binary stream from which to read the value.</param>
    public override void Read(BinaryStreamBase stream)
    {
        Value = stream.ReadInt32();
    }

    /// <summary>
    /// Writes the value of this SnapInt32 to a binary stream.
    /// </summary>
    /// <param name="stream">The binary stream to which to write the value.</param>
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
    /// Reads the value of this SnapInt32 from a memory stream.
    /// </summary>
    /// <param name="stream">A pointer to the memory stream from which to read the value.</param>
    public override unsafe void Read(byte* stream)
    {
        Value = *(int*)stream;
    }

    /// <summary>
    /// Writes the value of this SnapInt32 to a memory stream.
    /// </summary>
    /// <param name="stream">A pointer to the memory stream to which to write the value.</param>
    public override unsafe void Write(byte* stream)
    {
        *(int*)stream = Value;
    }

    /// <summary>
    /// Determines whether this SnapInt32 is less than the specified SnapInt32.
    /// </summary>
    /// <param name="right">The SnapInt32 to compare to.</param>
    /// <returns><c>true</c> if this SnapInt32 is less than the specified SnapInt32; otherwise, <c>false</c>.</returns>
    public override bool IsLessThan(SnapInt32 right)
    {
        return Value < right.Value;
    }

    /// <summary>
    /// Determines whether this SnapInt32 is equal to the specified SnapInt32.
    /// </summary>
    /// <param name="right">The SnapInt32 to compare to.</param>
    /// <returns><c>true</c> if this SnapInt32 is equal to the specified SnapInt32; otherwise, <c>false</c>.</returns>
    public override bool IsEqualTo(SnapInt32 right)
    {
        return Value == right.Value;
    }

    /// <summary>
    /// Determines whether this SnapInt32 is greater than the specified SnapInt32.
    /// </summary>
    /// <param name="right">The SnapInt32 to compare to.</param>
    /// <returns><c>true</c> if this SnapInt32 is greater than the specified SnapInt32; otherwise, <c>false</c>.</returns>
    public override bool IsGreaterThan(SnapInt32 right)
    {
        return Value > right.Value;
    }

    /// <summary>
    /// Determines whether this SnapInt32 is greater than or equal to the specified SnapInt32.
    /// </summary>
    /// <param name="right">The SnapInt32 to compare to.</param>
    /// <returns><c>true</c> if this SnapInt32 is greater than or equal to the specified SnapInt32; otherwise, <c>false</c>.</returns>
    public override bool IsGreaterThanOrEqualTo(SnapInt32 right)
    {
        return Value >= right.Value;
    }

    /// <summary>
    /// Determines whether this SnapInt32 is between the specified lower and upper bounds (inclusive).
    /// </summary>
    /// <param name="lowerBounds">The lower bounds SnapInt32 to compare to.</param>
    /// <param name="upperBounds">The upper bounds SnapInt32 to compare to.</param>
    /// <returns><c>true</c> if this SnapInt32 is between the specified lower and upper bounds; otherwise, <c>false</c>.</returns>
    public override bool IsBetween(SnapInt32 lowerBounds, SnapInt32 upperBounds)
    {
        return lowerBounds.Value <= Value && Value < upperBounds.Value;
    }

    /// <summary>
    /// Creates and returns custom methods for SnapInt32 values.
    /// </summary>
    /// <returns>A <see cref="SnapTypeCustomMethods{T}"/> instance for SnapInt32.</returns>
    public override SnapTypeCustomMethods<SnapInt32> CreateValueMethods()
    {
        return new SnapCustomMethodsInt32();
    }

    #endregion
}