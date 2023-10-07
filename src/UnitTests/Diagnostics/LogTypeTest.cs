//******************************************************************************************************
//  LogTypeTest.cs - Gbtc
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
using Gemstone.Diagnostics;
using NUnit.Framework;

namespace SnapDB.UnitTests.Diagnostics;

[TestFixture]
public class LogTypeTest
{
    #region [ Members ]

    public class T1<T11, T12>
    {
        #region [ Members ]

        public class T2<T22>
        {
            #region [ Members ]

            public readonly LogPublisher LogType = Logger.CreatePublisher(typeof(T2<T22>), MessageClass.Component);

            #endregion
        }

        #endregion
    }

    #endregion

    #region [ Methods ]

    //private readonly static LogType LogType = LogType.Create(typeof(LogTypeTest));

    [Test]
    public void Test()
    {
        _ = new T1<int?, string>.T2<long?>();

        Console.WriteLine(0);
    }

    #endregion
}