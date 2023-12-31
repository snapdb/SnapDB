﻿//******************************************************************************************************
//  ResourceQueueCollection.cs - Gbtc
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
//  9/22/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using NUnit.Framework;
using SnapDB.Collections;
using SnapDB.UnitTests.IO.Unmanaged;

namespace SnapDB.UnitTests.Collections;

[TestFixture]
public class ResourceQueueCollectionTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        MemoryPoolTest.TestMemoryLeak();
        ResourceQueueCollection<int, string> queue = new(x => () => x.ToString(), 3, 3);

        Assert.AreEqual("1", queue[1].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("999", queue[999].Dequeue());

        queue[250].Enqueue("0");

        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("0", queue[250].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        MemoryPoolTest.TestMemoryLeak();
    }

    [Test]
    public void Test2()
    {
        MemoryPoolTest.TestMemoryLeak();
        ResourceQueueCollection<int, string> queue = new(x => x.ToString(), 3, 3);

        Assert.AreEqual("1", queue[1].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("999", queue[999].Dequeue());

        queue[250].Enqueue("0");

        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        Assert.AreEqual("0", queue[250].Dequeue());
        Assert.AreEqual("250", queue[250].Dequeue());
        MemoryPoolTest.TestMemoryLeak();
    }

    [Test]
    public void Test3()
    {
        MemoryPoolTest.TestMemoryLeak();
        ResourceQueueCollection<int, string> queue = new(() => 3.ToString(), 3, 3);

        Assert.AreEqual("3", queue[1].Dequeue());
        Assert.AreEqual("3", queue[250].Dequeue());
        Assert.AreEqual("3", queue[999].Dequeue());

        queue[250].Enqueue("0");

        Assert.AreEqual("3", queue[250].Dequeue());
        Assert.AreEqual("3", queue[250].Dequeue());
        Assert.AreEqual("0", queue[250].Dequeue());
        Assert.AreEqual("3", queue[250].Dequeue());
        MemoryPoolTest.TestMemoryLeak();
    }

    #endregion
}