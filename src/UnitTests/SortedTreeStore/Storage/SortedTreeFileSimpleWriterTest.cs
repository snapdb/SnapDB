﻿//******************************************************************************************************
//  SortedTreeFileSimpleWriterTest.cs - Gbtc
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
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.Snap;
using SnapDB.Snap.Collection;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Storage;

[TestFixture]
public class SortedTreeFileSimpleWriterTest
{
    #region [ Methods ]

    [Test]
    public void TestOld()
    {
        Test(1000, false);

        int pointCount = 10000000;
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointID = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        File.Delete(@"C:\Temp\fileTemp.~d2i");
        File.Delete(@"C:\Temp\fileTemp.d2i");

        Stopwatch sw = new();
        sw.Start();

        using (SortedTreeFile file = SortedTreeFile.CreateFile(@"C:\Temp\fileTemp.~d2i"))
        using (SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = table.BeginEdit())
            {
                edit.AddPoints(points);
                edit.Commit();
            }
        }

        //SortedTreeFileSimpleWriter<HistorianKey, HistorianValue>.Create(@"C:\Temp\fileTemp.~d2i", @"C:\Temp\fileTemp.d2i", 4096, SortedTree.FixedSizeNode, points);

        sw.Stop();

        Console.WriteLine(SimplifiedSubFileStreamIoSession.ReadBlockCount);
        Console.WriteLine(SimplifiedSubFileStreamIoSession.WriteBlockCount);
        Console.WriteLine(sw.Elapsed.TotalSeconds.ToString());
    }

    [Test]
    public void CountIo()
    {
        Test(1000, false);

        int pointCount = 10000000;
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointID = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        File.Delete(@"C:\Temp\fileTemp.~d2i");
        File.Delete(@"C:\Temp\fileTemp.d2i");

        Stopwatch sw = new();
        sw.Start();

        SortedTreeFileSimpleWriter<HistorianKey, HistorianValue>.Create(@"C:\Temp\fileTemp.~d2i", @"C:\Temp\fileTemp.d2i", 4096, null, EncodingDefinition.FixedSizeCombinedEncoding, points);

        sw.Stop();

        Console.WriteLine(SimplifiedSubFileStreamIoSession.ReadBlockCount);
        Console.WriteLine(SimplifiedSubFileStreamIoSession.WriteBlockCount);
        Console.WriteLine(sw.Elapsed.TotalSeconds.ToString());
    }


    [Test]
    public void Test()
    {
        for (int x = 1; x < 1000000; x *= 2)
        {
            Test(x, true);
            Console.WriteLine(x);
        }
    }

    public void Test(int pointCount, bool verify)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointID = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        File.Delete(@"C:\Temp\fileTemp.~d2i");
        File.Delete(@"C:\Temp\fileTemp.d2i");

        SortedTreeFileSimpleWriter<HistorianKey, HistorianValue>.Create(@"C:\Temp\fileTemp.~d2i", @"C:\Temp\fileTemp.d2i", 4096, null, EncodingDefinition.FixedSizeCombinedEncoding, points);
        if (!verify)
            return;
        using SortedTreeFile file = SortedTreeFile.OpenFile(@"C:\Temp\fileTemp.d2i", true);
        using SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenTable<HistorianKey, HistorianValue>();
        using SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> read = table.AcquireReadSnapshot().CreateReadSnapshot();
        using (SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = read.GetTreeScanner())
        {
            scanner.SeekToStart();
            int cnt = 0;
            while (scanner.Read(key, value))
            {
                if (key.PointID != (ulong)cnt)
                    throw new Exception();
                cnt++;
            }

            if (cnt != pointCount)
                throw new Exception();
        }
    }

    [Test]
    public void TestNonSequential()
    {
        for (int x = 1; x < 1000000; x *= 2)
        {
            TestNonSequential(x, true);
            Console.WriteLine(x);
        }
    }

    public void TestNonSequential(int pointCount, bool verify)
    {
        SortedPointBuffer<HistorianKey, HistorianValue> points = new(pointCount, true);

        HistorianKey key = new();
        HistorianValue value = new();

        for (int x = 0; x < pointCount; x++)
        {
            key.PointID = (ulong)x;
            points.TryEnqueue(key, value);
        }

        points.IsReadingMode = true;

        File.Delete(@"C:\Temp\fileTemp.~d2i");
        File.Delete(@"C:\Temp\fileTemp.d2i");

        SortedTreeFileSimpleWriter<HistorianKey, HistorianValue>.CreateNonSequential(@"C:\Temp\fileTemp.~d2i", @"C:\Temp\fileTemp.d2i", 4096, null, EncodingDefinition.FixedSizeCombinedEncoding, points);
        if (!verify)
            return;
        using SortedTreeFile file = SortedTreeFile.OpenFile(@"C:\Temp\fileTemp.d2i", true);
        using SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenTable<HistorianKey, HistorianValue>();
        using SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> read = table.AcquireReadSnapshot().CreateReadSnapshot();
        using (SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = read.GetTreeScanner())
        {
            scanner.SeekToStart();
            int cnt = 0;
            while (scanner.Read(key, value))
            {
                if (key.PointID != (ulong)cnt)
                    throw new Exception();
                cnt++;
            }

            if (cnt != pointCount)
                throw new Exception();
        }
    }

    #endregion
}