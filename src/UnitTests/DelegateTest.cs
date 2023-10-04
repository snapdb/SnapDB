//******************************************************************************************************
//  DelegateTest.cs - Gbtc
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
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
public class DelegateTest
{
    public event Action MyEvent;

    [Test]
    public void Test()
    {
        if (MyEvent is null)
            System.Console.WriteLine("1: Null");
        else
            System.Console.WriteLine("1: " + MyEvent.GetType().ToString());

        MyEvent += Test;

        if (MyEvent is null)
            System.Console.WriteLine("2: Null");
        else
            System.Console.WriteLine("2: " + MyEvent.GetType().ToString());

        MyEvent -= Test;

        if (MyEvent is null)
            System.Console.WriteLine("3: Null");
        else
            System.Console.WriteLine("3: " + MyEvent.GetType().ToString());

    }


}
