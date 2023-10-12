//******************************************************************************************************
//  TestPageSizes.cs - Gbtc
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
using SnapDB.IO.Unmanaged;
using SnapDB.Snap;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;
using SnapDB.UnitTests.Snap.Definitions;

namespace SnapDB.UnitTests.SortedTreeStore.Storage;

[TestFixture]
public class TestPageSizes
{
    #region [ Members ]

    public class TestResults
    {
        #region [ Members ]

        public long CachedLookups;
        public long ChecksumCount;
        public long Lookups;
        public int PageSize;
        public float Rate;
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

        Console.Write("Size\t");
        lst.ForEach(x => Console.Write(x.PageSize.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Rate\t");
        lst.ForEach(x => Console.Write(x.Rate.ToString("0.000") + '\t'));
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
    }

    [Test]
    public void TestWriteFile()
    {
        string fileName = @"c:\temp\testFile.d2";
        TestFile(4096, fileName);
    }

    [Test]
    public void Test4096RandomHuge()
    {
        const uint count = 200000;
        List<TestResults> lst = new();
        //TestRandom(4096, count);
        //lst.Add(TestRandom(4096, count));
        lst.Add(TestRandom(4096, count));

        Console.Write("Size\t");
        lst.ForEach(x => Console.Write(x.PageSize.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Rate\t");
        lst.ForEach(x => Console.Write(x.Rate.ToString("0.000") + '\t'));
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
    public void Test4096Random()
    {
        const uint count = 100000;
        List<TestResults> lst = new();
        TestRandom(512, count);
        lst.Add(TestRandom(512, count));
        lst.Add(TestRandom(1024, count));
        lst.Add(TestRandom(2048, count));
        lst.Add(TestRandom(4096, count));
        lst.Add(TestRandom(4096 << 1, count));
        lst.Add(TestRandom(4096 << 2, count));
        lst.Add(TestRandom(4096 << 3, count));
        lst.Add(TestRandom(4096 << 4, count));

        Console.Write("Size\t");
        lst.ForEach(x => Console.Write(x.PageSize.ToString() + '\t'));
        Console.WriteLine();
        Console.Write("Rate\t");
        lst.ForEach(x => Console.Write(x.Rate.ToString("0.000") + '\t'));
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

    private TestResults Test(int pageSize)
    {
        //StringBuilder sb = new StringBuilder();
        HistorianKey key = new();
        HistorianValue value = new();
        DiskIoSession.ReadCount = 0;
        DiskIoSession.WriteCount = 0;
        Stats.ChecksumCount = 0;
        DiskIoSession.Lookups = 0;
        DiskIoSession.CachedLookups = 0;

        Stopwatch sw = new();
        sw.Start();
        using (SortedTreeFile af = SortedTreeFile.CreateInMemory(pageSize))
        using (SortedTreeTable<HistorianKey, HistorianValue> af2 = af.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af2.BeginEdit())
            {
                for (ulong x = 0; x < 1000000; x++)
                {
                    key.Timestamp = x;
                    key.PointID = 2 * x;
                    value.Value3 = 3 * x;
                    value.Value1 = 4 * x;
                    //if ((x % 100) == 0)
                    //    sb.AppendLine(x + "," + DiskIoSession.ReadCount + "," + DiskIoSession.WriteCount);
                    //if (x == 1000)
                    //    DiskIoSession.BreakOnIO = true;
                    edit.AddPoint(key, value);
                    //edit.AddPoint(uint.MaxValue - x, 2 * x, 3 * x, 4 * x);
                }

                edit.Commit();
            }
            //long cnt = af.Count();
        }

        sw.Stop();

        //File.WriteAllText(@"C:\temp\" + pageSize + ".csv",sb.ToString());


        return new TestResults
        {
            PageSize = pageSize,
            Rate = (float)(1 / sw.Elapsed.TotalSeconds),
            ReadCount = DiskIoSession.ReadCount,
            WriteCount = DiskIoSession.WriteCount,
            ChecksumCount = Stats.ChecksumCount,
            Lookups = DiskIoSession.Lookups,
            CachedLookups = DiskIoSession.CachedLookups
        };
    }

    private TestResults TestRandom(int pageSize, uint count)
    {
        //StringBuilder sb = new StringBuilder();
        Random r = new(1);
        HistorianKey key = new();
        HistorianValue value = new();
        DiskIoSession.ReadCount = 0;
        DiskIoSession.WriteCount = 0;
        Stats.ChecksumCount = 0;
        DiskIoSession.Lookups = 0;
        DiskIoSession.CachedLookups = 0;

        Stopwatch sw = new();
        sw.Start();
        using (SortedTreeFile af = SortedTreeFile.CreateInMemory(pageSize))
        using (SortedTreeTable<HistorianKey, HistorianValue> af2 = af.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
            uint pointPairs = count / 5000;
            for (uint i = 0; i < pointPairs; i++)
            {
                uint max = i * 5000 + 5000;
                using SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af2.BeginEdit();
                for (ulong x = i * 5000; x < max; x++)
                {
                    key.Timestamp = (uint)r.Next();
                    key.PointID = 2 * x;
                    value.Value3 = 3 * x;
                    value.Value1 = 4 * x;
                    //if ((x % 100) == 0)
                    //    sb.AppendLine(x + "," + DiskIoSession.ReadCount + "," + DiskIoSession.WriteCount);
                    //if (x == 1000)
                    //    DiskIoSession.BreakOnIO = true;
                    edit.AddPoint(key, value);
                    //edit.AddPoint(uint.MaxValue - x, 2 * x, 3 * x, 4 * x);
                }

                edit.Commit();
            }
            //long cnt = af.Count();
        }

        sw.Stop();

        //File.WriteAllText(@"C:\temp\" + pageSize + ".csv",sb.ToString());


        return new TestResults
        {
            PageSize = pageSize,
            Rate = (float)(count / sw.Elapsed.TotalSeconds / 1000000),
            ReadCount = DiskIoSession.ReadCount,
            WriteCount = DiskIoSession.WriteCount,
            ChecksumCount = Stats.ChecksumCount,
            Lookups = DiskIoSession.Lookups,
            CachedLookups = DiskIoSession.CachedLookups
        };
    }

    private TestResults TestRandomBinaryStream(int pageSize, uint count)
    {
        //StringBuilder sb = new StringBuilder();
        Random r = new(1);
        HistorianKey key = new();
        HistorianValue value = new();
        DiskIoSession.ReadCount = 0;
        DiskIoSession.WriteCount = 0;
        Stats.ChecksumCount = 0;
        DiskIoSession.Lookups = 0;
        DiskIoSession.CachedLookups = 0;

        Stopwatch sw = new();
        sw.Start();

        using (BinaryStream bs = new(true))
        {
            SortedTree<HistorianKey, HistorianValue> table = SortedTree<HistorianKey, HistorianValue>.Create(bs, 4096);
            for (ulong x = 0; x < count; x++)
            {
                key.Timestamp = (uint)r.Next();
                key.PointID = 2 * x;
                value.Value3 = 3 * x;
                value.Value1 = 4 * x;
                //if ((x % 100) == 0)
                //    sb.AppendLine(x + "," + DiskIoSession.ReadCount + "," + DiskIoSession.WriteCount);
                //if (x == 1000)
                //    DiskIoSession.BreakOnIO = true;
                table.TryAdd(key, value);
                //edit.AddPoint(uint.MaxValue - x, 2 * x, 3 * x, 4 * x);
            }
        }

        sw.Stop();

        //File.WriteAllText(@"C:\temp\" + pageSize + ".csv",sb.ToString());


        return new TestResults
        {
            PageSize = pageSize,
            Rate = (float)(count / sw.Elapsed.TotalSeconds / 1000000),
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
        HistorianValue value = new()
        {
            Value3 = 0,
            AsSingle = 65.20f
        };

        if (File.Exists(fileName))
            File.Delete(fileName);
        Stopwatch sw = new();
        sw.Start();
        using (SortedTreeFile af = SortedTreeFile.CreateFile(fileName, pageSize))
        using (SortedTreeTable<HistorianKey, HistorianValue> af2 = af.OpenOrCreateTable<HistorianKey, HistorianValue>(HistorianFileEncodingDefinition.TypeGuid))
        using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af2.BeginEdit())
        {
            for (uint x = 0; x < 10000000; x++)
            {
                key.Timestamp = 1;
                key.PointID = x;
                edit.AddPoint(key, value);
            }

            edit.Commit();
        }

        sw.Stop();
        Console.WriteLine("Size: " + pageSize + " Rate: " + 10 / sw.Elapsed.TotalSeconds);
    }

    #endregion
}