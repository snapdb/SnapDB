//******************************************************************************************************
//  KeyValueMethodsTest.cs - Gbtc
//
//  Copyright © 2023, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using NUnit.Framework;

namespace UnitTests.SortedTreeStore.Tree.Generic;

[TestFixture]
public unsafe class KeyValueMethodsTest
{
    [Test]
    public void Test1()
    {
        //byte* temp1 = stackalloc byte[9];
        //byte* temp2 = stackalloc byte[9];

        //var kvm = new KeyValueMethods<uint, uint>(new BoxKeyMethodsUint32(), new BoxValueMethodsUint32());
        //Assert.AreEqual(4 + 4 + 1, kvm.MaxKeyValueSize);
        //var key = new TreeUInt32();
        //var value = new TreeUInt32();

        //key.Value = 5;
        //value.Value = 6;

        //Assert.AreEqual(3, kvm.Write(temp1, key, value));

        //Assert.AreEqual(3, kvm.Read(temp1, temp2, key, value));
        //Assert.AreEqual(5u, key.Value);
        //Assert.AreEqual(6u, value.Value);

        //key.Value = 0xAA00DD00u;
        //value.Value = 0x00CC0011u;

        //Assert.AreEqual(5, kvm.Write(temp1, key, value));

        //Assert.AreEqual(5, kvm.Read(temp1, temp2, key, value));
        //Assert.AreEqual(0xAA00DD00u, key.Value);
        //Assert.AreEqual(0x00CC0011u, value.Value);
    }
}