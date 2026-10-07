//******************************************************************************************************
//  PhasorRetrievalPerformanceTest.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance. All Rights Reserved.
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
//******************************************************************************************************
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Filters;
using SnapDB.Snap.Services;
using SnapDB.Snap.Services.Reader;
using SnapDB.Snap.Storage;
using SnapDB.UnitTests.Snap;
using SnapDB.UnitTests.Snap.Definitions;

namespace SnapDB.UnitTests.SortedTreeStore.Services
{
    /// <summary>
    /// Times retrieval of six hours of time-aligned synchrophasor data: 5 PMUs at 50 frames per second,
    /// each with 20 phasors (magnitude and angle), frequency, dF/dt and status flags.
    /// </summary>
    [TestFixture]
    public class PhasorRetrievalPerformanceTest
    {
        private const int DeviceCount = 5;
        private const int PhasorsPerDevice = 20;
        private const int PointsPerDevice = PhasorsPerDevice * 2 + 3; // Phasors + frequency, dF/dt, status flags
        private const int PointCount = DeviceCount * PointsPerDevice;
        private const int FramesPerSecond = 50;
        private const int Hours = 6;
        private const long FrameCount = Hours * 3600L * FramesPerSecond;
        private const ulong TicksPerFrame = TimeSpan.TicksPerSecond / FramesPerSecond;
        private const int Iterations = 3;

        // Generates every frame in key order without materializing the data set; all points in a
        // frame share one timestamp.
        private sealed class PhasorFrames : TreeStream<HistorianKey, HistorianValue>
        {
            private readonly ulong m_startTime;
            private long m_frame;
            private int m_point;
            private ulong m_noise = 0x9E3779B97F4A7C15UL;

            internal PhasorFrames(ulong startTime) { m_startTime = startTime; }
            public override bool IsAlwaysSequential => true;
            public override bool NeverContainsDuplicates => true;

            protected override bool ReadNext(HistorianKey key, HistorianValue value)
            {
                if (m_frame >= FrameCount) return false;
                key.Timestamp = m_startTime + (ulong)m_frame * TicksPerFrame;
                key.PointID = (ulong)m_point + 1;
                key.EntryNumber = 0;
                value.Value1 = BitConverter.SingleToUInt32Bits(Measurement(m_point / PointsPerDevice, m_point % PointsPerDevice));
                value.Value2 = 0;
                value.Value3 = 0;
                if (++m_point == PointCount) { m_point = 0; m_frame++; }
                return true;
            }

            private float Measurement(int device, int signal)
            {
                m_noise ^= m_noise << 13; m_noise ^= m_noise >> 7; m_noise ^= m_noise << 17;
                float noise = (int)(m_noise >> 40) / (float)(1 << 23) - 1.0F; // [-1, 1)
                if (signal == PointsPerDevice - 1) return 0.0F;                 // Status flags
                if (signal == PointsPerDevice - 2) return noise * 0.01F;        // dF/dt
                if (signal == PointsPerDevice - 3) return 60.0F + noise * 0.02F; // Frequency
                int phasor = signal / 2;
                if (signal % 2 == 0) return 199000.0F + device * 500 + phasor * 100 + noise * 50.0F; // Magnitude
                return (float)((m_frame * 0.36 + phasor * 120 + device * 7) % 360.0 - 180.0) + noise * 0.05F; // Angle
            }
        }

        private static void Time(string name, long expected, ArchiveList<HistorianKey, HistorianValue> archives,
            Func<SeekFilterBase<HistorianKey>> seek, MatchFilterBase<HistorianKey, HistorianValue> match = null)
        {
            double[] milliseconds = new double[Iterations];
            HistorianKey key = new HistorianKey();
            HistorianValue value = new HistorianValue();
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                long count = 0;
                Stopwatch timer = Stopwatch.StartNew();
                using (SequentialReaderStream<HistorianKey, HistorianValue> reader = new SequentialReaderStream<HistorianKey, HistorianValue>(archives, null, seek(), match))
                {
                    while (reader.Read(key, value)) count++;
                }
                timer.Stop();
                milliseconds[iteration] = timer.Elapsed.TotalMilliseconds;
                Assert.AreEqual(expected, count, name);
            }
            double best = milliseconds.Min();
            System.Console.WriteLine("{0}: {1:N0} points; runs (ms): {2}; best {3:N0} ms = {4:N1} million points/second", name, expected,
                string.Join(", ", milliseconds.Select(ms => ms.ToString("F0"))), best, expected / best / 1000.0);
        }

        [Test]
        [Explicit("Writes and reads a 232,200,000 point archive in the temporary folder.")]
        [Category("Performance")]
        public void SixHoursOfFivePhasorMeasurementUnitsAtFiftyFramesPerSecond()
        {
            string folder = Path.Combine(Path.GetTempPath(), "phasor-retrieval-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "phasors.d2");
            ulong start = (ulong)new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            ulong end = start + (ulong)(FrameCount - 1) * TicksPerFrame;
            ulong[] all = Enumerable.Range(1, PointCount).Select(id => (ulong)id).ToArray();
            ulong[] oneDevice = all.Take(PointsPerDevice).ToArray();
            try
            {
                Gemstone.Diagnostics.Logger.Console.Verbose = Gemstone.Diagnostics.VerboseLevel.None;
                Stopwatch timer = Stopwatch.StartNew();
                using (PhasorFrames stream = new PhasorFrames(start))
                    SortedTreeFileSimpleWriter<HistorianKey, HistorianValue>.Create(path + ".pending", path, 4096,
                        null, HistorianFileEncodingDefinition.TypeGuid, stream);
                timer.Stop();
                System.Console.WriteLine("Archive: {0} devices x {1} points x {2} frames = {3:N0} points; {4:N1} MiB; written in {5:N0} ms; 64-bit={6}; CLR={7}",
                    DeviceCount, PointsPerDevice, FrameCount, FrameCount * PointCount, new FileInfo(path).Length / 1048576.0,
                    timer.ElapsedMilliseconds, Environment.Is64BitProcess, Environment.Version);

                using (SortedTreeFile file = SortedTreeFile.OpenFile(path, true))
                using (ArchiveList<HistorianKey, HistorianValue> archives = new ArchiveList<HistorianKey, HistorianValue>(null))
                {
                    using (ArchiveListEditor<HistorianKey, HistorianValue> editor = archives.AcquireEditLock())
                        editor.Add(file.OpenTable<HistorianKey, HistorianValue>());

                    Time("Full resolution, all points", FrameCount * PointCount, archives,
                        () => TimestampSeekFilter.CreateFromRange<HistorianKey>(start, end));
                    Time("Full resolution, one device", FrameCount * PointsPerDevice, archives,
                        () => TimestampSeekFilter.CreateFromRange<HistorianKey>(start, end),
                        PointIDMatchFilter.CreateFromList<HistorianKey, HistorianValue>(oneDevice));
                    Time("Nearest to each second, all points", Hours * 3600L * PointCount, archives,
                        () => new NearestTimestampSeekFilter<HistorianKey>(start, end, (ulong)TimeSpan.TicksPerSecond, all));
                }
            }
            finally
            {
                foreach (string entry in Directory.GetFiles(folder)) File.Delete(entry);
                Directory.Delete(folder);
            }
        }
    }
}
