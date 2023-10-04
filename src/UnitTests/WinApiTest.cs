//******************************************************************************************************
//  WinApiTest.cs - Gbtc
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

namespace UnitTests;

/// <summary>
///This is a test class for WinApiTest and is intended
///to contain all WinApiTest Unit Tests
///</summary>
[TestFixture()]
public class WinApiTest
{
    /// <summary>
    ///A test for GetAvailableFreeSpace
    ///</summary>
    [Test()]
    public void GetAvailableFreeSpaceTest()
    {
        long freeSpace = 0;
        long totalSize = 0;
        bool actual;

        actual = FilePath.GetAvailableFreeSpace("C:\\", out freeSpace, out totalSize);
        Assert.AreEqual(true, freeSpace > 0);
        Assert.AreEqual(true, totalSize > 0);
        Assert.AreEqual(true, actual);

        actual = FilePath.GetAvailableFreeSpace("C:\\windows", out freeSpace, out totalSize);
        Assert.AreEqual(true, freeSpace > 0);
        Assert.AreEqual(true, totalSize > 0);
        Assert.AreEqual(true, actual);

        actual = FilePath.GetAvailableFreeSpace("\\\\htpc\\h", out freeSpace, out totalSize);
        Assert.AreEqual(true, freeSpace > 0);
        Assert.AreEqual(true, totalSize > 0);
        Assert.AreEqual(true, actual);

        //actual = WinApi.GetAvailableFreeSpace("G:\\Steam\\steamapps\\common", out freeSpace, out totalSize);
        //Assert.AreEqual(true, freeSpace > 0);
        //Assert.AreEqual(true, totalSize > 0);
        //Assert.AreEqual(true, actual);

        //actual = WinApi.GetAvailableFreeSpace("G:\\Steam\\steamapps\\common\\portal 2", out freeSpace, out totalSize); //ntfs symbolic link
        //Assert.AreEqual(true, freeSpace > 0);
        //Assert.AreEqual(true, totalSize > 0);
        //Assert.AreEqual(true, actual);

        //actual = WinApi.GetAvailableFreeSpace("P:\\", out freeSpace, out totalSize);
        //Assert.AreEqual(true, freeSpace > 0);
        //Assert.AreEqual(true, totalSize > 0);
        //Assert.AreEqual(true, actual);

        //actual = WinApi.GetAvailableFreeSpace("P:\\R Drive", out freeSpace, out totalSize); //mount point
        //Assert.AreEqual(true, freeSpace > 0);
        //Assert.AreEqual(true, totalSize > 0);
        //Assert.AreEqual(true, actual);

        actual = FilePath.GetAvailableFreeSpace("L:\\", out freeSpace, out totalSize); //Bad Location
        Assert.AreEqual(false, freeSpace > 0);
        Assert.AreEqual(false, totalSize > 0);
        Assert.AreEqual(false, actual);
    }
}