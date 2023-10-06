//******************************************************************************************************
//  PointIDFilterTest.cs - Gbtc
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
using SnapDB.IO.Unmanaged;
using SnapDB.Snap;
using SnapDB.Snap.Filters;
using System;
using System.Collections.Generic;

namespace UnitTests.SortedTreeStore.Filters;

[TestFixture]
public class PointIDFilterTest
{
    [Test]
    public void TestBitArray()
    {
        List<ulong> list = new List<ulong>();
        MatchFilterBase<HistorianKey, HistorianValue> pointId = PointIdMatchFilter.CreateFromList<HistorianKey, HistorianValue>(list);

        if (!pointId.GetType().FullName.Contains("BitArrayFilter"))
            throw new Exception("Wrong type");

        using (BinaryStream bs = new BinaryStream(allocatesOwnMemory: true))
        {
            bs.Write(pointId.FilterType);
            pointId.Save(bs);
            bs.Position = 0;

            MatchFilterBase<HistorianKey, HistorianValue> filter = Library.Filters.GetMatchFilter<HistorianKey, HistorianValue>(bs.ReadGuid(), bs);

            if (!filter.GetType().FullName.Contains("BitArrayFilter"))
                throw new Exception("Wrong type");
        }
    }

    [Test]
    public void TestUintHashSet()
    {
        List<ulong> list = new List<ulong>();
        list.Add(132412341);
        MatchFilterBase<HistorianKey, HistorianValue> pointId = PointIdMatchFilter.CreateFromList<HistorianKey, HistorianValue>(list);

        if (!pointId.GetType().FullName.Contains("UIntHashSet"))
            throw new Exception("Wrong type");

        using (BinaryStream bs = new BinaryStream(allocatesOwnMemory: true))
        {
            bs.Write(pointId.FilterType);
            pointId.Save(bs);
            bs.Position = 0;

            MatchFilterBase<HistorianKey, HistorianValue> filter = Library.Filters.GetMatchFilter<HistorianKey, HistorianValue>(bs.ReadGuid(), bs);

            if (!filter.GetType().FullName.Contains("UIntHashSet"))
                throw new Exception("Wrong type");
        }
    }

    [Test]
    public void TestUlongHashSet()
    {
        List<ulong> list = new List<ulong>();
        list.Add(13242345234523412341ul);
        MatchFilterBase<HistorianKey, HistorianValue> pointId = PointIdMatchFilter.CreateFromList<HistorianKey, HistorianValue>(list);

        if (!pointId.GetType().FullName.Contains("ULongHashSet"))
            throw new Exception("Wrong type");

        using (BinaryStream bs = new BinaryStream(allocatesOwnMemory: true))
        {
            bs.Write(pointId.FilterType);
            pointId.Save(bs);
            bs.Position = 0;

            MatchFilterBase<HistorianKey, HistorianValue> filter = Library.Filters.GetMatchFilter<HistorianKey, HistorianValue>(bs.ReadGuid(), bs);

            if (!filter.GetType().FullName.Contains("ULongHashSet"))
                throw new Exception("Wrong type");
        }
    }

}
