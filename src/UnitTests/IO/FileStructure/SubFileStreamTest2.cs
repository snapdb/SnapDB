﻿//******************************************************************************************************
//  SubFileStreamTest2.cs - Gbtc
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
//  12/10/2011 - Steven E. Chisholm
//       Generated original version of source code.
//
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap;
using SnapDB.UnitTests.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
public class SubFileStreamTest2
{
    #region [ Methods ]

    [Test]
    public void TestRandomWriteAmplification()
    {
        MemoryPoolTest.TestMemoryLeak();
        double size;

        using (TransactionalFileStructure file = TransactionalFileStructure.CreateInMemory(4096))
        using (TransactionalEdit edit = file.BeginEdit())
        using (SubFileStream stream = edit.CreateFile(SubFileName.CreateRandom()))
        using (BinaryStream bs = new(stream))
        {
            Stats.ChecksumCount = 0;
            DiskIoSession.WriteCount = 0;
            DiskIoSession.ReadCount = 0;

            bs.Position = 8 * 1000000 - 8;
            bs.Write(0);
            Random r = new(2425);

            //Write 8 million
            for (long s = 0; s < 1000000; s++)
            {
                bs.Position = r.Next(8000000 - 8);
                bs.Write((byte)1);
            }

            size = 1000000;
        }

        Console.WriteLine("Read: " + (DiskIoSession.ReadCount / size).ToString("0.000"));
        Console.WriteLine("Write: " + (DiskIoSession.WriteCount / size).ToString("0.000"));
        Console.WriteLine("Checksums: " + (Stats.ChecksumCount / size).ToString("0.000"));
        MemoryPoolTest.TestMemoryLeak();
    }


    [Test]
    public void TestSequentialWriteAmplification()
    {
        MemoryPoolTest.TestMemoryLeak();
        double size;
        Stats.ChecksumCount = 0;
        DiskIoSession.WriteCount = 0;
        DiskIoSession.ReadCount = 0;

        using (TransactionalFileStructure file = TransactionalFileStructure.CreateInMemory(4096))
        using (TransactionalEdit edit = file.BeginEdit())
        using (SubFileStream stream = edit.CreateFile(SubFileName.CreateRandom()))
        using (BinaryStream bs = new(stream))
        {
            Stats.ChecksumCount = 0;
            DiskIoSession.WriteCount = 0;
            DiskIoSession.ReadCount = 0;

            //Write 8 million
            for (long s = 0; s < 1000000; s++)
                bs.Write(s);
            size = bs.Position / 4096.0;
        }

        Console.WriteLine("Read: " + (DiskIoSession.ReadCount / size).ToString("0.0"));
        Console.WriteLine("Write: " + (DiskIoSession.WriteCount / size).ToString("0.0"));
        Console.WriteLine("Checksums: " + (Stats.ChecksumCount / size).ToString("0.0"));
        MemoryPoolTest.TestMemoryLeak();
    }

    [Test]
    public void TestSequentialReWriteAmplification()
    {
        MemoryPoolTest.TestMemoryLeak();
        double size;
        Stats.ChecksumCount = 0;
        DiskIoSession.WriteCount = 0;
        DiskIoSession.ReadCount = 0;

        using (TransactionalFileStructure file = TransactionalFileStructure.CreateInMemory(4096))
        using (TransactionalEdit edit = file.BeginEdit())
        using (SubFileStream stream = edit.CreateFile(SubFileName.CreateRandom()))
        using (BinaryStream bs = new(stream))
        {
            Stats.ChecksumCount = 0;
            DiskIoSession.WriteCount = 0;
            DiskIoSession.ReadCount = 0;

            //Write 8 million
            for (long s = 0; s < 1000000; s++)
                bs.Write(s);
            size = bs.Position / 4096.0;
            bs.Position = 0;

            Stats.ChecksumCount = 0;
            DiskIoSession.WriteCount = 0;
            DiskIoSession.ReadCount = 0;

            for (long s = 0; s < 1000000; s++)
                bs.Write(s);
        }

        Console.WriteLine("Read: " + (DiskIoSession.ReadCount / size).ToString("0.0"));
        Console.WriteLine("Write: " + (DiskIoSession.WriteCount / size).ToString("0.0"));
        Console.WriteLine("Checksums: " + (Stats.ChecksumCount / size).ToString("0.0"));
        MemoryPoolTest.TestMemoryLeak();
    }

    [Test]
    public void TestSequentialReadAmplification()
    {
        MemoryPoolTest.TestMemoryLeak();
        double size;
        Stats.ChecksumCount = 0;
        DiskIoSession.WriteCount = 0;
        DiskIoSession.ReadCount = 0;

        using (TransactionalFileStructure file = TransactionalFileStructure.CreateInMemory(4096))
        using (TransactionalEdit edit = file.BeginEdit())
        using (SubFileStream stream = edit.CreateFile(SubFileName.CreateRandom()))
        using (BinaryStream bs = new(stream))
        {
            //Write 8 million
            for (long s = 0; s < 1000000; s++)
                bs.Write(s);
            size = bs.Position / 4096.0;
            bs.Position = 0;

            Stats.ChecksumCount = 0;
            DiskIoSession.WriteCount = 0;
            DiskIoSession.ReadCount = 0;

            for (long s = 0; s < 1000000; s++)
                bs.ReadInt64();
        }

        Console.WriteLine("Read: " + (DiskIoSession.ReadCount / size).ToString("0.0"));
        Console.WriteLine("Write: " + (DiskIoSession.WriteCount / size).ToString("0.0"));
        Console.WriteLine("Checksums: " + (Stats.ChecksumCount / size).ToString("0.0"));
        MemoryPoolTest.TestMemoryLeak();
    }

    #endregion
}