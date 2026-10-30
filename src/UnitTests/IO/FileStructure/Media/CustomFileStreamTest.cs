//******************************************************************************************************
//  CustomFileStreamTest.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
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
//  10/14/2026 - Preston Crawford
//       Generated original version of source code.
//
//  10/30/2026 - Preston Crawford
//       Replaced the injected short-read test, which ReadRaw no longer exercises, with a concurrent read test.
//
//******************************************************************************************************

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SnapDB.IO.FileStructure.Media;

namespace SnapDB.UnitTests.IO.FileStructure.Media;

[TestFixture]
public class CustomFileStreamTest
{
    #region [ Methods ]

    [Test]
    public void ConcurrentReadsReturnRequestedPosition()
    {
        const int BlockSize = 4096;
        const int PageSize = 65536;
        const int BlocksPerPage = PageSize / BlockSize;
        const int FullPages = 73;
        const int BlockCount = FullPages * BlocksPerPage + 4; // The last page holds only 4 blocks, so reading it is a short read.
        const int ReadsPerThread = 20000;
        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d2i");

        try
        {
            using CustomFileStream stream = CustomFileStream.CreateFile(fileName, PageSize, BlockSize);
            stream.Open();

            try
            {
                // Stamp every block with its own index.
                byte[] page = new byte[PageSize];

                for (int block = 0; block < BlockCount; block += BlocksPerPage)
                {
                    int blocks = Math.Min(BlocksPerPage, BlockCount - block);

                    for (int x = 0; x < blocks; x++)
                        BitConverter.TryWriteBytes(page.AsSpan(x * BlockSize), block + x);

                    stream.WriteRaw((long)block * BlockSize, page, blocks * BlockSize);
                }

                // Overlapping reads, some of them of the short last page, must each return the blocks at their own position.
                Parallel.For(0, 8, new ParallelOptions { MaxDegreeOfParallelism = 8 }, thread =>
                {
                    byte[] buffer = new byte[PageSize];
                    Random random = new(thread);

                    for (int read = 0; read < ReadsPerThread; read++)
                    {
                        int pageIndex = random.Next(5) == 0 ? FullPages : random.Next(FullPages);
                        int firstBlock = pageIndex * BlocksPerPage;
                        int length = stream.ReadRaw((long)firstBlock * BlockSize, buffer, PageSize);

                        Assert.That(length, Is.EqualTo(Math.Min(BlocksPerPage, BlockCount - firstBlock) * BlockSize));

                        for (int x = 0; x < length / BlockSize; x++)
                            Assert.That(BitConverter.ToInt32(buffer, x * BlockSize), Is.EqualTo(firstBlock + x), $"Read of block {firstBlock} returned the wrong data.");
                    }
                });
            }
            finally
            {
                stream.Close();
            }
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    #endregion
}