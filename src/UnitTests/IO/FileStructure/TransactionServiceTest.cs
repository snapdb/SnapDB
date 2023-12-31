﻿//******************************************************************************************************
//  TransactionServiceTest.cs - Gbtc
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
//  10/14/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
public class TransactionServiceTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);

        //string file = Path.GetTempFileName();
        //System.IO.File.Delete(file);
        //using (FileSystemSnapshotService service = FileSystemSnapshotService.CreateFile(file))

        using (TransactionalFileStructure service = TransactionalFileStructure.CreateInMemory(s_blockSize))
        {
            using (TransactionalEdit edit = service.BeginEdit())
            {
                SubFileStream fs = edit.CreateFile(SubFileName.CreateRandom());
                BinaryStream bs = new(fs);
                bs.Write((byte)1);
                bs.Dispose();
                fs.Dispose();
                edit.CommitAndDispose();
            }

            {
                ReadSnapshot read = service.Snapshot;
                SubFileStream f1 = read.OpenFile(0);
                BinaryStream bs1 = new(f1);
                if (bs1.ReadUInt8() != 1)
                    throw new Exception();

                using (TransactionalEdit edit = service.BeginEdit())
                {
                    SubFileStream f2 = edit.OpenFile(0);
                    BinaryStream bs2 = new(f2);
                    if (bs2.ReadUInt8() != 1)
                        throw new Exception();
                    bs2.Write((byte)3);
                    bs2.Dispose();
                } //rollback should be issued;

                if (bs1.ReadUInt8() != 0)
                    throw new Exception();
                bs1.Dispose();

                {
                    ReadSnapshot read2 = service.Snapshot;
                    SubFileStream f2 = read2.OpenFile(0);
                    BinaryStream bs2 = new(f2);
                    if (bs2.ReadUInt8() != 1)
                        throw new Exception();
                    if (bs2.ReadUInt8() != 0)
                        throw new Exception();
                    bs2.Dispose();
                }
            }
            using (TransactionalEdit edit = service.BeginEdit())
            {
                SubFileStream f2 = edit.OpenFile(0);
                BinaryStream bs2 = new(f2);
                bs2.Write((byte)13);
                bs2.Write((byte)23);
                bs2.Dispose();
                edit.RollbackAndDispose();
            } //rollback should be issued;
        }

        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);

        //Assert.IsTrue(true);
    }

    #endregion

    #region [ Static ]

    private static readonly int s_blockSize = 4096;

    #endregion
}