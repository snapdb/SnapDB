//******************************************************************************************************
//  VariousWritingSizes.cs - Gbtc
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
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;

namespace UnitTests.SortedTreeStore;

[TestFixture]
public class VariousWritingSizes
{
    [Test]
    public void TestSmall()
    {
        HistorianKey key = new HistorianKey();
        HistorianValue value = new HistorianValue();
        using (SortedTreeFile af = SortedTreeFile.CreateInMemory())
        using (SortedTreeTable<HistorianKey, HistorianValue> file = af.OpenOrCreateTable<HistorianKey, HistorianValue>(EncodingDefinition.FixedSizeCombinedEncoding))
        {
            using (SortedTreeTableEditor<HistorianKey, HistorianValue> edit = file.BeginEdit())
            {
              
                for (int x = 0; x < 10000000; x++)
                {
                    key.Timestamp = (ulong)x;
                    edit.AddPoint(key, value);
                }
                edit.Commit();
            }

            using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> read = file.BeginRead())
            using (SortedTreeScannerBase<HistorianKey, HistorianValue> scan = read.GetTreeScanner())
            {
                int count = 0;
                scan.SeekToStart();
                while (scan.Read(key,value))
                {
                    count++;
                }
                System.Console.WriteLine(count.ToString());
            }
        }
    }
}
