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
        SortedTreeFile first = null;
        SortedTreeFile second = null;

        try
        {
            SortedTreeTable<HistorianKey, HistorianValue> firstTable = CreateTable(firstFileName);
            first = firstTable.BaseFile;
            SortedTreeTable<HistorianKey, HistorianValue> secondTable = CreateTable(secondFileName);
            second = secondTable.BaseFile;
            List<ArchiveTableSummary<HistorianKey, HistorianValue>> tables = [new(firstTable), new(secondTable)];

            // Disposing the second file makes its stream fail to open part way through the constructor.
            second.Dispose();

            Assert.Catch(() => new UnionReader<HistorianKey, HistorianValue>(tables));

            // A leaked I/O session keeps its file open.
            Assert.That(IsFileOpen(firstFileName), Is.False);
        }
        finally
        {
            first?.Dispose();
            second?.Dispose();
            TryDeleteFile(firstFileName);
            TryDeleteFile(secondFileName);
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

    private static bool IsFileOpen(string fileName)
    {
        try
        {
            using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static void TryDeleteFile(string fileName)
    {
        // Cleanup must not replace the test's own failure.
        try
        {
            File.Delete(fileName);
        }
        catch (IOException)
        {
        }
    }

    #endregion
}