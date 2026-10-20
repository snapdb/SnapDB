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
//******************************************************************************************************

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SnapDB.IO.FileStructure;
using SnapDB.IO.FileStructure.Media;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Storage;

namespace SnapDB.UnitTests.IO.FileStructure.Media;

[TestFixture]
public class CustomFileStreamTest
{
    #region [ Members ]

    /// <summary>
    /// A <see cref="FileStream"/> that returns only part of each full-page read, as the <see cref="Stream"/> contract allows.
    /// </summary>
    private class ShortReadFileStream : FileStream
    {
        private readonly int m_pageSize;
        private readonly int m_shortReadLength;

        public ShortReadFileStream(string fileName, int pageSize, int shortReadLength) : base(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 2048, true)
        {
            m_pageSize = pageSize;
            m_shortReadLength = shortReadLength;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == m_pageSize)
                count = m_shortReadLength;

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }

    #endregion

    #region [ Methods ]

    [Test]
    public void PartialReadsReturnCorrectData()
    {
        string pendingFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.~d2i");
        string completeFileName = Path.ChangeExtension(pendingFileName, ".d2i");
        Random r = new(1);

        try
        {
            using (SimplifiedFileWriter writer = new(pendingFileName, completeFileName, 4096, FileFlags.ManualRollover))
            {
                using (ISupportsBinaryStream file = writer.CreateFile(SubFileName.CreateRandom()))
                using (BinaryStream bs = new(file))
                {
                    for (int x = 0; x < 100000; x++)
                        bs.Write(r.NextDouble());
                }

                writer.Commit();
            }

            r = new Random(1);

            using TransactionalFileStructure reader = TransactionalFileStructure.OpenFile(completeFileName, true);
            CustomFileStream queue = GetCustomFileStream(reader);

            // Holding a stream user keeps Open/Close from replacing the injected stream.
            queue.Open();

            try
            {
                FieldInfo streamField = typeof(CustomFileStream).GetField("m_stream", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ((FileStream)streamField.GetValue(queue)!).Dispose();
                streamField.SetValue(queue, new ShortReadFileStream(completeFileName, Globals.MemoryPool.PageSize, 4 * 4096));

                using SubFileStream file = reader.Snapshot.OpenFile(0);
                using BinaryStream bs = new(file);

                for (int x = 0; x < 100000; x++)
                    Assert.That(bs.ReadDouble(), Is.EqualTo(r.NextDouble()));
            }
            finally
            {
                queue.Close();
            }
        }
        finally
        {
            File.Delete(pendingFileName);
            File.Delete(completeFileName);
        }
    }

    private static CustomFileStream GetCustomFileStream(TransactionalFileStructure fileStructure)
    {
        object diskIo = GetPrivateField(fileStructure, "m_diskIo");
        object diskMedium = GetPrivateField(diskIo, "m_stream");
        object bufferedFile = GetPrivateField(diskMedium, "m_disk");

        return (CustomFileStream)GetPrivateField(bufferedFile, "m_queue");
    }

    private static object GetPrivateField(object instance, string name)
    {
        return instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    }

    #endregion
}