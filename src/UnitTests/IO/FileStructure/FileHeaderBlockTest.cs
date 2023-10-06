//******************************************************************************************************
//  FileHeaderBlockTest.cs - Gbtc
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
using SnapDB;
using SnapDB.IO.FileStructure;
using System;
using System.Linq;
using UnitTests.IO.Unmanaged;

namespace UnitTests.IO.FileStructure;

[TestFixture()]
public class FileHeaderBlockTest
{
    [Test()]
    public void Test()
    {
        MemoryPoolTest.TestMemoryLeak();
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
        FileHeaderBlock header = FileHeaderBlock.CreateNew(4096);
        header = header.CloneEditable();
        header.CreateNewFile(SubFileName.CreateRandom());
        header.CreateNewFile(SubFileName.CreateRandom());
        header.CreateNewFile(SubFileName.CreateRandom());
        header.IsReadOnly = true;

        FileHeaderBlock header2 = FileHeaderBlock.Open(header.GetBytes());

        CheckEqual(header2, header);
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
        //verify they are the same;
        MemoryPoolTest.TestMemoryLeak();
    }

    private static void CheckEqual(FileHeaderBlock RO, FileHeaderBlock RW)
    {
        if (!AreEqual(RW, RO))
            throw new Exception();
    }


    /// <summary>
    /// Compares two instances of <see cref="FileHeaderBlock"/> for equality.
    /// </summary>
    /// <param name="other">The <see cref="FileHeaderBlock"/> to compare to.</param>
    /// <param name="self">The <see cref="FileHeaderBlock"/> to compare from.</param>
    /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
    internal static bool AreEqual(FileHeaderBlock other, FileHeaderBlock self)
    {
        if (other is null)
            return false;
        if (self is null)
            return false;
        if (self.CanWrite != other.CanWrite) return false;
        if (self.CanRead != other.CanRead) return false;
        if (self.ArchiveId != other.ArchiveId) return false;
        if (self.ArchiveType != other.ArchiveType) return false;
        if (self.SnapshotSequenceNumber != other.SnapshotSequenceNumber) return false;
        if (self.LastAllocatedBlock != other.LastAllocatedBlock) return false;
        if (self.FileCount != other.FileCount) return false;

        //compare files.
        if (self.Files is null)
        {
            if (other.Files != null) return false;
        }
        else
        {
            if (other.Files is null) return false;
            if (self.Files.Count != other.Files.Count) return false;
            for (int x = 0; x < self.Files.Count; x++)
            {
                SubFileHeader subFile = self.Files[x];
                SubFileHeader subFileOther = other.Files[x];

                if (subFile is null)
                {
                    if (subFileOther != null) return false;
                }
                else
                {
                    if (subFileOther is null) return false;
                    if (!SubFileMetaDataTest.AreEqual(subFile, subFileOther)) return false;
                }
            }
        }

        return self.GetBytes().SequenceEqual(other.GetBytes());
    }
}