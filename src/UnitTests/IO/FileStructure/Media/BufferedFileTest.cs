//******************************************************************************************************
//  BufferedFileTest.cs - Gbtc
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
//  10/28/2026 - Preston Crawford
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.IO;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;

namespace SnapDB.UnitTests.IO.FileStructure.Media;

[TestFixture]
public class BufferedFileTest
{
    #region [ Methods ]

    [Test]
    public void IoSessionDisposeAfterFileDisposeDoesNotThrow()
    {
        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d2i");

        try
        {
            FileHeaderBlock header = FileHeaderBlock.CreateNew(4096);
            CustomFileStream stream = CustomFileStream.CreateFile(fileName, Globals.MemoryPool.PageSize, header.BlockSize);
            BufferedFile file = new(stream, Globals.MemoryPool, header, true);
            BinaryStreamIoSessionBase session = file.CreateIoSession();

            // A leaked reader can be finalized after its file was deleted, so the session must tolerate this order.
            file.Dispose();

            Assert.DoesNotThrow(session.Dispose);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    #endregion
}