﻿//******************************************************************************************************
//  BenchmarkSubFileStreamTest.cs - Gbtc
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

using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.Unmanaged;
using SnapDB.UnitTests.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
internal class BenchmarkSubFileStreamTest
{
    #region [ Methods ]

    [Test]
    public void TestSubFileStream()
    {
        const int blockSize = 256;
        MemoryPoolTest.TestMemoryLeak();

        //string file = Path.GetTempFileName();
        //System.IO.File.Delete(file);
        //using (FileSystemSnapshotService service = FileSystemSnapshotService.CreateFile(file))

        using (TransactionalFileStructure service = TransactionalFileStructure.CreateInMemory(blockSize))
        using (TransactionalEdit edit = service.BeginEdit())
        {
            SubFileStream fs = edit.CreateFile(SubFileName.Empty);
            BinaryStream bs = new(fs);

            for (int x = 0; x < 20000000; x++)
                bs.Write(1L);

            bs.Position = 0;

            BinaryStreamBenchmark.Run(bs, false);

            bs.Dispose();
            fs.Dispose();
            edit.CommitAndDispose();
        }

        MemoryPoolTest.TestMemoryLeak();
    }

    #endregion
}