//******************************************************************************************************
//  SortedPointBufferTest.cs - Gbtc
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
//  2/5/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SnapDB.Snap.Collection;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Collection;

[TestFixture]
public class SortedPointBufferTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        const int maxCount = 1000;
        Stopwatch sw = new();
        SortedPointBuffer<HistorianKey, HistorianValue> buffer = new(maxCount, true);

        HistorianKey key = new();
        HistorianValue value = new();
        Random r = new(1);

        for (int x = 0; x < maxCount; x++)
        {
            key.Timestamp = (ulong)r.Next();
            key.PointId = (ulong)x;

            buffer.TryEnqueue(key, value);
        }

        sw.Start();
        buffer.IsReadingMode = true;
        sw.Stop();

        Console.WriteLine(sw.ElapsedMilliseconds);
        Console.WriteLine(maxCount / sw.Elapsed.TotalSeconds / 1000000);

        for (int x = 0; x < maxCount; x++)
        {
            buffer.ReadSorted(x, key, value);
            Console.WriteLine(key.Timestamp + "\t" + key.PointId);
        }
    }


    [Test]
    public void BenchmarkRandomData()
    {
        for (int x = 16; x < 1000 * 1000; x *= 2)
            BenchmarkRandomData(x);
    }

    public void BenchmarkRandomData(int pointCount)
    {
        Stopwatch sw = new();
        SortedPointBuffer<HistorianKey, HistorianValue> buffer = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        List<double> times = new();
        for (int cnt = 0; cnt < 10; cnt++)
        {
            Random r = new(1);
            buffer.IsReadingMode = false;
            for (int x = 0; x < pointCount; x++)
            {
                key.Timestamp = (ulong)r.Next();
                key.PointId = (ulong)x;

                buffer.TryEnqueue(key, value);
            }

            sw.Restart();
            buffer.IsReadingMode = true;
            sw.Stop();
            times.Add(sw.Elapsed.TotalSeconds);
        }

        times.Sort();
        Console.WriteLine("{0} points {1}ms {2} Million/second ", pointCount, times[5] * 1000, pointCount / times[5] / 1000000);
    }

    [Test]
    public void BenchmarkRandomDataRead()
    {
        for (int x = 16; x < 1000 * 1000; x *= 2)
            BenchmarkRandomDataRead(x);
    }

    public void BenchmarkRandomDataRead(int pointCount)
    {
        Stopwatch sw = new();
        SortedPointBuffer<HistorianKey, HistorianValue> buffer = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        List<double> times = new();
        for (int cnt = 0; cnt < 10; cnt++)
        {
            Random r = new(1);
            buffer.IsReadingMode = false;
            for (int x = 0; x < pointCount; x++)
            {
                key.Timestamp = (ulong)r.Next();
                key.PointId = (ulong)x;

                buffer.TryEnqueue(key, value);
            }

            buffer.IsReadingMode = true;
            sw.Restart();
            while (buffer.Read(key, value))
            {
            }

            sw.Stop();
            times.Add(sw.Elapsed.TotalSeconds);
        }

        times.Sort();
        Console.WriteLine("{0} points {1}ms {2} Million/second ", pointCount, times[5] * 1000, pointCount / times[5] / 1000000);
    }

    [Test]
    public void BenchmarkSortedData()
    {
        for (int x = 16; x < 1000 * 1000; x *= 2)
            BenchmarkSortedData(x);
    }

    public void BenchmarkSortedData(int pointCount)
    {
        Stopwatch sw = new();
        SortedPointBuffer<HistorianKey, HistorianValue> buffer = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        List<double> times = new();
        for (int cnt = 0; cnt < 10; cnt++)
        {
            buffer.IsReadingMode = false;
            for (int x = 0; x < pointCount; x++)
            {
                key.PointId = (ulong)x;

                buffer.TryEnqueue(key, value);
            }

            sw.Restart();
            buffer.IsReadingMode = true;
            sw.Stop();
            times.Add(sw.Elapsed.TotalSeconds);
        }

        times.Sort();
        Console.WriteLine("{0} points {1}ms {2} Million/second ", pointCount, times[5] * 1000, pointCount / times[5] / 1000000);
    }

    [Test]
    public void BenchmarkSortedDataRead()
    {
        for (int x = 16; x < 1000 * 1000; x *= 2)
            BenchmarkSortedDataRead(x);
    }

    public void BenchmarkSortedDataRead(int pointCount)
    {
        Stopwatch sw = new();
        SortedPointBuffer<HistorianKey, HistorianValue> buffer = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        List<double> times = new();
        for (int cnt = 0; cnt < 10; cnt++)
        {
            buffer.IsReadingMode = false;
            for (int x = 0; x < pointCount; x++)
            {
                key.PointId = (ulong)x;

                buffer.TryEnqueue(key, value);
            }

            buffer.IsReadingMode = true;
            sw.Restart();
            while (buffer.Read(key, value))
            {
            }

            sw.Stop();
            times.Add(sw.Elapsed.TotalSeconds);
        }

        times.Sort();
        Console.WriteLine("{0} points {1}ms {2} Million/second ", pointCount, times[5] * 1000, pointCount / times[5] / 1000000);
    }

    #endregion
}