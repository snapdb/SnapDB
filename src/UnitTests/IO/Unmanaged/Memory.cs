//******************************************************************************************************
//  MemoryTest.cs - Gbtc
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
//  3/18/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  6/8/2012 - Steven E. Chisholm
//       Removed large page support and simplified unused and untested procedures for initial release     
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SnapDB.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.Unmanaged;

[TestFixture]
public class MemoryTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        Memory block = new(1);

        if (block.Address == nint.Zero)
            throw new Exception();

        if (block.Size != 1)
            throw new Exception();

        block.Release();

        if (block.Address != nint.Zero)
            throw new Exception();

        if (block.Size != 0)
            throw new Exception();

        block.Release();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long mem = (long)MemoryPoolPageList.GetAvailablePhysicalMemory();

        // Allocate 100MB
        List<Memory> blocks = new();

        for (int x = 0; x < 10; x++)
            blocks.Add(new Memory(10000000));

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long mem2 = (long)MemoryPoolPageList.GetAvailablePhysicalMemory();

        // Verify that it increased by more than 50MB
        if (mem2 > mem + 1000000 * 50)
            throw new Exception();

        //Release through collection
        blocks = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Verify that the difference between the start and the end is less than 50MB
        long mem3 = (long)MemoryPoolPageList.GetAvailablePhysicalMemory();

        if (Math.Abs(mem3 - mem) > 1000000 * 50)
            throw new Exception();
    }

    #endregion
}