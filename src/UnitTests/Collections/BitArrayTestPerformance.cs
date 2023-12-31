﻿//******************************************************************************************************
//  BitArrayTestPerformance.cs - Gbtc
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
//  3/20/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using NUnit.Framework;
using SnapDB.Collections;
using SnapDB.UnitTests.IO.Unmanaged;

namespace SnapDB.UnitTests.Collections;

[TestFixture]
public class BitArrayTestPerformance
{
    #region [ Methods ]

    [Test]
    public void BitArray()
    {
        MemoryPoolTest.TestMemoryLeak();
        Stopwatch sw1 = new();
        Stopwatch sw2 = new();
        Stopwatch sw3 = new();
        Stopwatch sw4 = new();
        Stopwatch sw5 = new();
        Stopwatch sw6 = new();

        const int count = 20 * 1024 * 1024;

        //20 million, That's like 120GB of 64KB pages
        BitArray array = new(false, count);

        sw1.Start();
        for (int x = 0; x < count; x++)
            array.SetBit(x);
        sw1.Stop();

        sw2.Start();
        for (int x = 0; x < count; x++)
            array.SetBit(x);
        sw2.Stop();

        sw3.Start();
        for (int x = 0; x < count; x++)
            array.ClearBit(x);
        sw3.Stop();

        sw4.Start();
        for (int x = 0; x < count; x++)
            array.ClearBit(x);
        sw4.Stop();

        sw5.Start();
        for (int x = 0; x < count; x++)
        {
            if (array.GetBitUnchecked(x))
                throw new Exception();
        }

        sw5.Stop();

        //for (int x = 0; x < count -1; x++)
        //{
        //    array.SetBit(x);
        //}

        sw6.Start();
        for (int x = 0; x < count; x++)
            array.SetBit(array.FindClearedBit());
        sw6.Stop();

        Console.WriteLine("Set Bits: " + (count / sw1.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        Console.WriteLine("Set Bits Again: " + (count / sw2.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        Console.WriteLine("Clear Bits: " + (count / sw3.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        Console.WriteLine("Clear Bits Again: " + (count / sw4.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        Console.WriteLine("Get Bits: " + (count / sw5.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        Console.WriteLine("Find Cleared Bit (All bits cleared): " + (count / sw6.Elapsed.TotalSeconds / 1000000).ToString("0.0 MPP"));
        MemoryPoolTest.TestMemoryLeak();
    }

    #endregion
}