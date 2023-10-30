//******************************************************************************************************
//  SnapTypeBaseOfT.cs - Gbtc
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

namespace SnapDB.Snap;

/// <summary>
/// The interface that is required to use as a value in the sorted tree.
/// </summary>
/// <typeparam name="T">A class that has a default constructor</typeparam>
/// <remarks>
/// It is highly recommended to override many of the base class methods as many of these methods are slow.
/// The following methods should be overriden if possible:
/// Read(byte*)
/// Write(byte*)
/// IsLessThan(T)
/// IsEqualTo(T)
/// IsGreaterThan(T)
/// IsLessThanOrEqualTo(T)
/// IsBetween(T,T)
/// CompareTo(byte*)
/// IsLessThanOrEqualTo(byte*, byte*)
/// For better random I/O inserts, it is also a good idea to implement a custom
/// <see cref="SnapTypeCustomMethods{T}"/> that overrides
/// the <see cref="SnapTypeCustomMethods{T}.BinarySearch"/> method.
/// </remarks>
public abstract class SnapTypeBase<T> : SnapTypeBase, IComparable<T>, IEquatable<T>, IComparer<T> where T : SnapTypeBase<T>, new()
{
    #region [ Methods ]

    /// <summary>
    /// Copies the source to the destination.
    /// </summary>
    /// <param name="destination">The destination for the source to be copied to.</param>
    public abstract void CopyTo(T destination);

    /// <summary>
    /// Compares the current <typeparamref name="T"/> object with the data in the provided byte stream.
    /// </summary>
    /// <param name="stream">A pointer to a byte stream containing data to compare against.</param>
    /// <returns>
    /// A value that indicates the relative order of the current <typeparamref name="T"/> object and the data in the stream.
    /// - Less than zero: The current object is less than the data in the stream.
    /// - Zero: The current object is equal to the data in the stream.
    /// - Greater than zero: The current object is greater than the data in the stream.
    /// </returns>
    public virtual unsafe int CompareTo(byte* stream)
    {
        T other = new();
        other.Read(stream);

        return CompareTo(other);
    }

    /// <summary>
    /// Creates and returns an instance of custom methods for handling values of the <typeparamref name="T"/> type.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="SnapTypeCustomMethods{T}"/> for custom value handling.
    /// </returns>
    public virtual SnapTypeCustomMethods<T> CreateValueMethods()
    {
        return new SnapTypeCustomMethods<T>();
    }

    /// <summary>
    /// Compares the current instance with another instance of the same type and determines whether they are equal.
    /// </summary>
    /// <param name="right">The instance to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is equal to the <paramref name="right"/> instance; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsEqualTo(T? right)
    {
        return CompareTo(right) == 0;
    }

    /// <summary>
    /// Compares the current instance with another instance of the same type and determines whether they are not equal.
    /// </summary>
    /// <param name="right">The instance to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is not equal to the <paramref name="right"/> instance; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsNotEqualTo(T? right)
    {
        return CompareTo(right) != 0;
    }

    /// <summary>
    /// Determines whether the current instance falls within a specified range defined by lower and upper bounds.
    /// </summary>
    /// <param name="lowerBounds">The lower bounds of the range (inclusive).</param>
    /// <param name="upperBounds">The upper bounds of the range (exclusive).</param>
    /// <returns>
    /// <c>true</c> if the current instance is greater than or equal to <paramref name="lowerBounds"/> (inclusive)
    /// and less than <paramref name="upperBounds"/> (exclusive); otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsBetween(T lowerBounds, T upperBounds)
    {
        return lowerBounds.IsLessThanOrEqualTo((T)this) && IsLessThan(upperBounds);
    }

    /// <summary>
    /// Determines whether the current instance is less than or equal to a specified value.
    /// </summary>
    /// <param name="right">The value to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is less than or equal to <paramref name="right"/>; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsLessThanOrEqualTo(T right)
    {
        return CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether the current instance is less than a specified value.
    /// </summary>
    /// <param name="right">The value to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is less than <paramref name="right"/>; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsLessThan(T right)
    {
        return CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether the current instance is greater than a specified value.
    /// </summary>
    /// <param name="right">The value to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is greater than <paramref name="right"/>; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsGreaterThan(T right)
    {
        return CompareTo(right) > 0;
    }

    /// <summary>
    /// Determines whether the current instance is greater than or equal to a specified value.
    /// </summary>
    /// <param name="right">The value to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the current instance is greater than or equal to <paramref name="right"/>; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool IsGreaterThanOrEqualTo(T right)
    {
        return CompareTo(right) >= 0;
    }

    /// <summary>
    /// Creates a new instance that is a copy of the current instance.
    /// </summary>
    /// <returns>A new instance that is a copy of the current instance.</returns>
    public virtual T Clone()
    {
        T clone = new();
        CopyTo(clone);
        return clone;
    }

    /// <summary>
    /// Compares the current instance to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">the key to compare to</param>
    /// <returns>
    /// A value that indicates the relative order of the objects being compared.
    /// </returns>
    public abstract int CompareTo(T? other);

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>.
    /// </returns>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    public virtual int Compare(T? x, T? y)
    {
        return x switch
        {
            null when y is null => 0,
            null => -1,
            _ => y is null ? 1 : x.CompareTo(y)
        };
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public virtual bool Equals(T? other)
    {
        return IsEqualTo(other);
    }

    #endregion
}