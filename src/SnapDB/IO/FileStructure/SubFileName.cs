//******************************************************************************************************
//  SubFileName.cs - Gbtc
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
//  05/18/2013 - Steven E. Chisholm
//       Generated original version of source code.
//
//  04/11/2017 - J. Ritchie Carroll
//       Modified code to use FIPS compatible security algorithms when required.
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Security.Cryptography;
using System.Text;

namespace SnapDB.IO.FileStructure;

/// <summary>
/// This is used to generate the file name that will be used for the subfile.
/// </summary>
public class SubFileName : IComparable<SubFileName>, IEquatable<SubFileName>
{
    #region [ Constructors ]

    private SubFileName(long rawValue1, long rawValue2, int rawValue3)
    {
        RawValue1 = rawValue1;
        RawValue2 = rawValue2;
        RawValue3 = rawValue3;
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// The first 8 bytes of the <see cref="SubFileName"/>.
    /// </summary>
    public long RawValue1 { get; }

    /// <summary>
    /// The next 8 bytes of the <see cref="SubFileName"/>.
    /// </summary>
    public long RawValue2 { get; }

    /// <summary>
    /// The final 4 bytes of the <see cref="SubFileName"/>.
    /// </summary>
    public int RawValue3 { get; }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Writes the <see cref="SubFileName"/> to the <see cref="writer"/>.
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        writer.Write(RawValue1);
        writer.Write(RawValue2);
        writer.Write(RawValue3);
    }

    /// <summary>
    /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the specified object  is equal to the current object; otherwise, <c>false</c>.
    /// </returns>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <filterpriority>2</filterpriority>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        return ReferenceEquals(obj, this) || Equals(obj as SubFileName);
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="T:System.Object"/>.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public override int GetHashCode()
    {
        // Since using SHA1 to compute the name. Taking a single field is good enough.
        // ReSharper disable once NonReadonlyMemberInGetHashCode.
        return RawValue3 & int.MaxValue;
    }

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <returns>
    /// A value that indicates the relative order of the objects being compared.
    /// The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero
    /// This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public int CompareTo(SubFileName? other)
    {
        if (other is null)
            return 1;

        int compare = RawValue1.CompareTo(other.RawValue1);

        if (compare != 0)
            return compare;

        compare = RawValue2.CompareTo(other.RawValue2);

        return compare != 0 ? compare : RawValue3.CompareTo(other.RawValue3);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(SubFileName? other)
    {
        return this == other;
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// An empty subfile name. Should not generally be used as a single file system.
    /// Must have unique file names.
    /// </summary>
    public static SubFileName Empty => new(0, 0, 0);

    /// <summary>
    /// Creates a random <see cref="SubFileName"/>
    /// </summary>
    public static SubFileName CreateRandom()
    {
        return Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    /// <summary>
    /// Creates a <see cref="SubFileName"/> from the supplied data.
    /// </summary>
    /// <param name="fileType">The type identifier of the file.</param>
    /// <param name="keyType">The GUID identifier of the type of the SortedTreeStore.</param>
    /// <param name="valueType">The GUID identifier of the value type of the SortedTreeStore.</param>
    public static unsafe SubFileName Create(Guid fileType, Guid keyType, Guid valueType)
    {
        byte[] data = new byte[16 * 3];

        fixed (byte* ptr = data)
        {
            *(Guid*)ptr = fileType;
            *(Guid*)(ptr + 16) = keyType;
            *(Guid*)(ptr + 32) = valueType;
        }

        return Create(data);
    }

    /// <summary>
    /// Creates a <see cref="SubFileName"/> from the supplied data.
    /// </summary>
    /// <param name="fileName">A name associated with the data.</param>
    /// <param name="keyType">The GUID identifier of the type of the <see cref="SortedTreeStore"/>.</param>
    /// <param name="valueType">The GUID identifier of the value type of the <see cref="SortedTreeStore"/>.</param>
    /// <returns></returns>
    public static unsafe SubFileName Create(string fileName, Guid keyType, Guid valueType)
    {
        byte[] data = new byte[16 * 2 + fileName.Length * 2];

        fixed (byte* ptr = data)
        {
            *(Guid*)ptr = keyType;
            *(Guid*)(ptr + 16) = valueType;
        }

        Encoding.Unicode.GetBytes(fileName, 0, fileName.Length, data, 32);

        return Create(data);
    }

    /// <summary>
    /// Creates a <see cref="SubFileName"/> from the supplied data.
    /// </summary>
    /// <param name="data"></param>
    public static unsafe SubFileName Create(byte[] data)
    {
        byte[] hash = SHA1.HashData(data);

        fixed (byte* ptr = hash)
        {
            return new SubFileName(*(long*)ptr, *(long*)(ptr + 8), *(int*)(ptr + 16));
        }
    }

    /// <summary>
    /// Loads the <see cref="SubFileName"/> from the supplied <see cref="reader"/>.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <returns>The subFile's corresponding values.</returns>
    public static SubFileName Load(BinaryReader reader)
    {
        long value1 = reader.ReadInt64();
        long value2 = reader.ReadInt64();
        int value3 = reader.ReadInt32();

        return new SubFileName(value1, value2, value3);
    }

    /// <summary>
    /// Compares the equality of the two file names.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns><c>true</c>if they are equal, <c>false</c> if they are not.</returns>
    public static bool operator ==(SubFileName? a, SubFileName? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.RawValue1 == b.RawValue1 && a.RawValue2 == b.RawValue2 && a.RawValue3 == b.RawValue3;
    }

    /// <summary>
    /// Compares the two files if they are not equal.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>The two files.</returns>
    public static bool operator !=(SubFileName? a, SubFileName? b)
    {
        return !(a == b);
    }

    #endregion
}