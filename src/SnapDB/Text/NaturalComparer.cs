//******************************************************************************************************
//  NaturalComparer.cs - Gbtc
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
//  01/11/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

// Origional Source From http://www.codeproject.com/Articles/22517/Natural-Sort-Comparer
// Licensed under The Code Project Open License (CPOL)

using System.Text.RegularExpressions;

namespace SnapDB.Text;

/// <summary>
/// Provides a natural sorting mechanism for strings, considering embedded numeric values within the strings.
/// </summary>
public class NaturalComparer : Comparer<string>
{
    #region [ Members ]

    private readonly Dictionary<string, string[]> m_table;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalComparer"/> class.
    /// </summary>
    public NaturalComparer()
    {
        m_table = new Dictionary<string, string[]>();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Compares two strings using natural sorting rules.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>,
    /// as shown in the following table:
    /// -1 if <paramref name="x"/> is less than <paramref name="y"/>,
    /// 0 if <paramref name="x"/> equals <paramref name="y"/>,
    /// 1 if <paramref name="x"/> is greater than <paramref name="y"/>.
    /// </returns>
    public override int Compare(string x, string y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;
        if (x == y)
            return 0;

        if (!m_table.TryGetValue(x, out string[]? x1))
        {
            x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            m_table.Add(x, x1);
        }

        if (!m_table.TryGetValue(y, out string[]? y1))
        {
            y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
            m_table.Add(y, y1);
        }

        for (int i = 0; i < x1.Length && i < y1.Length; i++)
        {
            if (x1[i] != y1[i])
                return PartCompare(x1[i], y1[i]);
        }

        if (y1.Length > x1.Length)
            return 1;

        if (x1.Length > y1.Length)
            return -1;

        return 0;
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Compares two string parts, handling numeric values within the strings.
    /// </summary>
    /// <param name="left">The left string part to compare.</param>
    /// <param name="right">The right string part to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="left"/> and <paramref name="right"/>,
    /// as shown in the following table:
    /// -1 if <paramref name="left"/> is less than <paramref name="right"/>,
    /// 0 if <paramref name="left"/> equals <paramref name="right"/>,
    /// 1 if <paramref name="left"/> is greater than <paramref name="right"/>.
    /// </returns>
    private static int PartCompare(string left, string right)
    {
        if (!int.TryParse(left, out int x))
            return left.CompareTo(right);

        if (!int.TryParse(right, out int y))
            return left.CompareTo(right);

        return x.CompareTo(y);
    }

    #endregion
}