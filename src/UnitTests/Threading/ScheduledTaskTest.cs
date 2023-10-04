//******************************************************************************************************
//  ScheduledTaskTest.cs - Gbtc
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
using System.Threading;

namespace UnitTests.Threading;

[TestFixture]
public class ScheduledTaskTest
{
    [Test]
    public void TestDisposed()
    {
        int count = 0;
        ScheduledTask worker = new ScheduledTask(ThreadingMode.DedicatedForeground);
        WeakReference workerWeak = new WeakReference(worker);
        worker = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        worker = (ScheduledTask)workerWeak.Target;
        Assert.IsNull(worker);
    }

    [Test]
    public void TestDisposedNested()
    {
        int count = 0;
        NestedDispose worker = new NestedDispose();
        WeakReference workerWeak = new WeakReference(worker);
        worker = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        worker = (NestedDispose)workerWeak.Target;
        Assert.IsNull(worker);
    }


    private class NestedDispose
    {
        public readonly ScheduledTask worker;

        public NestedDispose()
        {
            worker = new ScheduledTask(ThreadingMode.DedicatedForeground);
            worker.Running += Method;
        }

        private void Method(object sender, EventArgs eventArgs)
        {
        }
    }


    [Test]
    public void Test()
    {
        using (ScheduledTask work = new ScheduledTask(ThreadingMode.DedicatedForeground))
        {
            work.Running += work_DoWork;
            work.Running += work_CleanupWork;
            work.Start();
        }
        double x = 1;
        while (x > 3)
        {
            x--;
        }
    }

    private void work_CleanupWork(object sender, EventArgs eventArgs)
    {
        Thread.Sleep(100);
    }

    private void work_DoWork(object sender, EventArgs eventArgs)
    {
        Thread.Sleep(100);
    }
}