﻿//******************************************************************************************************
//  TestPageSizesBulk.cs - Gbtc
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SnapDB.IO.FileStructure.Media;
using SnapDB.Snap;
using SnapDB.Snap.Storage;
using SnapDB.UnitTests.Snap;
using SnapDB.UnitTests.Snap.Definitions;

namespace SnapDB.UnitTests.SortedTreeStore.Storage;

[TestFixture]
public class TestPageSizesBulk
{
    #region [ Members ]

    //TestResults Test(int pageSize)
    //{
    //    GSF.Stats.LookupKeys = 0;
    //    DiskIoSession.ReadCount = 0;
    //    DiskIoSession.WriteCount = 0;
    //    Statistics.ChecksumCount = 0;
    //    DiskMediumIoSession.Lookups = 0;
    //    DiskMediumIoSession.CachedLookups = 0;
    //    long cnt;
    //    var sw = new Stopwatch();
    //    var sw2 = new Stopwatch();
    //    sw.Start();
    //    using (var af = ArchiveFile.CreateInMemory(CompressionMethod.TimeSeriesEncoded2, pageSize))
    //    {
    //        using (var edit = af.BeginEdit())
    //        {
    //            for (int x = 0; x < 10; x++)
    //            {
    //                edit.AddPoints(new PointStreamSequential(x * 10000, 10000));
    //            }
    //            edit.Commit();
    //        }
    //        sw.Stop();
    //        sw2.Start();
    //        cnt = af.Count();
    //        sw2.Stop();
    //    }
    //    return new TestResults()
    //        {
    //            Count = cnt,
    //            PageSize = pageSize,
    //            RateWrite = (float)(1 / sw.Elapsed.TotalSeconds),
    //            RateRead = (float)(1 / sw2.Elapsed.TotalSeconds),
    //            ReadCount = DiskIoSession.ReadCount,
    //            WriteCount = DiskIoSession.WriteCount,
    //            ChecksumCount = Statistics.ChecksumCount,
    //            Lookups = DiskMediumIoSession.Lookups,
    //            CachedLookups = DiskMediumIoSession.CachedLookups
    //        };
    //}

    public class TestResults
    {
        #region [ Members ]

        public long CachedLookups;
        public long ChecksumCount;
        public long Count;
        public long Lookups;
        public int PageSize;
        public float RateRead;
        public float RateWrite;
        public long ReadCount;
        public long WriteCount;

        #endregion
    }

    #endregion

    #region [ Methods ]

    [Test]
    public void Test4096()
    {
        List<TestResults> lst = new();
        Test(512);
        lst.Add(Test(512));
        lst.Add(Test(1024));
        lst.Add(Test(2048));
        lst.Add(Test(4096));
        lst.Add(Test(4096 << 1));
        lst.Add(Test(4096 << 2));
        lst.Add(Test(4096 << 3));
        lst.Add(Test(4096 << 4));

        Console.Write("Count\t");
        lst.ForEach(x => Console.Write(x.Count.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Size\t");
        lst.ForEach(x => Console.Write(x.PageSize.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Rate Write\t");
        lst.ForEach(x => Console.Write(x.RateWrite.ToString("0.000") + '\t'));
        Console.WriteLine();
        Console.Write("Rate Read\t");
        lst.ForEach(x => Console.Write(x.RateRead.ToString("0.000") + '\t'));
        Console.WriteLine();
        Console.Write("Read\t");
        lst.ForEach(x => Console.Write(x.ReadCount.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Write\t");
        lst.ForEach(x => Console.Write(x.WriteCount.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Checksum\t");
        lst.ForEach(x => Console.Write(x.ChecksumCount.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Lookups\t");
        lst.ForEach(x => Console.Write(x.Lookups.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Cached\t");
        lst.ForEach(x => Console.Write(x.CachedLookups.ToString() + '\t'));
        Console.WriteLine();


        //string fileName = @"c:\temp\testFile.d2";
        //TestFile(1024, fileName);
        //TestFile(2048, fileName);
        //TestFile(4096, fileName);
        //TestFile(4096 << 1, fileName);
        //TestFile(4096 << 2, fileName);
        //TestFile(4096 << 3, fileName);
        //TestFile(4096 << 4, fileName);
    }

    [Test]
    public void TestBulkRolloverFile()
    {
        Stats.LookupKeys = 0;
        DiskIoSession.ReadCount = 0;
        DiskIoSession.WriteCount = 0;
        Stats.ChecksumCount = 0;
        DiskIoSession.Lookups = 0;
        DiskIoSession.CachedLookups = 0;
        long cnt;
        Stopwatch sw = new();
        sw.Start();
        //using (SortedTreeTable<HistorianKey, HistorianValue> af = SortedTreeFile.CreateInMemory(4096).OpenOrCreateTable<HistorianKey, HistorianValue>(SortedTree.FixedSizeNode))
        using (SortedTreeTable<HistorianKey, HistorianValue> af = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(HistorianFileEncodingDefinition.TypeGuid))
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af.BeginEdit())
            {
                edit.AddPoints(new PointStreamSequentialPoints(1, 20000000));
                edit.Commit();
            }

            sw.Stop();

            cnt = af.Count();
            Console.WriteLine(cnt);
        }

        Console.WriteLine((float)(20 / sw.Elapsed.TotalSeconds));
    }

    private TestResults Test(int pageSize)
    {
        Stats.LookupKeys = 0;
        DiskIoSession.ReadCount = 0;
        DiskIoSession.WriteCount = 0;
        Stats.ChecksumCount = 0;
        DiskIoSession.Lookups = 0;
        DiskIoSession.CachedLookups = 0;
        long cnt;
        Stopwatch sw = new();
        Stopwatch sw2 = new();
        sw.Start();
        using (SortedTreeTable<HistorianKey, HistorianValue> af = SortedTreeFile.CreateInMemory(pageSize).OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af.BeginEdit())
            {
                for (int x = 0; x < 100; x++)
                    edit.AddPoints(new PointStreamSequential(x * 10000, 10000));
                edit.Commit();
            }

            sw.Stop();
            sw2.Start();
            cnt = af.Count();
            sw2.Stop();
        }

        return new TestResults
        {
            Count = cnt,
            PageSize = pageSize,
            RateWrite = (float)(1 / sw.Elapsed.TotalSeconds),
            RateRead = (float)(1 / sw2.Elapsed.TotalSeconds),
            ReadCount = DiskIoSession.ReadCount,
            WriteCount = DiskIoSession.WriteCount,
            ChecksumCount = Stats.ChecksumCount,
            Lookups = DiskIoSession.Lookups,
            CachedLookups = DiskIoSession.CachedLookups
        };
    }

    private void TestFile(int pageSize, string fileName)
    {
        HistorianKey key = new();
        HistorianValue value = new();
        key.Timestamp = 1;

        if (File.Exists(fileName))
            File.Delete(fileName);
        Stopwatch sw = new();
        sw.Start();
        using (SortedTreeTable<HistorianKey, HistorianValue> af = SortedTreeFile.CreateFile(fileName, pageSize).OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af.BeginEdit())
        {
            for (uint x = 0; x < 1000000; x++)
            {
                key.PointID = x;
                edit.AddPoint(key, value);
            }

            edit.Commit();
        }

        sw.Stop();
        Console.WriteLine("Size: " + pageSize + " Rate: " + 1 / sw.Elapsed.TotalSeconds);
    }

    #endregion
}