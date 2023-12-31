﻿//******************************************************************************************************
//  TimestampFilterTest.cs - Gbtc
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
using SnapDB.IO.Unmanaged;
using SnapDB.Snap;
using SnapDB.Snap.Filters;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Filters;

[TestFixture]
public class TimestampFilterTest
{
    #region [ Methods ]

    [Test]
    public void TestFixedRange()
    {
        _ = new List<ulong>();
        SeekFilterBase<HistorianKey> pointId = TimestampSeekFilter.CreateFromRange<HistorianKey>(0, 100);

        if (!pointId.GetType().FullName.Contains("FixedRange"))
            throw new Exception("Wrong type");

        using BinaryStream bs = new(true);
        bs.Write(pointId.FilterType);
        pointId.Save(bs);
        bs.Position = 0;

        SeekFilterBase<HistorianKey> filter = Library.Filters.GetSeekFilter<HistorianKey>(bs.ReadGuid(), bs);

        if (!filter.GetType().FullName.Contains("FixedRange"))
            throw new Exception("Wrong type");
    }

    [Test]
    public void TestIntervalRanges()
    {
        _ = new List<ulong>();
        SeekFilterBase<HistorianKey> pointId = TimestampSeekFilter.CreateFromIntervalData<HistorianKey>(0, 100, 10, 3, 1);

        if (!pointId.GetType().FullName.Contains("IntervalRanges"))
            throw new Exception("Wrong type");

        using BinaryStream bs = new(true);
        bs.Write(pointId.FilterType);
        pointId.Save(bs);
        bs.Position = 0;

        SeekFilterBase<HistorianKey> filter = Library.Filters.GetSeekFilter<HistorianKey>(bs.ReadGuid(), bs);

        if (!filter.GetType().FullName.Contains("IntervalRanges"))
            throw new Exception("Wrong type");
    }

    #endregion
}