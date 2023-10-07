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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Org.BouncyCastle.Crypto.Digests;
using SnapDB.IO;
using SnapDB.Security;
using SnapDB.Security.Authentication;

namespace SnapDB.UnitTests.Security;

[TestFixture]
public class SrpTest
{
    #region [ Members ]

    private readonly Stopwatch m_sw = new();

    #endregion

    #region [ Methods ]

    [Test]
    public void TestDhKeyExchangeTime()
    {
        SrpConstants c = SrpConstants.Lookup(SrpStrength.Bits1024);
        c.G.ModPow(c.N, c.N);

        DebugStopwatch sw = new();
        double time = sw.TimeEvent(() => Hash<Sha1Digest>.Compute(c.Nb));
        Console.WriteLine(time);
    }

    [Test]
    public void Test()
    {
        Stopwatch sw = new();
        sw.Start();
        SrpServer srp = new();
        sw.Stop();
        Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        srp.Users.AddUser("user", "password");
        sw.Stop();
        Console.WriteLine(sw.Elapsed.TotalMilliseconds);

        sw.Restart();
        srp.Users.AddUser("user2", "password");
        sw.Stop();
        Console.WriteLine(sw.Elapsed.TotalMilliseconds);
    }

    [Test]
    public void Test1()
    {
        m_sw.Reset();

        NetworkStreamSimulator net = new();

        SrpServer sa = new();
        sa.Users.AddUser("user1", "password1");

        ThreadPool.QueueUserWorkItem(Client1, net.ClientStream);
        SrpServerSession user = sa.AuthenticateAsServer(net.ServerStream);
        user = sa.AuthenticateAsServer(net.ServerStream);
        if (user is null)
            throw new Exception();

        Thread.Sleep(100);
    }

    [Test]
    public void TestRepeat()
    {
        for (int x = 0; x < 5; x++)
            Test1();
    }

    private void Client1(object state)
    {
        Stream client = (Stream)state;
        SrpClient sa = new("user1", "password1");
        m_sw.Start();
        _ = sa.AuthenticateAsClient(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
        m_sw.Restart();
        bool success = sa.AuthenticateAsClient(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
        if (!success)
            throw new Exception();
    }

    #endregion
}