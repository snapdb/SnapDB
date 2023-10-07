//******************************************************************************************************
//  SequentialSortedTreeWriter_Test.cs - Gbtc
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
//  10/09/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using NUnit.Framework;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap;
using SnapDB.Snap.Collection;
using SnapDB.Snap.Tree;
using SnapDB.Snap.Tree.Specialized;
using SnapDB.UnitTests.Snap;
using SnapDB.UnitTests.Snap.Definitions;

namespace SnapDB.UnitTests.SortedTreeStore.Tree.Specialized;

[TestFixture]
public class SequentialSortedTreeWriterTest
{
    #region [ Methods ]

    [Test]
    public void BenchmarkOld2()
    {
        BenchmarkOld2(100);
        BenchmarkOld2(1000);
        BenchmarkOld2(10000);
        BenchmarkOld2(100000);
        BenchmarkOld2(1000000);
    }

    public void BenchmarkOld2(int pointCount)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointId = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        Stopwatch sw = new();
        sw.Start();
        using (BinaryStream bs = new(true))
        {
            SortedTree<HistorianKey, HistorianValue> st = SortedTree<HistorianKey, HistorianValue>.Create(bs, 4096, HistorianFileEncodingDefinition.TypeGuid);

            st.AddRange(points);

            //SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 4096, SortedTree.FixedSizeNode, points);
        }

        sw.Stop();

        Console.WriteLine("Points {0}: {1}MPPS", pointCount, (pointCount / sw.Elapsed.TotalSeconds / 1000000).ToString("0.0"));
    }

    [Test]
    public void BenchmarkOld()
    {
        BenchmarkOld(100);
        BenchmarkOld(1000);
        BenchmarkOld(10000);
        BenchmarkOld(100000);
        BenchmarkOld(1000000);
    }

    public void BenchmarkOld(int pointCount)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointId = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        Stopwatch sw = new();
        sw.Start();
        using (BinaryStream bs = new(true))
        {
            SortedTree<HistorianKey, HistorianValue> st = SortedTree<HistorianKey, HistorianValue>.Create(bs, 4096, HistorianFileEncodingDefinition.TypeGuid);
            st.TryAddRange(points);
            //SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 4096, SortedTree.FixedSizeNode, points);
        }

        sw.Stop();

        Console.WriteLine("Points {0}: {1}MPPS", pointCount, (pointCount / sw.Elapsed.TotalSeconds / 1000000).ToString("0.0"));
    }

    [Test]
    public void Benchmark()
    {
        Benchmark(100);
        Benchmark(1000);
        Benchmark(10000);
        Benchmark(100000);
        Benchmark(1000000);
    }

    public void Benchmark(int pointCount)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointId = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        Stopwatch sw = new();
        sw.Start();
        using (BinaryStream bs = new(true))
            SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 4096, HistorianFileEncodingDefinition.TypeGuid, points);
        //SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 4096, SortedTree.FixedSizeNode, points);
        sw.Stop();

        Console.WriteLine("Points {0}: {1}MPPS", pointCount, (pointCount / sw.Elapsed.TotalSeconds / 1000000).ToString("0.0"));
    }

    [Test]
    public void Test()
    {
        for (int x = 1; x < 1000000; x *= 2)
        {
            Test(x);
            Console.WriteLine(x);
        }
    }

    public void Test(int pointCount)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);
        Random r = new(1);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointId = (ulong)r.Next();
            key.Timestamp = (ulong)r.Next();
            value.Value1 = key.PointId;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        using BinaryStream bs = new(true);
        //var tree = new SequentialSortedTreeWriter<HistorianKey, HistorianValue>(bs, 256, SortedTree.FixedSizeNode);
        //SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 512, CreateTsCombinedEncoding.TypeGuid, points);
        SequentialSortedTreeWriter<HistorianKey, HistorianValue>.Create(bs, 512, EncodingDefinition.FixedSizeCombinedEncoding, points);

        SortedTree<HistorianKey, HistorianValue> sts = SortedTree<HistorianKey, HistorianValue>.Open(bs);
        r = new Random(1);

        for (int x = 0; x < pointCount; x++)
        {
            key.PointId = (ulong)r.Next();
            key.Timestamp = (ulong)r.Next();
            sts.Get(key, value);
            if (value.Value1 != key.PointId)
                throw new Exception();
        }
    }

    #endregion
}