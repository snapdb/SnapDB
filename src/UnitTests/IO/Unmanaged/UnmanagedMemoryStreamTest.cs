﻿//******************************************************************************************************
//  UnmanagedMemoryStreamTest.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  9/30/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using NUnit.Framework;
using SnapDB.IO;
using SnapDB.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.Unmanaged;

[TestFixture]
public class UnmanagedMemoryStreamTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        MemoryPoolTest.TestMemoryLeak();
        SelfTest();
        UnmanagedMemoryStream ms = new();
        BinaryStreamTest.Test(ms);
        Assert.IsTrue(true);
        ms.Dispose();
        MemoryPoolTest.TestMemoryLeak();
    }

    #endregion

    #region [ Static ]

    private static void SelfTest()
    {
        UnmanagedMemoryStream ms1 = new();
        BinaryStreamBase ms = ms1.CreateBinaryStream();
        Random rand = new();
        int seed = rand.Next();
        rand = new Random(seed);
        byte[] data = new byte[255];
        rand.NextBytes(data);

        while (ms.Position < 1000000)
            ms.Write(data, 0, rand.Next(256));

        byte[] data2 = new byte[255];
        rand = new Random(seed);
        rand.NextBytes(data2);
        ms.Position = 0;
        Compare(data, data2, 255);
        while (ms.Position < 1000000)
        {
            int length = rand.Next(256);
            ms.ReadAll(data2, 0, length);
            Compare(data, data2, length);
        }

        ms.Dispose();
        ms1.Dispose();
    }

    private static void Compare(byte[] a, byte[] b, int length)
    {
        for (int x = 0; x < length; x++)
        {
            if (a[x] != b[x])
                throw new Exception();
        }
    }

    #endregion
}