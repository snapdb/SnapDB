//******************************************************************************************************
//  SecureStream_Test.cs - Gbtc
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
using Gemstone.Diagnostics;
using Gemstone.IO.StreamExtensions;
using NUnit.Framework;
using SnapDB.IO;
using SnapDB.Security;
using SnapDB.Security.Authentication;

namespace SnapDB.UnitTests.Security;

// TODO: Re-enable these tests when self-signed certificate implementation is fixed
//[TestFixture]
public class SecureStreamTest
{
    #region [ Members ]

    public NullToken T;
    private readonly Stopwatch m_sw = new();

    #endregion

    #region [ Methods ]

    //[Test]
    //public void Test1()
    //{
    //    m_sw.Reset();

    //    var net = new NetworkStreamSimulator();

    //    var sa = new SecureStreamServer<NullToken>();
    //    sa.Srp.Users.AddUser("user1","password");
    //    ThreadPool.QueueUserWorkItem(Client1, net.ClientStream);

    //    SecureStream stream;
    //    sa.TryAuthenticateAsServer(net.ServerStream, out stream, out T);
    //    sa.TryAuthenticateAsServer(net.ServerStream, out stream, out T);

    //    Thread.Sleep(100);
    //}

    //void Client1(object state)
    //{
    //    Stream client = (Stream)state;
    //    var sa = new SecureStreamClientSrp("user1", "password");
    //    m_sw.Start();

    //    sa.TryAuthenticate(client);

    //    m_sw.Stop();
    //    System.Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
    //    m_sw.Restart();
    //    sa.TryAuthenticate(client);

    //    m_sw.Stop();
    //    System.Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
    //}

    //[Test]
    //public void TestRepeat()
    //{
    //    for (int x = 0; x < 5; x++)
    //        Test1();

    //}

    //[Test]
    //public void Default()
    //{
    //    for (int x = 0; x < 5; x++)
    //        TestDefault();

    //}

    // TODO: Disabled [Test]
    public void TestDefault()
    {
        Logger.Console.Verbose = VerboseLevel.All;
        m_sw.Reset();

        NetworkStreamSimulator net = new();
        SecureStreamServer<NullToken> sa = new();
        sa.SetDefaultUser(true, new NullToken());
        ThreadPool.QueueUserWorkItem(ClientDefault, net.ClientStream);

        // TODO: This should timeout to accomodate initialization failure
        if (!sa.TryAuthenticateAsServer(net.ServerStream, true, out Stream stream, out T))
            throw new Exception();

        stream.Write("Message");
        stream.Flush();
        if (stream.ReadString() != "Response")
            throw new Exception();
        stream.Dispose();

        Thread.Sleep(100);
    }

    // TODO: Disabled [Test]
    public void BenchmarkDefault()
    {
        for (int x = 0; x < 5; x++)
            TestBenchmarkDefault();
    }

    // TODO: Disabled [Test]
    public void TestBenchmarkDefault()
    {
        Logger.Console.Verbose = VerboseLevel.All;
        m_sw.Reset();

        NetworkStreamSimulator net = new();

        SecureStreamServer<NullToken> sa = new();
        sa.SetDefaultUser(true, new NullToken());
        ThreadPool.QueueUserWorkItem(ClientBenchmarkDefault, net.ClientStream);

        sa.TryAuthenticateAsServer(net.ServerStream, false, out Stream stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, false, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, false, out stream, out T);

        Thread.Sleep(100);
    }

    // TODO: Disabled [Test]
    public void TestBenchmarkIntegrated()
    {
        return;
        Logger.Console.Verbose = VerboseLevel.All;
        m_sw.Reset();

        NetworkStreamSimulator net = new();

        SecureStreamServer<NullToken> sa = new();
        sa.AddUserIntegratedSecurity("Zthe\\steven", new NullToken());
        ThreadPool.QueueUserWorkItem(ClientBenchmarkIntegrated, net.ClientStream);

        Stream stream;
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);
        sa.TryAuthenticateAsServer(net.ServerStream, true, out stream, out T);

        Thread.Sleep(100);
    }

    // TODO: Disabled [Test]
    public void TestRepeatIntegrated()
    {
        for (int x = 0; x < 5; x++)
            TestBenchmarkIntegrated();
    }

    private void ClientDefault(object state)
    {
        Stream client = (Stream)state;
        SecureStreamClientDefault sa = new();
        if (!sa.TryAuthenticate(client, true, out Stream stream))
            throw new Exception();

        if (stream.ReadString() != "Message")
            throw new Exception();
        stream.Write("Response");
        stream.Flush();
        stream.Dispose();
    }

    private void ClientBenchmarkDefault(object state)
    {
        Stream client = (Stream)state;
        SecureStreamClientDefault sa = new();
        m_sw.Start();
        sa.TryAuthenticate(client, false);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client, false);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client, false);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
    }

    private void ClientBenchmarkIntegrated(object state)
    {
        Stream client = (Stream)state;
        SecureStreamClientIntegratedSecurity sa = new();
        m_sw.Start();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);

        m_sw.Restart();
        sa.TryAuthenticate(client);
        m_sw.Stop();
        Console.WriteLine(m_sw.Elapsed.TotalMilliseconds);
    }

    #endregion
}