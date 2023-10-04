//******************************************************************************************************
//  Srp_Test.cs - Gbtc
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
public class Srp_Test
{
    [Test]
    public void TestDHKeyExchangeTime()
    {
        SrpConstants c = SrpConstants.Lookup(SrpStrength.Bits1024);
        c.g.ModPow(c.N, c.N);

        DebugStopwatch sw = new DebugStopwatch();
        double time = sw.TimeEvent(() => Hash<Sha1Digest>.Compute(c.Nb));
        System.Console.WriteLine(time);



    }

    [Test]
    public void Test()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        SrpServer srp = new SrpServer();
        sw.Stop();
        System.Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        srp.Users.AddUser("user", "password");
        sw.Stop();
        System.Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        srp.Users.AddUser("user2", "password");
        sw.Stop();
        System.Console.WriteLine(sw.Elapsed.TotalMilliseconds);
    }

    readonly Stopwatch m_sw = new Stopwatch();

    [Test]
    public void Test1()
    {
        m_sw.Reset();

        NetworkStreamSimulator net = new NetworkStreamSimulator();

        SrpServer sa = new SrpServer();
        sa.Users.AddUser("user1", "password1", SrpStrength.Bits1024);

        ThreadPool.QueueUserWorkItem(Client1, net.ClientStream);
        SrpServerSession user = sa.AuthenticateAsServer(net.ServerStream);
        user = sa.AuthenticateAsServer(net.ServerStream);
        if (user is null)
            throw new Exception();

        Thread.Sleep(100);
    }

    void Client1(object state)
    {
        Stream client = (Stream)state;
        SrpClient sa = new SrpClient("user1", "password1");
        m_sw.Start();
        _ = sa.AuthenticateAsClient(client);
        m_sw.Stop();
        System.Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
        m_sw.Restart();
        bool success = sa.AuthenticateAsClient(client);
        m_sw.Stop();
        System.Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
        if (!success)
            throw new Exception();
    }

    [Test]
    public void TestRepeat()
    {
        for (int x = 0; x < 5; x++)
            Test1();

    }
}
