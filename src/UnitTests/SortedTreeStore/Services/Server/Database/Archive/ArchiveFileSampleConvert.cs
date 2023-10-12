//******************************************************************************************************
//  ArchiveFileSampleConvert.cs - Gbtc
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
using System.IO;
using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Services.Server.Database.Archive;

[TestFixture]
public class ArchiveFileSampleConvert
{
    #region [ Methods ]

    // NOTE: Write test must happen before read. ReSharper will run tests in alphabetical order,
    // so ensure test names reflect this needed order.

    [Test]
    public void StepA_WriteFile()
    {
        HistorianKey key = new();
        HistorianValue value = new();

        if (File.Exists("c:\\temp\\ArchiveTestFileBig.d2"))
            File.Delete("c:\\temp\\ArchiveTestFileBig.d2");

        //using (var af = ArchiveFile.CreateInMemory(CompressionMethod.TimeSeriesEncoded))
        using SortedTreeFile af = SortedTreeFile.CreateFile("c:\\temp\\ArchiveTestFileBig.d2");
        using SortedTreeTable<HistorianKey, HistorianValue> af2 = af.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        Random r = new(3);

        for (ulong v1 = 1; v1 < 36; v1++)
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = af2.BeginEdit())
            {
                for (ulong v2 = 1; v2 < 86000; v2++)
                {
                    key.Timestamp = v1 * 2342523;
                    key.PointID = v2;
                    value.Value1 = (ulong)r.Next();
                    value.Value3 = 0;

                    edit.AddPoint(key, value);
                }

                edit.Commit();
            }

            af2.Count();
        }

        af2.Count();
    }

    //[Test]
    //public void WriteFile2()
    //{
    //    using (var af = ArchiveFile.CreateInMemory(CompressionMethod.None))
    //    //using (var af = ArchiveFile.CreateInMemory(CompressionMethod.TimeSeriesEncoded))
    //    //using (var af = ArchiveFile.CreateFile("c:\\temp\\ArchiveTestFileBig.d2", CompressionMethod.TimeSeriesEncoded))
    //    {
    //        Random r = new Random(3);

    //        for (ulong v1 = 1; v1 < 360; v1++)
    //        {
    //            long cnt = af.Count();
    //            using (var edit = af.BeginEdit())
    //            {
    //                for (ulong v2 = 1; v2 < 28; v2++)
    //                {
    //                    Assert.AreEqual(cnt, af.Count());
    //                    if (v1 == 128)
    //                        v1 = v1;

    //                    edit.AddPoint(v1 * 2342523, v2, 0, (ulong)r.Next());
    //                    Assert.AreEqual(cnt, af.Count());
    //                    Assert.AreEqual(cnt + (long)v2, edit.GetRange().Count());
    //                }
    //                edit.Commit();
    //            }
    //            af.Count();
    //        }
    //        af.Count();
    //    }
    //}

    //[Test]
    //public void WriteFile2()
    //{
    //    if (File.Exists("c:\\temp\\ArchiveTestFileBig.d2"))
    //        File.Delete("c:\\temp\\ArchiveTestFileBig.d2");
    //    using (var af = ArchiveFile.CreateFile("c:\\temp\\ArchiveTestFileBig.d2", CompressionMethod.DeltaEncoded))
    //    {
    //        Random r = new Random(3);
    //        using (var edit = af.BeginEdit())
    //        {
    //            for (ulong v1 = 1; v1 < 36; v1++)
    //            {

    //                for (ulong v2 = 1; v2 < 86000; v2++)
    //                {
    //                    edit.AddPoint(v1 * 2342523, v2, 0, (ulong)r.Next());
    //                }

    //            }
    //            edit.Commit();
    //        }

    //    }
    //}

    [Test]
    public void StepB_ReadFile()
    {
        using SortedTreeFile af = SortedTreeFile.OpenFile("c:\\temp\\ArchiveTestFileBig.d2", true);
        using SortedTreeTable<HistorianKey, HistorianValue> af2 = af.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding);
        HistorianKey key = new();
        HistorianValue value = new();
        Random r = new(3);

        SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = af2.AcquireReadSnapshot().CreateReadSnapshot().GetTreeScanner();
        scanner.SeekToStart();
        for (ulong v1 = 1; v1 < 36; v1++)
        for (ulong v2 = 1; v2 < 86000; v2++)
        {
            Assert.IsTrue(scanner.Read(key, value));
            Assert.AreEqual(key.Timestamp, v1 * 2342523);
            Assert.AreEqual(key.PointID, v2);
            Assert.AreEqual(value.Value3, 0ul);
            Assert.AreEqual(value.Value1, (ulong)r.Next());
        }
    }

    #endregion
}