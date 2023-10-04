//******************************************************************************************************
//  DelMeTest.cs - Gbtc
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
using System;

namespace UnitTests;

[TestFixture]
public class DelMeTest
{

    //[Test]
    //public void SlowReading()
    //{
    //    RemoteClientOptions clientOptions = new RemoteClientOptions();
    //    clientOptions.NetworkPort = 38402;
    //    clientOptions.ServerNameOrIp = "127.0.0.1";
    //    clientOptions.DefaultDatabase = "PPA";

    //    using (var server = new HistorianClient(clientOptions))
    //    {
    //        using (var database = server.GetDefaultDatabase<HistorianKey, HistorianValue>())
    //        {
    //            var stream = database.Read();
    //            while (stream.Read())
    //                ;
    //            //System.Threading.Thread.Sleep(1);
    //        }
    //    }
    //}

    [Test]
    public void Test()
    {
        //throw new NotImplementedException();
        //HistorianDatabaseInstance db = new HistorianDatabaseInstance();
        //db.InMemoryArchive = true;
        //db.ConnectionString = "port=12345";
        //db.Paths = new[] { @"c:\temp\Tulsa Bank 1 LTC 1.d2" };

        //using (HistorianServer server = new HistorianServer(db))
        //{
        //    DateTime start = DateTime.Parse("4/17/2013 10:38 AM");
        //    DateTime stop = DateTime.Parse("4/17/2013 10:38 AM");
        //    //DateTime stop = DateTime.Parse("4/17/2013 11:00 PM");
        //    var database = server.GetDefaultDatabase();

        //    ////var reader = database.GetRawSignals(start, stop, new ulong[] { 3142011, 3142023 });
        //    //var reader = database.GetRawSignals(start, stop, new ulong[] {  3142023 });
        //    ////var reader = database.GetRawSignals(start, stop, new ulong[] { 3142011 });
        //    //Console.WriteLine(reader.Count);

        //    var stream = database.Read(start, stop, new ulong[] { 3142023 });
        //    while (stream.Read())
        //        Console.WriteLine(stream.CurrentKey.Timestamp.ToString() + '\t' + stream.CurrentKey.TimestampAsDate.ToString() + '\t' +
        //            stream.CurrentValue.Value1.ToString());
        //}
    }

    [Test]
    public void ReadDataFromAFile()
    {
        string fileName = @"c:\temp\Tulsa Bank 1 LTC 1.d2";
        DateTime start = DateTime.Parse("4/17/2013 10:38 AM");
        DateTime stop = DateTime.Parse("4/17/2013 10:38 AM");

        HistorianKey key = new HistorianKey();
        HistorianValue value = new HistorianValue();

        using (SortedTreeFile file = SortedTreeFile.OpenFile(fileName, isReadOnly: true))
        using (SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenTable<HistorianKey, HistorianValue>())
        using (SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> snapshot = table.BeginRead())
        {
            SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = snapshot.GetTreeScanner();
            HistorianKey seekKey = new HistorianKey();
            seekKey.TimestampAsDate = start;
            seekKey.PointID = 3142023;
            scanner.SeekToKey(seekKey);
            while (scanner.Read(key,value) && key.TimestampAsDate <= stop)
            {
                Console.WriteLine("{0}, {1}, {2}",
                    key.TimestampAsDate.ToString(), key.PointID, value.AsString);
            }
        }
    }


}
