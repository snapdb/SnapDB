//******************************************************************************************************
//  SequentialReaderStreamTest.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
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
//  10/29/2026 - Preston Crawford
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Filters;
using SnapDB.Snap.Services;
using SnapDB.Snap.Services.Reader;
using SnapDB.Snap.Storage;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Services.Reader;

[TestFixture]
public class SequentialReaderStreamTest
{
    #region [ Methods ]

    [Test]
    public void SeekFailureReleasesAcquiredResources()
    {
        const ulong PointCount = 2000;
        const ulong FailingTimestamp = PointCount / 2;
        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d2i");
        SortedTreeFile file = null;
        ArchiveList<HistorianKey, HistorianValue> list = null;

        try
        {
            CreateFile(fileName, PointCount);

            // Opening the table reads only its first and last leaves, so a corrupt leaf in the middle lets the table
            // and its stream open, and the read fails when the reader seeks to the start of the query range.
            CorruptLeafContaining(fileName, FailingTimestamp);

            file = SortedTreeFile.OpenFile(fileName, true);
            SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenTable<HistorianKey, HistorianValue>();
            list = new ArchiveList<HistorianKey, HistorianValue>();

            using (ArchiveListEditor<HistorianKey, HistorianValue> editor = list.AcquireEditLock())
                editor.Add(table);

            SeekFilterBase<HistorianKey> range = TimestampSeekFilter.CreateFromRange<HistorianKey>(FailingTimestamp, PointCount);
            Exception ex = Assert.Catch(() => new SequentialReaderStream<HistorianKey, HistorianValue>(list, null, range));
            Assert.That(ex.Message, Does.StartWith("Read Error"));

            // A leaked snapshot keeps the file from being deleted, and a leaked I/O session keeps it open.
            Assert.That(list.IsFileBeingUsed(table), Is.False);
            Assert.That(HelperFunctions.IsFileOpen(fileName), Is.False);
        }
        finally
        {
            // Disposing the list waits, without a timeout, for every snapshot to be released. Bound the wait so a
            // leaked snapshot fails the assertions above instead of hanging the test run.
            if (list is not null)
                Task.Run(list.Dispose).Wait(TimeSpan.FromSeconds(10));

            file?.Dispose();
            HelperFunctions.TryDeleteFile(fileName);
        }
    }

    private static void CreateFile(string fileName, ulong pointCount)
    {
        HistorianKey key = new();
        HistorianValue value = new();

        using SortedTreeFile file = SortedTreeFile.CreateFile(fileName);
        SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        using SortedTreeTableEditor<HistorianKey, HistorianValue> editor = table.BeginEdit();

        for (ulong x = 0; x < pointCount; x++)
        {
            key.Timestamp = x;
            key.PointID = 1;
            value.Value1 = x;
            editor.AddPoint(key, value);
        }

        editor.Commit();
    }

    private static unsafe void CorruptLeafContaining(string fileName, ulong timestamp)
    {
        // Fixed size encoding stores each point as its raw key followed by its raw value. Index nodes hold keys
        // without values, so the full point only appears in the leaf that contains it.
        HistorianKey key = new() { Timestamp = timestamp, PointID = 1 };
        HistorianValue value = new() { Value1 = timestamp };
        byte[] point = new byte[key.Size + value.Size];

        fixed (byte* ptr = point)
        {
            key.Write(ptr);
            value.Write(ptr + key.Size);
        }

        byte[] data = File.ReadAllBytes(fileName);
        int position = data.AsSpan().IndexOf(point);
        Assert.That(position, Is.GreaterThan(0));
        Assert.That(data.AsSpan(position + 1).IndexOf(point), Is.EqualTo(-1), "Point must appear in exactly one block.");

        // Changing any byte makes the block fail its checksum when it is read.
        data[position] ^= 0xFF;
        File.WriteAllBytes(fileName, data);
    }

    #endregion
}