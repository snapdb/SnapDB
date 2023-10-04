//******************************************************************************************************
//  ISupportsReadonlyTest.cs - Gbtc
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

namespace UnitTests.Collections;

/// <summary>
///This is a test class for ISupportsReadonlyTest and is intended
///to contain all ISupportsReadonlyTest Unit Tests
///</summary>
public class ISupportsReadonlyTest
{
    public static void Test<T>(IImmutableObject<T> obj)
    {
        bool origional = obj.IsReadOnly;

        IImmutableObject<T> ro = (IImmutableObject<T>)obj.CloneReadonly();
        Assert.AreEqual(true, ro.IsReadOnly);
        Assert.AreEqual(origional, obj.IsReadOnly);

        IImmutableObject<T> rw = (IImmutableObject<T>)obj.CloneEditable();
        Assert.AreEqual(false, rw.IsReadOnly);
        Assert.AreEqual(origional, obj.IsReadOnly);
        rw.IsReadOnly = true;
        Assert.AreEqual(origional, obj.IsReadOnly);

        Assert.AreEqual(true, rw.IsReadOnly);
        HelperFunctions.ExpectError(() => rw.IsReadOnly = false);
        Assert.AreEqual(origional, obj.IsReadOnly);

        HelperFunctions.ExpectError(() => ro.IsReadOnly = false);
    }
}