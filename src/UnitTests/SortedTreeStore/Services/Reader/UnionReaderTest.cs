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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Services;
using SnapDB.Snap.Services.Reader;
using SnapDB.Snap.Storage;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Services.Reader;

[TestFixture]
public class UnionReaderTest
{
    #region [ Members ]

    private int m_seed = 1;

    #endregion

    #region [ Methods ]

    [Test]
    public void Test200()
    {
        Test(200);
    }

    [Test]
    public void Test()
    {
        for (int x = 1; x < 1000; x *= 2)
            Test(x);
    }

    public void Test(int count)
    {
        List<SortedTreeTable<HistorianKey, HistorianValue>> lst = new();
        for (int x = 0; x < count; x++)
            lst.Add(CreateTable());

        using (UnionTreeStream<HistorianKey, HistorianValue> reader = new(lst.Select(x => new ArchiveTreeStreamWrapper<HistorianKey, HistorianValue>(x)), true))
        {
            HistorianKey key = new();
            HistorianValue value = new();
            Stopwatch sw = new();
            sw.Start();
            while (reader.Read(key, value))
                ;
            sw.Stop();
            Console.Write("{0}\t{1}\t{2}", count, sw.Elapsed.TotalSeconds, sw.Elapsed.TotalSeconds / count);
            Console.WriteLine();
        }

        lst.ForEach(x => x.Dispose());
    }

    [Test]
    public void ConstructorFailureDisposesOpenedStreams()
    {
        string firstFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d2i");
        string secondFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d2i");

        try
        {
            using SortedTreeTable<HistorianKey, HistorianValue> first = CreateTable(firstFileName);
            using SortedTreeTable<HistorianKey, HistorianValue> second = CreateTable(secondFileName);
            List<ArchiveTableSummary<HistorianKey, HistorianValue>> tables = [new(first), new(second)];

            // Disposing the second table makes its stream fail to open part way through the constructor.
            second.Dispose();

            Assert.Catch(() => new UnionReader<HistorianKey, HistorianValue>(tables));

            // A leaked I/O session on the first table would still be counted as a user of its file.
            Assert.That(GetStreamUsers(first), Is.Zero);
        }
        finally
        {
            File.Delete(firstFileName);
            File.Delete(secondFileName);
        }
    }

    private SortedTreeTable<HistorianKey, HistorianValue> CreateTable(string fileName = null)
    {
        Random r = new(m_seed++);
        HistorianKey key = new();
        HistorianValue value = new();
        SortedTreeFile file = fileName is null ? SortedTreeFile.CreateInMemory() : SortedTreeFile.CreateFile(fileName);
        SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);

        using SortedTreeTableEditor<HistorianKey, HistorianValue> edit = table.BeginEdit();
        for (int x = 0; x < 1000; x++)
        {
            key.Timestamp = (ulong)r.Next();
            key.PointID = (ulong)r.Next();
            key.EntryNumber = (ulong)r.Next();
            edit.AddPoint(key, value);
        }

        edit.Commit();


        return table;
    }

    private static int GetStreamUsers(SortedTreeTable<HistorianKey, HistorianValue> table)
    {
        object fileStructure = GetPrivateField(table.BaseFile, "m_fileStructure");
        object diskIo = GetPrivateField(fileStructure, "m_diskIo");
        object diskMedium = GetPrivateField(diskIo, "m_stream");
        object bufferedFile = GetPrivateField(diskMedium, "m_disk");
        object queue = GetPrivateField(bufferedFile, "m_queue");

        return (int)GetPrivateField(queue, "m_streamUsers");
    }

    private static object GetPrivateField(object instance, string name)
    {
        return instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    }

    #endregion
}