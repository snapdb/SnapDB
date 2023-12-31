﻿//******************************************************************************************************
//  PointStreamHelpers.cs - Gbtc
//
//  Copyright © 2023, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/04/2023 - Lillian Gensolin
//       Generated original version of source code.
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using SnapDB.Snap;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests;

public class PointStreamSequentialPoints : TreeStream<HistorianKey, HistorianValue>
{
    #region [ Members ]

    private int m_count;
    private ulong m_start;

    #endregion

    #region [ Constructors ]

    public PointStreamSequentialPoints(int start, int count)
    {
        m_start = (ulong)start;
        m_count = count;
    }

    #endregion

    #region [ Methods ]

    protected override bool ReadNext(HistorianKey key, HistorianValue value)
    {
        if (m_count <= 0)
        {
            key.Timestamp = 0;
            key.PointID = 0;
            value.Value3 = 0;
            value.Value1 = 0;
            return false;
        }

        m_count--;
        key.Timestamp = 0;
        key.PointID = m_start;
        value.AsSingle = 60.251f;
        m_start++;
        return true;
    }

    #endregion
}

public class PointStreamSequential : TreeStream<HistorianKey, HistorianValue>
{
    #region [ Members ]

    private int m_count;
    private readonly Func<ulong, ulong> m_key1;
    private readonly Func<ulong, ulong> m_key2;
    private ulong m_start;
    private readonly Func<ulong, ulong> m_value1;
    private readonly Func<ulong, ulong> m_value2;

    #endregion

    #region [ Constructors ]

    public PointStreamSequential(int start, int count) : this(start, count, x => 1 * x, x => 2 * x, x => 3 * x, x => 4 * x)
    {
    }

    public PointStreamSequential(int start, int count, Func<ulong, ulong> key1, Func<ulong, ulong> key2, Func<ulong, ulong> value1, Func<ulong, ulong> value2)
    {
        m_start = (ulong)start;
        m_count = count;
        m_key1 = key1;
        m_key2 = key2;
        m_value1 = value1;
        m_value2 = value2;
    }

    #endregion

    #region [ Methods ]

    protected override bool ReadNext(HistorianKey key, HistorianValue value)
    {
        if (m_count <= 0)
        {
            key.Timestamp = 0;
            key.PointID = 0;
            value.Value3 = 0;
            value.Value1 = 0;
            return false;
        }

        m_count--;
        key.Timestamp = m_key1(m_start);
        key.PointID = m_key2(m_start);
        value.Value3 = m_value1(m_start);
        value.Value1 = m_value2(m_start);
        m_start++;
        return true;
    }

    #endregion
}

public static class PointStreamHelpers
{
    #region [ Static ]

    //public static bool AreEqual(this IStream256 source, IStream256 destination)
    //{
    //    bool isValidA, isValidB;
    //    ulong akey1, akey2, avalue1, avalue2;
    //    ulong bkey1, bkey2, bvalue1, bvalue2;

    //    while (true)
    //    {
    //        isValidA = source.Read(out akey1, out akey2, out avalue1, out avalue2);
    //        isValidB = destination.Read(out bkey1, out bkey2, out bvalue1, out bvalue2);

    //        if (isValidA != isValidB)
    //            return false;
    //        if (isValidA && isValidB)
    //        {
    //            if (akey1 != bkey1)
    //                return false;
    //            if (akey2 != bkey2)
    //                return false;
    //            if (avalue1 != bvalue1)
    //                return false;
    //            if (avalue2 != bvalue2)
    //                return false;
    //        }
    //        else
    //        {
    //            return true;
    //        }
    //    }
    //}

    //public static long Count(this IStream256 stream)
    //{
    //    long x = 0;
    //    ulong akey1, akey2, avalue1, avalue2;
    //    while (stream.Read(out akey1, out akey2, out avalue1, out avalue2))
    //        x++;
    //    return x;
    //}
    //public static long Count(this Stream256Base stream)
    //{
    //    long x = 0;
    //    while (stream.Read())
    //        x++;
    //    return x;
    //}

    public static long Count<TKey, TValue>(this TreeStream<TKey, TValue> stream) where TKey : class, new() where TValue : class, new()
    {
        TKey key = new();
        TValue value = new();
        long x = 0;
        while (stream.Read(key, value))
            x++;
        return x;
    }

    public static long Count(this SortedTreeTable<HistorianKey, HistorianValue> stream)
    {
        using SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> read = stream.BeginRead();
        SortedTreeScannerBase<HistorianKey, HistorianValue> scan = read.GetTreeScanner();
        scan.SeekToStart();
        return scan.Count();
    }

    #endregion
}