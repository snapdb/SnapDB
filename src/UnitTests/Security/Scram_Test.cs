//******************************************************************************************************
//  Scram_Test.cs - Gbtc
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
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace UnitTests.Security;

[TestFixture]
public class Scram_Test
{
    readonly Stopwatch m_sw = new Stopwatch();

    [Test]
    public void Test1()
    {
        m_sw.Reset();

        NetworkStreamSimulator net = new NetworkStreamSimulator();

        Stopwatch sw = new Stopwatch();
        ScramServer sa = new ScramServer();
        sw.Start();
        sa.Users.AddUser("user1", "password1", 10000, 1, HashMethod.Sha256);
        sw.Stop();
        System.Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        ThreadPool.QueueUserWorkItem(Client1, net.ClientStream);
        ScramServerSession user = sa.AuthenticateAsServer(net.ServerStream, new byte[] { 100, 29 });
        user = sa.AuthenticateAsServer(net.ServerStream, new byte[] { 100, 29 });
        if (user is null)
            throw new Exception();

        Thread.Sleep(100);
    }

    void Client1(object state)
    {
        Stream client = (Stream)state;
        ScramClient sa = new ScramClient("user1", "password1");
        sa.AuthenticateAsClient(client, new byte[] { 100, 29 });
        m_sw.Start();
        bool success = sa.AuthenticateAsClient(client, new byte[] { 100, 29 });
        m_sw.Stop();
        System.Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
        if (!success)
            throw new Exception();
    }

    [Test]
    public void TestMultiple()
    {
        Test1();
        Test1();
        Test1();
        Test1();
        Test1();
        Test1();
        Test1();
    }

}
