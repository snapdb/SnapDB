//******************************************************************************************************
//  RemoteOutputAdapterTest.cs - Gbtc
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
using System.Threading;

namespace UnitTests;

[TestFixture]
internal class RemoteOutputAdapterTest
{
    [Test]
    public void TestRemoteAdapter()
    {
        HistorianKey key = new HistorianKey();
        HistorianValue value = new HistorianValue();

        HistorianServerDatabaseConfig settings = new HistorianServerDatabaseConfig("PPA", @"c:\temp\historian\", true);

        using (HistorianServer server = new HistorianServer(settings))
        using (SnapClient client = SnapClient.Connect(server.Host))
        {
            using (HistorianInputQueue queue = new HistorianInputQueue(() => client.GetDatabase<HistorianKey, HistorianValue>(string.Empty)))
            {
                for (uint x = 0; x < 100000; x++)
                {
                    key.PointID = x;
                    queue.Enqueue(key, value);
                }
                Thread.Sleep(100);
            }
            Thread.Sleep(100);
        }
        //Thread.Sleep(100);
    }
}