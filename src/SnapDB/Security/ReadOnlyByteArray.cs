﻿//******************************************************************************************************
//  ReadonlyByteArray.cs - Gbtc
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

namespace SnapDB.Security;

/// <summary>
/// Provides a way for byte arrays to be added to sorted lists and dictionaries.
/// </summary>
public readonly struct ReadonlyByteArray : IComparable<ReadonlyByteArray>, IEquatable<ReadonlyByteArray>
{
    #region [ Members ]

    private readonly int m_hashCode;
    private readonly byte[] m_value;

    #endregion

    #region [ Constructors ]
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadonlyByteArray"/> struct with the specified byte array.
    /// </summary>
    /// <param name="array">The byte array to wrap.</param>
    public ReadonlyByteArray(byte[] array)
    {
        m_value = array;
        if (array is null)
            m_hashCode = 0;
        else
            m_hashCode = ComputeHash(array);
    }

    #endregion

    #region [ Methods ]
    /// <summary>
    /// Returns the hash code for this <see cref="ReadonlyByteArray"/> instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return m_hashCode;
    }

    /// <summary>
    /// Determines whether this <see cref="ReadonlyByteArray"/> instance is equal to another object.
    /// </summary>
    /// <param name="other">The object to compare with this instance.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public override bool Equals(object other)
    {
        if (other is ReadonlyByteArray)
            return Equals((ReadonlyByteArray)other);
        return false;
    }

    /// <summary>
    /// Compares this <see cref="ReadonlyByteArray"/> instance with another instance for ordering.
    /// </summary>
    /// <param name="other">The <see cref="ReadonlyByteArray"/> to compare with this instance.</param>
    /// <returns>
    /// A negative value if this instance is less than <paramref name="other"/>,
    /// a positive value if this instance is greater than <paramref name="other"/>,
    /// or zero if they are equal.
    /// </returns>
    public int CompareTo(ReadonlyByteArray other)
    {
        if (m_value is null && other.m_value is null)
            return 0;
        if (m_value is null)
            return -1;
        if (other.m_value is null)
            return 1;
        if (m_value.Length < other.m_value.Length)
            return 1;
        if (m_value.Length > other.m_value.Length)
            return -1;

        for (int x = 0; x < m_value.Length; x++)
        {
            if (m_value[x] < other.m_value[x])
                return -1;
            if (m_value[x] > other.m_value[x])
                return 1;
        }

        return 0;
    }

    /// <summary>
    /// Determines whether this <see cref="ReadonlyByteArray"/> instance is equal to another instance.
    /// </summary>
    /// <param name="other">The <see cref="ReadonlyByteArray"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(ReadonlyByteArray other)
    {
        if (m_hashCode != other.m_hashCode)
            return false;
        if (m_value is null && other.m_value is null)
            return true;
        if (m_value is null || other.m_value is null)
            return false;
        return m_value.SequenceEqual(other.m_value);
    }

    #endregion

    #region [ Static ]

    //http://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c-sharp
    private static int ComputeHash(byte[] data)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < data.Length; i++)
                hash = (hash ^ data[i]) * p;

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;
            return hash;
        }
    }

    #endregion
}