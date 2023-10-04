//******************************************************************************************************
//  PeriodicScannerTest.cs - Gbtc
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
using System;

namespace UnitTests.Data;

[TestFixture]
public class PeriodicScannerTest
{
    [Test]
    public void Test1()
    {
        DateTime start = DateTime.Now.Date;
        DateTime stop = start.AddDays(1);

        PeriodicScanner scanner = new PeriodicScanner(30);
        _ = scanner.GetParser(start, stop, 2592000);
        _ = scanner.GetParser(start, stop, 2592000 / 2);
        _ = scanner.GetParser(start, stop, 2592000 / 3);
        _ = scanner.GetParser(start, stop, 2592000 / 4);
        _ = scanner.GetParser(start, stop, 2592000 / 5);
        _ = new DateTime(634794697200000000).ToString();
    }
}