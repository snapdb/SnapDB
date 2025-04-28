//******************************************************************************************************
//  StepTimer.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  12/19/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Diagnostics;
using System.Text;

namespace SnapDB;

/// <summary>
/// A utility class for measuring and analyzing the execution time of code segments.
/// </summary>
public static class StepTimer
{
    #region [ Members ]

    private class RunCount : ITimer
    {
        #region [ Members ]

        public readonly List<double> RunResults = [];
        public readonly Stopwatch Sw = new();

        #endregion

        #region [ Methods ]

        public void Stop(int loopCount = 1)
        {
            Sw.Stop();
            RunResults.Add(Sw.Elapsed.TotalSeconds / loopCount);
        }

        #endregion
    }

    /// <summary>
    /// Represents an interface for measuring and recording the execution time of code segments.
    /// </summary>
    public interface ITimer
    {
        #region [ Methods ]

        /// <summary>
        /// Stops the timer and records the elapsed time for the code segment.
        /// </summary>
        /// <param name="loopCount">The number of times the code segment was executed (default is 1).</param>
        void Stop(int loopCount = 1);

        #endregion
    }

    #endregion

    #region [ Static ]

    private static readonly SortedList<string, RunCount> s_allStopwatches;

    static StepTimer()
    {
        s_allStopwatches = new SortedList<string, RunCount>();
    }

    /// <summary>
    /// Starts a named timer and optionally forces garbage collection before starting.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="runGc">Indicates whether to force garbage collection before starting the timer.</param>
    /// <returns>An object implementing the ITimer interface that represents the started timer.</returns>
    public static ITimer Start(string name, bool runGc = false)
    {
        if (!s_allStopwatches.ContainsKey(name))
            s_allStopwatches.Add(name, new RunCount());

        if (runGc)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        RunCount sw = s_allStopwatches[name];
        sw.Sw.Restart();

        return sw;
    }

    /// <summary>
    /// Resets all timers, clearing their recorded data.
    /// </summary>
    public static void Reset()
    {
        s_allStopwatches.Clear();
    }

    /// <summary>
    /// Calculates and returns the average execution time recorded by a timer with the specified name.
    /// </summary>
    /// <param name="name">The name of the timer for which to calculate the average.</param>
    /// <returns>The average execution time in seconds.</returns>
    public static double GetAverage(string name)
    {
        RunCount kvp = s_allStopwatches[name];
        kvp.RunResults.Sort();

        return kvp.RunResults[kvp.RunResults.Count >> 1];
    }

    /// <summary>
    /// Calculates and returns the average execution time in nanoseconds recorded by a timer with the specified name.
    /// </summary>
    /// <param name="name">The name of the timer for which to calculate the average.</param>
    /// <param name="loopCount">The number of iterations or loops used during measurements.</param>
    /// <returns>The average execution time in nanoseconds.</returns>
    public static double GetNanoSeconds(string name, int loopCount)
    {
        RunCount kvp = s_allStopwatches[name];
        kvp.RunResults.Sort();

        return kvp.RunResults[kvp.RunResults.Count >> 1] * 1000000000.0 / loopCount;
    }

    /// <summary>
    /// Calculates and returns the slowest recorded execution time (90th percentile) in seconds by a timer with the specified name.
    /// </summary>
    /// <param name="name">The name of the timer for which to calculate the slowest execution time.</param>
    /// <returns>The slowest recorded execution time in seconds.</returns>
    public static double GetSlowest(string name)
    {
        RunCount kvp = s_allStopwatches[name];
        kvp.RunResults.Sort();

        return kvp.RunResults[(int)(kvp.RunResults.Count * 0.9)];
    }

    /// <summary>
    /// Generates a summary of recorded execution times for all timers and returns the results as a formatted string.
    /// </summary>
    /// <returns>A string containing the summary of recorded execution times for all timers.</returns>
    public static string GetResults()
    {
        StringBuilder sb = new();
        foreach (KeyValuePair<string, RunCount> kvp in s_allStopwatches)
        {
            kvp.Value.RunResults.Sort();
            double rate = kvp.Value.RunResults[kvp.Value.RunResults.Count >> 1];
            sb.Append(kvp.Key + '\t' + (rate / 1000000).ToString("0.00"));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Measures the execution time of an action and returns the median execution time in microseconds.
    /// </summary>
    /// <param name="internalLoopCount">The number of internal loops used for timing.</param>
    /// <param name="del">The action to be timed.</param>
    /// <returns>The median execution time of the action in microseconds.</returns>
    public static string Time(int internalLoopCount, Action del)
    {
        Stopwatch sw = new();

        int innerLoopCount = 1;

        // Primary loop.
        del();
        Thread.Sleep(1);
        del();
        Thread.Sleep(1);
        del();

        // Build an inner loop that takes at least 3 ms to complete.
        while (TimeLoop(sw, del, innerLoopCount) < 3)
            innerLoopCount *= 2;

        List<double> list = [];

        for (int x = 0; x < 100; x++)
        {
            if (x % 10 == 0)
                Thread.Sleep(1);
            list.Add(TimeLoop(sw, del, innerLoopCount));
        }

        list.Sort();

        return (list[list.Count >> 2] * 1000000.0 / innerLoopCount / internalLoopCount).ToString("0.0");
    }

    private static double TimeLoop(Stopwatch sw, Action del, int loopCount)
    {
        sw.Restart();

        for (int x = 0; x < loopCount; x++)
            del();

        sw.Stop();

        return sw.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Measures the execution time of an action that takes a Stopwatch parameter
    /// and returns the median execution time in microseconds.
    /// </summary>
    /// <param name="internalLoopCount">The number of internal loops used for timing.</param>
    /// <param name="del">The action to be timed, which takes a Stopwatch parameter.</param>
    /// <returns>The median execution time of the action in microseconds.</returns>
    public static string Time(int internalLoopCount, Action<Stopwatch> del)
    {
        Stopwatch sw = new();

        int innerLoopCount = 1;

        // Primary loop
        del(sw);
        Thread.Sleep(1);
        del(sw);
        Thread.Sleep(1);
        del(sw);

        // Build an inner loop that takes at least 3 ms to complete.
        while (TimeLoop(sw, del, innerLoopCount) < 3)
            innerLoopCount *= 2;

        List<double> list = [];

        for (int x = 0; x < 100; x++)
        {
            if (x % 10 == 0)
                Thread.Sleep(1);
            list.Add(TimeLoop(sw, del, innerLoopCount));
        }

        list.Sort();

        return (list[list.Count >> 2] * 1000000.0 / innerLoopCount / internalLoopCount).ToString("0.0");
    }

    private static double TimeLoop(Stopwatch sw, Action<Stopwatch> del, int loopCount)
    {
        sw.Reset();

        for (int x = 0; x < loopCount; x++)
            del(sw);

        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion
}