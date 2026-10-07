//******************************************************************************************************
//  ArchivePointIDIndex.cs - Gbtc
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using SnapDB.Snap.Types;

namespace SnapDB.Snap.Storage
{
    // Optional, exact presence synopsis for a finalized default timestamp/point-ID table.
    // Missing, incomplete, stale or corrupt indexes must never become absence evidence.
    internal static class ArchivePointIDIndex
    {
        internal const string Extension = ".pidx";
        internal const int MaximumIDs = 4000000;
        private const int Magic = 0x31444950; // PID1; little endian, version 1

        internal static ulong[]? Load(string path, Guid archive, uint sequence, Guid keyType, Guid valueType)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                using (FileStream file = File.OpenRead(path + Extension))
                {
                    // Bound both compressed input and decompressed output before allocating.
                    if (file.Length < 32 || file.Length > MaximumIDs * 8L + 4096) return null;
                    byte[] expected = new byte[32];
                    if (file.Read(expected, 0, expected.Length) != expected.Length) return null;
                    byte[] payload;
                    using (MemoryStream buffer = new MemoryStream())
                    using (DeflateStream compressed = new DeflateStream(file, CompressionMode.Decompress))
                    {
                        byte[] chunk = new byte[8192];
                        int read;
                        while ((read = compressed.Read(chunk, 0, chunk.Length)) != 0)
                        {
                            if (buffer.Length + read > MaximumIDs * 8L + 68) return null;
                            buffer.Write(chunk, 0, read);
                        }
                        payload = buffer.ToArray();
                    }
                    using (SHA256 hash = SHA256.Create())
                        if (!hash.ComputeHash(payload).SequenceEqual(expected)) return null;
                    using (BinaryReader reader = new BinaryReader(new MemoryStream(payload)))
                    {
                        if (reader.ReadInt32() != Magic || new Guid(reader.ReadBytes(16)) != archive ||
                            reader.ReadUInt32() != sequence || new Guid(reader.ReadBytes(16)) != keyType ||
                            new Guid(reader.ReadBytes(16)) != valueType || reader.ReadInt64() != new FileInfo(path).Length)
                            return null;
                        int count = reader.ReadInt32();
                        if (count < 0 || count > MaximumIDs || payload.Length != 68L + count * 8L) return null;
                        ulong[] ids = new ulong[count];
                        for (int i = 0; i < count; i++)
                        {
                            ids[i] = reader.ReadUInt64();
                            if (i > 0 && ids[i] <= ids[i - 1]) return null;
                        }
                        return ids;
                    }
                }
            }
            catch (InvalidDataException) { return null; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (ArgumentException) { return null; }
        }

        internal static void Save(string path, Guid archive, uint sequence, Guid keyType, Guid valueType, IEnumerable<ulong> ids)
        {
            string temporary = path + Extension + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                ulong[] ordered = ids.OrderBy(id => id).ToArray();
                byte[] payload;
                using (MemoryStream buffer = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
                    {
                        writer.Write(Magic);
                        writer.Write(archive.ToByteArray());
                        writer.Write(sequence);
                        writer.Write(keyType.ToByteArray());
                        writer.Write(valueType.ToByteArray());
                        writer.Write(new FileInfo(path).Length);
                        writer.Write(ordered.Length);
                        foreach (ulong id in ordered) writer.Write(id);
                    }
                    payload = buffer.ToArray();
                }
                using (FileStream file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (SHA256 hash = SHA256.Create())
                    {
                        byte[] checksum = hash.ComputeHash(payload);
                        file.Write(checksum, 0, checksum.Length);
                    }
                    using (DeflateStream compressed = new DeflateStream(file, CompressionLevel.Optimal, true))
                        compressed.Write(payload, 0, payload.Length);
                    file.Flush(true);
                }
                // Publish only after the archive and complete synopsis are durable. An existing
                // sidecar is left alone: its identity will be checked by every new snapshot.
                File.Move(temporary, path + Extension);
            }
            catch (IOException) { /* Optional optimization; the committed archive remains usable. */ }
            catch (UnauthorizedAccessException) { }
            finally { Delete(temporary, false); }
        }

        internal static void Delete(string path, bool appendExtension = true)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { File.Delete(path + (appendExtension ? Extension : "")); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        internal static void Move(string oldPath, string newPath)
        {
            if (string.IsNullOrEmpty(oldPath) || oldPath == newPath) return;
            try { File.Move(oldPath + Extension, newPath + Extension); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // Does not own the input stream. A disabled/oversized collector leaves no index.
    internal sealed class PointIDCollectingStream<TKey, TValue> : TreeStream<TKey, TValue>
        where TKey : SnapTypeBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        private readonly TreeStream<TKey, TValue> m_source;
        private HashSet<ulong>? m_ids;
        private bool m_complete;
        private readonly ArchiveTimeBucketBloomIndex.Builder? m_bloom;
        internal PointIDCollectingStream(TreeStream<TKey, TValue> source, TimeBucketBloomFilterSettings? bloom = null, bool enablePointIDIndex = true)
        {
            m_source = source ?? throw new ArgumentNullException(nameof(source));
            if (new TKey() is TimestampPointIDBase<TKey>)
            {
                if (enablePointIDIndex) m_ids = new HashSet<ulong>();
                if (bloom != null && bloom.Enabled) m_bloom = new ArchiveTimeBucketBloomIndex.Builder(bloom);
            }
        }
        public override bool IsAlwaysSequential => m_source.IsAlwaysSequential;
        public override bool NeverContainsDuplicates => m_source.NeverContainsDuplicates;
        protected override bool ReadNext(TKey key, TValue value)
        {
            if (!m_source.Read(key, value)) { m_complete = true; return false; }
            if (m_ids != null)
            {
                m_ids.Add(((TimestampPointIDBase<TKey>)(object)key).PointID);
                if (m_ids.Count > ArchivePointIDIndex.MaximumIDs) m_ids = null;
            }
            if (m_bloom != null)
            {
                TimestampPointIDBase<TKey> timeKey = (TimestampPointIDBase<TKey>)(object)key;
                m_bloom.Add(timeKey.Timestamp, timeKey.PointID);
            }
            return true;
        }
        internal void Publish(string path, Guid archive, uint sequence)
        {
            if (m_complete && m_ids != null)
                ArchivePointIDIndex.Save(path, archive, sequence, new TKey().GenericTypeGuid, new TValue().GenericTypeGuid, m_ids);
            if (m_complete && m_bloom != null)
                m_bloom.Complete()?.Save(path, archive, sequence, new TKey().GenericTypeGuid, new TValue().GenericTypeGuid);
        }
    }
}
