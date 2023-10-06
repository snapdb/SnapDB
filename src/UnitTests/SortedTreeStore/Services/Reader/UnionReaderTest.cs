//******************************************************************************************************
//  UnionReaderTest.cs - Gbtc
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

using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnitTests.SortedTreeStore.Services.Reader;

[TestFixture]
public class UnionReaderTest
{
    private int seed = 1;

    [Test]
    public void Test200()
    {
        Test(200);
    }

    [Test]
    public void Test()
    {
        for (int x = 1; x < 1000; x *= 2)
        {
            Test(x);
        }
    }

    public void Test(int count)
    {
        List<SortedTreeTable<HistorianKey, HistorianValue>> lst = new List<SortedTreeTable<HistorianKey, HistorianValue>>();
        for (int x = 0; x < count; x++)
        {
            lst.Add(CreateTable());
        }

        using (UnionTreeStream<HistorianKey, HistorianValue> reader = new UnionTreeStream<HistorianKey, HistorianValue>(lst.Select(x => new ArchiveTreeStreamWrapper<HistorianKey, HistorianValue>(x)), true))
        {
            HistorianKey key = new HistorianKey();
            HistorianValue value = new HistorianValue();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (reader.Read(key, value))
                ;
            sw.Stop();
            System.Console.Write("{0}\t{1}\t{2}", count, sw.Elapsed.TotalSeconds, sw.Elapsed.TotalSeconds / count);
            System.Console.WriteLine();
        }

        lst.ForEach(x => x.Dispose());
    }


    SortedTreeTable<HistorianKey, HistorianValue> CreateTable()
    {
        Random r = new Random(seed++);
        HistorianKey key = new HistorianKey();
        HistorianValue value = new HistorianValue();
        SortedTreeFile file = SortedTreeFile.CreateInMemory();
        SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);

        using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = table.BeginEdit())
        {
            for (int x = 0; x < 1000; x++)
            {
                key.Timestamp = (ulong)r.Next();
                key.PointID = (ulong)r.Next();
                key.EntryNumber = (ulong)r.Next();
                edit.AddPoint(key, value);
            }
            edit.Commit();
        }




        return table;
    }




}
