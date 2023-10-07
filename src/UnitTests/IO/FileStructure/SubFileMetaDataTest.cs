//******************************************************************************************************
//  SubFileMetaDataTest.cs - Gbtc
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
//  11/23/2011 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.IO;
using NUnit.Framework;
using SnapDB.IO.FileStructure;

namespace SnapDB.UnitTests.IO.FileStructure;

[TestFixture]
public class SubFileMetaDataTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
        Random rand = new();
        ushort fileIdNumber = (ushort)rand.Next(int.MaxValue);
        SubFileName fileName = SubFileName.CreateRandom();
        int dataBlock1 = rand.Next(int.MaxValue);
        int singleRedirect = rand.Next(int.MaxValue);
        int doubleRedirect = rand.Next(int.MaxValue);
        int tripleRedirect = rand.Next(int.MaxValue);
        int quadrupleRedirect = rand.Next(int.MaxValue);

        SubFileHeader node = new(fileIdNumber, fileName, false, false)
        {
            DirectBlock = (uint)dataBlock1,
            SingleIndirectBlock = (uint)singleRedirect,
            DoubleIndirectBlock = (uint)doubleRedirect,
            TripleIndirectBlock = (uint)tripleRedirect,
            QuadrupleIndirectBlock = (uint)quadrupleRedirect
        };
        SubFileHeader node2 = SaveItem(node);

        if (node2.FileIdNumber != fileIdNumber)
            throw new Exception();
        if (node2.FileName != fileName)
            throw new Exception();
        if (node2.DirectBlock != dataBlock1)
            throw new Exception();
        if (node2.SingleIndirectBlock != singleRedirect)
            throw new Exception();
        if (node2.DoubleIndirectBlock != doubleRedirect)
            throw new Exception();
        if (node2.TripleIndirectBlock != tripleRedirect)
            throw new Exception();
        if (node2.QuadrupleIndirectBlock != quadrupleRedirect)
            throw new Exception();
        Assert.IsTrue(true);

        Assert.AreEqual(Globals.MemoryPool.AllocatedBytes, 0L);
    }

    #endregion

    #region [ Static ]

    private static SubFileHeader SaveItem(SubFileHeader node)
    {
        //Serialize the header
        MemoryStream stream = new();
        node.Save(new BinaryWriter(stream));

        stream.Position = 0;
        //load the header
        SubFileHeader node2 = new(new BinaryReader(stream), true, false);

        CheckEqual(node2, node);

        SubFileHeader node3 = node2.CloneEditable();

        CheckEqual(node2, node3);
        return node3;
    }

    internal static void CheckEqual(SubFileHeader ro, SubFileHeader rw)
    {
        if (!AreEqual(ro, rw))
            throw new Exception();
    }

    /// <summary>
    /// Determines if the two objects are equal in value.
    /// </summary>
    /// <param name="a">The first <see cref="SubFileHeader"/> to compare.</param>
    /// <param name="b">The second <see cref="SubFileHeader"/> to compare.</param>
    /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
    internal static bool AreEqual(SubFileHeader a, SubFileHeader b)
    {
        if (a is null || b is null)
            return false;

        if (b.FileIdNumber != a.FileIdNumber)
            return false;
        if (b.FileName != a.FileName)
            return false;
        if (b.DirectBlock != a.DirectBlock)
            return false;
        if (b.SingleIndirectBlock != a.SingleIndirectBlock)
            return false;
        if (b.DoubleIndirectBlock != a.DoubleIndirectBlock)
            return false;
        if (b.TripleIndirectBlock != a.TripleIndirectBlock)
            return false;
        if (b.QuadrupleIndirectBlock != a.QuadrupleIndirectBlock)
            return false;
        return true;
    }

    #endregion
}