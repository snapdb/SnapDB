//******************************************************************************************************
//  StreamingClientServerTest.cs - Gbtc
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

using System.Threading;
using Gemstone.Diagnostics;
using NUnit.Framework;
using SnapDB.IO;
using SnapDB.Security;
using SnapDB.Snap.Services;
using SnapDB.Snap.Services.Net;
using SnapDB.Snap.Storage;
using SnapDB.Snap.Tree;
using SnapDB.UnitTests.Snap;
using SnapDB.UnitTests.SortedTreeStore.Tree.Generic;

namespace SnapDB.UnitTests.SortedTreeStore.Services.Net;

[TestFixture]
public class StreamingClientServerTest
{
    [Test]
    public void Test1()
    {
        Logger.Console.Verbose = VerboseLevel.All;

        NetworkStreamSimulator netStream = new();

        HistorianServerDatabaseConfig dbcfg = new HistorianServerDatabaseConfig("DB", @"C:\Archive", true);
        HistorianServer server = new HistorianServer(dbcfg);
        SecureStreamServer<SocketUserPermissions> auth = new();
        auth.SetDefaultUser(true, new SocketUserPermissions()
        {
            CanRead = true,
            CanWrite = true,
            IsAdmin = true
        });

        SnapStreamingServer netServer = new(auth, netStream.ServerStream, server.Host);

        ThreadPool.QueueUserWorkItem(ProcessClient, netServer);

        SnapStreamingClient client = new(netStream.ClientStream, new SecureStreamClientDefault(), true);

        ClientDatabaseBase db = client.GetDatabase("DB");

        client.Dispose();
        server.Dispose();
    }

    [Test]
    public void TestFile()
    {
        //		FilePath	"C:\\Temp\\Historian\\635287587300536177-Stage1-d559e63e-d938-46a9-8d57-268f7c8ba194.d2"	string
        //635329017197429979-Stage1-38887e11-4097-4937-b269-ce4037157691.d2
        //using (var file = SortedTreeFile.OpenFile(@"C:\Temp\Historian\635287587300536177-Stage1-d559e63e-d938-46a9-8d57-268f7c8ba194.d2", true))
        using SortedTreeFile file = SortedTreeFile.OpenFile(@"C:\Archive\635329017197429979-Stage1-38887e11-4097-4937-b269-ce4037157691.d2", true);
        using SortedTreeTable<HistorianKey, HistorianValue> table = file.OpenTable<HistorianKey, HistorianValue>();
        using SortedTreeTableReadSnapshot<HistorianKey, HistorianValue> reader = table.BeginRead();
        SortedTreeScannerBase<HistorianKey, HistorianValue> scanner = reader.GetTreeScanner();
        scanner.SeekToStart();
        scanner.TestSequential().Count();

        HistorianKey key = new();
        HistorianValue value = new();

        int x = 0;
        scanner.Peek(key, value);

        while (scanner.Read(key, value) && x < 10000)
        {
            System.Console.WriteLine(key.PointId);
            x++;

            scanner.Peek(key, value);
        }

        scanner.Count();
    }

    [Test]
    public void Test2()
    {
        Logger.Console.Verbose = VerboseLevel.All;

        NetworkStreamSimulator netStream = new();

        HistorianServerDatabaseConfig dbcfg = new HistorianServerDatabaseConfig("DB", @"C:\Archive", true);
        HistorianServer server = new HistorianServer(dbcfg);
        SecureStreamServer<SocketUserPermissions> auth = new();
        auth.SetDefaultUser(true, new SocketUserPermissions()
        {
            CanRead = true,
            CanWrite = true,
            IsAdmin = true
        });

        SnapStreamingServer netServer = new(auth, netStream.ServerStream, server.Host);

        ThreadPool.QueueUserWorkItem(ProcessClient, netServer);

        SnapStreamingClient client = new(netStream.ClientStream, new SecureStreamClientDefault(), true);

        ClientDatabaseBase<HistorianKey, HistorianValue> db = client.GetDatabase<HistorianKey, HistorianValue>("DB");
        long len = db.Read().Count();
        System.Console.WriteLine(len);

        client.Dispose();
        server.Dispose();
    }


    [Test]
    public void TestWriteServer()
    {
        HistorianKey key = new();
        HistorianValue value = new();

        Logger.Console.Verbose = VerboseLevel.All;
        Logger.FileWriter.SetPath(@"C:\Temp\", VerboseLevel.All);

        NetworkStreamSimulator netStream = new();

        HistorianServerDatabaseConfig dbcfg = new HistorianServerDatabaseConfig("DB", @"C:\Temp\Scada", true);
        HistorianServer server = new HistorianServer(dbcfg);
        SecureStreamServer<SocketUserPermissions> auth = new();
        auth.SetDefaultUser(true, new SocketUserPermissions()
        {
            CanRead = true,
            CanWrite = true,
            IsAdmin = true
        });

        SnapStreamingServer netServer = new(auth, netStream.ServerStream, server.Host);

        ThreadPool.QueueUserWorkItem(ProcessClient, netServer);

        SnapStreamingClient client = new(netStream.ClientStream, new SecureStreamClientDefault(), false);

        ClientDatabaseBase<HistorianKey, HistorianValue> db = client.GetDatabase<HistorianKey, HistorianValue>("DB");
        for (uint x = 0; x < 1000; x++)
        {
            key.Timestamp = x;
            db.Write(key, value);
            break;
        }

        client.Dispose();
        server.Dispose();
    }

    private void ProcessClient(object netServer)
    {
        ((SnapStreamingServer)netServer).ProcessClient();
    }

}
