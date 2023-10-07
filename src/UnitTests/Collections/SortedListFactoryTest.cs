//******************************************************************************************************
//  SortedListFactoryTest.cs - Gbtc
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

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SnapDB.Collections;
using SnapDB.UnitTests.IO.Unmanaged;

namespace SnapDB.UnitTests.Collections;

[TestFixture]
internal class SortedListFactoryTest
{
    #region [ Methods ]

    [Test]
    public void Test1()
    {
        MemoryPoolTest.TestMemoryLeak();
        DebugStopwatch sw = new();

        for (int max = 10; max < 10000; max *= 2)
        {
            void Add1()
            {
                SortedList<int, int> list = new();
                for (int x = 0; x < max; x++)
                    list.Add(x, x);
            }

            void Add2()
            {
                List<int> keys = new(max);
                List<int> values = new(max);

                for (int x = 0; x < max; x++)
                {
                    keys.Add(x);
                    values.Add(x);
                }

                SortedList<int, int> sl = SortedListFactory.Create(keys, values);
            }

            //var makeList = new SortedListConstructorUnsafe<int, int>();
            //Action add3 = () =>
            //{
            //    List<int> keys = new List<int>(max);
            //    List<int> values = new List<int>(max);

            //    for (int x = 0; x < max; x++)
            //    {
            //        keys.Add(x);
            //        values.Add(x);
            //    }

            //    var sl = makeList.Create(keys, values);
            //    //var sl = SortedListFactory.CreateUnsafe(keys, values);

            //};
            Console.WriteLine("Old Method " + max + " " + sw.TimeEvent(Add1) * 1000000);
            Console.WriteLine("New Method " + max + " " + sw.TimeEvent(Add2) * 1000000);
            //Console.WriteLine("Unsafe Method " + max + " " + sw.TimeEvent(add3) * 1000000);
            MemoryPoolTest.TestMemoryLeak();
        }
    }

    #endregion
}