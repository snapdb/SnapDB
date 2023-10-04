//******************************************************************************************************
//  DebugStopwatch.cs - Gbtc
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
using System.Diagnostics;
using System.Runtime;

namespace SnapDB;

public class DebugStopwatch
{
    private readonly Stopwatch sw;

    public DebugStopwatch()
    {
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        sw = new Stopwatch();
    }

    public void DoGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public void Start(bool skipCollection = false)
    {
        if (skipCollection)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        sw.Restart();
    }

    public void Stop(double maximumTime)
    {
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.TotalMilliseconds <= maximumTime);
    }

    public void Stop(double minimumTime, double maximumTime)
    {
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.TotalMilliseconds >= minimumTime);
        Assert.IsTrue(sw.Elapsed.TotalMilliseconds <= maximumTime);
    }

    public double TimeEvent(Action function)
    {
        sw.Reset();
        GC.Collect();
        function();
        int count = 0;
        while (sw.Elapsed.TotalSeconds < .25)
        {
            sw.Start();
            function();
            sw.Stop();
            count++;
        }
        return sw.Elapsed.TotalSeconds / count;
    }
}