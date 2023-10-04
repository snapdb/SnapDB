//******************************************************************************************************
//  RolloverLogFile_Test.cs - Gbtc
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
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.SortedTreeStore.Services.Writer;

[TestFixture]
public class RolloverLogFile_Test
{
    [Test]
    public void TestLongTerm()
    {
        Logger.Console.Verbose = VerboseLevel.All;

        string file = @"C:\Temp\LogFileTest.log";

        List<Guid> source = new List<Guid>();
        source.Add(Guid.NewGuid());
        Guid dest = Guid.NewGuid();

        RolloverLogFile rolloverFile = new RolloverLogFile(file, source, dest);

        RolloverLogFile rolloverFile2 = new RolloverLogFile(file);

        Assert.AreEqual(rolloverFile.IsValid, rolloverFile2.IsValid);
        Assert.AreEqual(rolloverFile.DestinationFile, rolloverFile2.DestinationFile);
        Assert.AreEqual(rolloverFile.SourceFiles.Count, rolloverFile2.SourceFiles.Count);

        if (!rolloverFile2.SourceFiles.SequenceEqual(rolloverFile.SourceFiles))
            throw new Exception("Expecting equals.");



    }


}

