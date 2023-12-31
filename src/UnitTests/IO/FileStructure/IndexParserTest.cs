﻿//******************************************************************************************************
//  IndexParserTest.cs - Gbtc
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
//  1/4/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
public class IndexParserTest
{
    #region [ Methods ]

    //Note: Most of this code is tested in other test procedures.
    [Test]
    public void Test()
    {
        int blockSize = 4096;
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);

        DiskIo stream = DiskIo.CreateMemoryFile(Globals.MemoryPool, blockSize);
        SubFileName name = SubFileName.CreateRandom();
        SubFileHeader node = new(1, name, false, false);
        SubFileDiskIoSessionPool pool = new(stream, stream.LastCommittedHeader, node, true);
        IndexParser parse = new(pool);

        parse.SetPositionAndLookup(14312);
        pool.Dispose();
        Assert.IsTrue(true);
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
    }

    #endregion
}