//******************************************************************************************************
//  Murmur3Test.cs - Gbtc
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
//  12/3/2011 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
internal class Murmur3Test
{
    #region [ Methods ]

    [Test]
    public unsafe void TestIsSame()
    {
        byte[] data = new byte[4096];
        Random r = new();

        for (int x = 0; x < 100; x++)
        {
            r.NextBytes(data);

            Murmur3Orig mm3 = new();
            byte[] checksum = mm3.ComputeHash(data);
            byte[] checksum2 = new byte[16];

            fixed (byte* lp = data)
            {
                Murmur3.ComputeHash(lp, data.Length, out ulong value1, out ulong value2);

                Array.Copy(BitConverter.GetBytes(value1), 0, checksum2, 0, 8);
                Array.Copy(BitConverter.GetBytes(value2), 0, checksum2, 8, 8);
            }

            Assert.IsTrue(checksum2.SequenceEqual(checksum));
        }
    }

    [Test]
    public unsafe void Benchmark()
    {
        byte[] data = new byte[4096];
        Random r = new(1);
        r.NextBytes(data);

        //Prime the run
        Murmur3Orig mm3 = new();
        for (int x = 0; x < 1000; x++)
            mm3.ComputeHash(data);
        fixed (byte* lp = data)
        {
            for (int x = 0; x < 1000; x++)
                Murmur3.ComputeHash(lp, data.Length, out ulong value1, out ulong value2);

            for (int x = 0; x < 1000; x++)
                Footer.ComputeChecksum((nint)lp, out long value3, out int value4, data.Length);
        }


        Stopwatch sw1 = new();
        Stopwatch sw2 = new();
        Stopwatch sw3 = new();

        sw1.Start();
        for (int x = 0; x < 10000; x++)
            mm3.ComputeHash(data);
        sw1.Stop();

        fixed (byte* lp = data)
        {
            sw2.Start();
            for (int x = 0; x < 10000; x++)
                Murmur3.ComputeHash(lp, data.Length, out ulong value1, out ulong value2);
            sw2.Stop();

            sw3.Start();
            for (int x = 0; x < 10000; x++)
                Footer.ComputeChecksum((nint)lp, out long value3, out int value4, data.Length);
            sw3.Stop();
        }

        Console.WriteLine("orig: " + (4096 * 10000 / sw1.Elapsed.TotalSeconds / 1024 / 1024).ToString("0 MB/S"));
        Console.WriteLine("mine: " + (4096 * 10000 / sw2.Elapsed.TotalSeconds / 1024 / 1024).ToString("0 MB/S"));
        Console.WriteLine("old: " + (4096 * 10000 / sw3.Elapsed.TotalSeconds / 1024 / 1024).ToString("0 MB/S"));
    }

    #endregion
}