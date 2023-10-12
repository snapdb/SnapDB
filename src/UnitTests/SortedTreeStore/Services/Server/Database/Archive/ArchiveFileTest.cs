//******************************************************************************************************
//  ArchiveFileTest.cs - Gbtc
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
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Services.Server.Database.Archive;

/// <summary>
/// This is a test class for PartitionFileTest and is intended
/// to contain all PartitionFileTest Unit Tests
/// </summary>
[TestFixture]
public class ArchiveFileTest
{
    #region [ Methods ]

    /// <summary>
    /// A test for PartitionFile Constructor
    /// </summary>
    [Test]
    public void PartitionFileConstructorTest()
    {
        using (SortedTreeTable<HistorianKey, HistorianValue> target = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
        }
    }

    /// <summary>
    /// A test for AddPoint
    /// </summary>
    [Test]
    public void AddPointTest()
    {
        using SortedTreeTable<HistorianKey, HistorianValue> target = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        using SortedTreeTableEditor<HistorianKey, HistorianValue> fileEditor = target.BeginEdit();
        fileEditor.AddPoint(new HistorianKey(), new HistorianValue());
        fileEditor.Commit();
    }

    /// <summary>
    /// A test for AddPoint
    /// </summary>
    [Test]
    public void EnduranceTest()
    {
        HistorianKey key = new();
        HistorianValue value = new();
        using SortedTreeTable<HistorianKey, HistorianValue> target = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        for (uint x = 0; x < 100; x++)
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> fileEditor = target.BeginEdit())
            {
                for (int y = 0; y < 10; y++)
                {
                    key.Timestamp = x;
                    key.PointID = x;
                    value.Value1 = x;
                    value.Value3 = x;
                    fileEditor.AddPoint(key, value);
                    x++;
                }

                fileEditor.Commit();
            }

            Assert.AreEqual(target.FirstKey.Timestamp, 0);
            Assert.AreEqual(target.LastKey.Timestamp, x - 1);
        }
    }

    /// <summary>
    /// A test for CreateSnapshot
    /// </summary>
    [Test]
    public void CreateSnapshotTest()
    {
        HistorianKey key = new();
        HistorianValue value = new();
        key.Timestamp = 1;
        key.PointID = 2;
        value.Value1 = 3;
        value.Value2 = 4;
        using SortedTreeTable<HistorianKey, HistorianValue> target = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        ulong date = 1;
        ulong pointId = 2;
        ulong value1 = 3;
        ulong value2 = 4;
        SortedTreeTableSnapshotInfo<HistorianKey, HistorianValue> snap1;
        using (SortedTreeTableEditor<HistorianKey, HistorianValue> fileEditor = target.BeginEdit())
        {
            fileEditor.AddPoint(key, value);
            key.Timestamp++;
            fileEditor.AddPoint(key, value);
            snap1 = target.AcquireReadSnapshot();
            fileEditor.Commit();
        }

        SortedTreeTableSnapshotInfo<HistorianKey, HistorianValue> snap2 = target.AcquireReadSnapshot();

        using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> instance = snap1.CreateReadSnapshot())
        {
            SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = instance.GetTreeScanner();
            scanner.SeekToStart();
            Assert.AreEqual(false, scanner.Read(key, value));
        }

        using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> instance = snap2.CreateReadSnapshot())
        {
            SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = instance.GetTreeScanner();
            scanner.SeekToStart();
            Assert.AreEqual(true, scanner.Read(key, value));
            Assert.AreEqual(1uL, key.Timestamp);
            Assert.AreEqual(2uL, key.PointID);
            Assert.AreEqual(3uL, value.Value1);
            Assert.AreEqual(4uL, value.Value2);
        }

        Assert.AreEqual(1uL, target.FirstKey.Timestamp);
        Assert.AreEqual(2uL, target.LastKey.Timestamp);
    }

    /// <summary>
    /// A test for RollbackEdit
    /// </summary>
    [Test]
    public void RollbackEditTest()
    {
        HistorianKey key = new();
        HistorianValue value = new();
        key.Timestamp = 1;
        key.PointID = 2;
        value.Value1 = 3;
        value.Value2 = 4;

        using SortedTreeTable<HistorianKey, HistorianValue> target = SortedTreeFile.CreateInMemory().OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        ulong date = 1;
        ulong pointId = 2;
        ulong value1 = 3;
        ulong value2 = 4;
        SortedTreeTableSnapshotInfo<HistorianKey, HistorianValue> snap1;
        using (SortedTreeTableEditor<HistorianKey, HistorianValue> fileEditor = target.BeginEdit())
        {
            fileEditor.AddPoint(key, value);
            snap1 = target.AcquireReadSnapshot();
            fileEditor.Rollback();
        }

        SortedTreeTableSnapshotInfo<HistorianKey, HistorianValue> snap2 = target.AcquireReadSnapshot();

        using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> instance = snap1.CreateReadSnapshot())
        {
            SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = instance.GetTreeScanner();
            scanner.SeekToStart();
            Assert.AreEqual(false, scanner.Read(key, value));
        }

        using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> instance = snap2.CreateReadSnapshot())
        {
            SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = instance.GetTreeScanner();
            scanner.SeekToStart();
            Assert.AreEqual(false, scanner.Read(key, value));
        }
    }

    #endregion
}