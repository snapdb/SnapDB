//******************************************************************************************************
//  WeakListTest.cs - Gbtc
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
//  4/11/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  10/04/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnapDB.Collections;

namespace SnapDB.UnitTests.Collections;

[TestFixture]
internal class WeakListTest
{
    private class Data
    {
        public string Value;

        public Data(string value)
        {
            Value = value;
        }
    }

    #region [ Methods ]

    [Test]
    public void Test()
    {
        Random rand = new(3);

        List<Data> list1 = new();
        WeakList<Data> list2 = new();

        for (int x = 0; x < 1000; x++)
        {
            Data str = new(x.ToString());
            list1.Add(str);
            list2.Add(str);

            if (!list1.SequenceEqual(list2))
                throw new Exception("Lists are not the same.");
        }

        for (int x = 1000; x < 2000; x++)
        {
            Data str = new(x.ToString());
            Data removeItem = list1[rand.Next(list1.Count)];
            list1.Remove(removeItem);
            list2.Remove(removeItem);

            if (!list1.SequenceEqual(list2))
                throw new Exception("Lists are not the same.");

            list1.Add(str);
            list2.Add(str);

            if (!list1.SequenceEqual(list2))
                throw new Exception("Lists are not the same.");
        }

        for (int x = 0; x < 100; x++)
        {
            list1.RemoveAt(rand.Next(list1.Count));
            GC.Collect();

            if (!list1.SequenceEqual(list2))
                throw new Exception("Lists are not the same.");
        }

        list2.Clear();
        foreach (Data data in list2)
            throw new Exception();
    }

    #endregion
}