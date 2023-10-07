//******************************************************************************************************
//  TreadSafeListTest.cs - Gbtc
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

using System.Linq;
using NUnit.Framework;
using SnapDB.Threading;

namespace SnapDB.UnitTests.Threading;

[TestFixture]
internal class ThreadSafeListTest
{
    #region [ Methods ]

    [Test]
    public void Test()
    {
        ThreadSafeList<int> ts = new();

        for (int x = 0; x < 10; x++)
            ts.Add(x);

        Assert.AreEqual(10, ts.Count());
        Assert.IsTrue(ts.Remove(5));
        Assert.AreEqual(9, ts.Count());
        Assert.IsFalse(ts.Remove(5));

        int count = 0;
        foreach (int x in ts)
        {
            count++;
            ts.ForEach(i => ts.Remove(i));
        }

        Assert.AreEqual(1, count);
    }

    #endregion
}