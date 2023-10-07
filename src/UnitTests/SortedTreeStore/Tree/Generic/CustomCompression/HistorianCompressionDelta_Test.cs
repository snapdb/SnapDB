//******************************************************************************************************
//  HistorianCompressionDelta_Test.cs - Gbtc
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
using System.Collections.Generic;
using SnapDB.UnitTests.Snap;

namespace SnapDB.UnitTests.SortedTreeStore.Tree.Generic.CustomCompression;

internal class SequentialTest
 : TreeNodeRandomizerBase<HistorianKey, HistorianValue>
{
    private readonly SortedList<uint, uint> m_sortedItems = new();
    private readonly List<KeyValuePair<uint, uint>> m_items = new();

    private int m_maxCount;
    private uint m_current = 0;

    public override void Reset(int maxCount)
    {
        m_sortedItems.Clear();
        m_items.Clear();
        m_maxCount = maxCount;
        m_current = 1;
    }

    public override void Next()
    {
        m_sortedItems.Add(m_current, m_current * 2);
        m_items.Add(new KeyValuePair<uint, uint>(m_current, m_current * 2));
        m_current++;
    }

    public override void GetRandom(int index, HistorianKey key, HistorianValue value)
    {
        KeyValuePair<uint, uint> kvp = m_items[index];
        key.Timestamp = kvp.Key;
        value.Value1 = kvp.Value;
    }

    public override void GetInSequence(int index, HistorianKey key, HistorianValue value)
    {
        key.Timestamp = m_sortedItems.Keys[index];
        value.Value1 = m_sortedItems.Values[index];
    }
}

internal class ReverseSequentialTest
    : TreeNodeRandomizerBase<HistorianKey, HistorianValue>
{
    private readonly SortedList<uint, uint> m_sortedItems = new();
    private readonly List<KeyValuePair<uint, uint>> m_items = new();

    private int m_maxCount;
    private uint m_current = 0;

    public override void Reset(int maxCount)
    {
        m_sortedItems.Clear();
        m_items.Clear();
        m_maxCount = maxCount;
        m_current = (uint)maxCount;
    }

    public override void Next()
    {
        m_sortedItems.Add(m_current, m_current * 2);
        m_items.Add(new KeyValuePair<uint, uint>(m_current, m_current * 2));
        m_current--;
    }

    public override void GetRandom(int index, HistorianKey key, HistorianValue value)
    {
        KeyValuePair<uint, uint> kvp = m_items[index];
        key.Timestamp = kvp.Key;
        value.Value1 = kvp.Value;
    }

    public override void GetInSequence(int index, HistorianKey key, HistorianValue value)
    {
        key.Timestamp = m_sortedItems.Keys[index];
        value.Value1 = m_sortedItems.Values[index];
    }
}

internal class RandomTest
    : TreeNodeRandomizerBase<HistorianKey, HistorianValue>
{
    private readonly SortedList<ulong, ulong> m_sortedItems = new();
    private readonly List<KeyValuePair<ulong, ulong>> m_items = new();

    private Random r;

    public override void Reset(int maxCount)
    {
        r = new Random(1);
        m_sortedItems.Clear();
        m_items.Clear();
    }

    public override void Next()
    {
        ulong rand = (((ulong)r.Next()) << 33) | (uint)r.Next();
        m_sortedItems.Add(rand, rand * 2);
        m_items.Add(new KeyValuePair<ulong, ulong>(rand, rand * 2));
    }

    public override void GetRandom(int index, HistorianKey key, HistorianValue value)
    {
        KeyValuePair<ulong, ulong> kvp = m_items[index];
        key.Timestamp = kvp.Key;
        value.Value1 = kvp.Value;
    }

    public override void GetInSequence(int index, HistorianKey key, HistorianValue value)
    {
        key.Timestamp = m_sortedItems.Keys[index];
        value.Value1 = m_sortedItems.Values[index];
    }
}

//[TestFixture]
//class HistorianCompressionDelta
//{
//    private const int Max = 1000000;

//    [Test]
//    public void TestSequently()
//    {
//        var tree = TreeNodeInitializer.CreateTreeNode<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid, 0);

//        LeafNodeTest.TestNode(tree, new SequentialTest(), 5000);
//    }

//    [Test]
//    public void TestReverseSequently()
//    {
//        var tree = TreeNodeInitializer.CreateTreeNode<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid, 0);

//        LeafNodeTest.TestNode(tree, new ReverseSequentialTest(), 5000);
//    }

//    [Test]
//    public void TestRandom()
//    {
//        var tree = TreeNodeInitializer.CreateTreeNode<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid, 0);

//        LeafNodeTest.TestNode(tree, new RandomTest(), 2000);
//    }

//    [Test]
//    public void BenchmarkSequently()
//    {
//        var tree = TreeNodeInitializer.GetTreeNodeInitializer<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid);

//        LeafNodeTest.TestSpeed(tree, new SequentialTest(), 500, 512);
//    }

//    [Test]
//    public void BenchmarkReverseSequently()
//    {
//        var tree = TreeNodeInitializer.GetTreeNodeInitializer<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid);

//        LeafNodeTest.TestSpeed(tree, new ReverseSequentialTest(), 500, 512);
//    }

//    [Test]
//    public void BenchmarkRandom()
//    {
//        var tree = TreeNodeInitializer.GetTreeNodeInitializer<HistorianKey, HistorianValue>(CreateHistorianCompressionDelta.TypeGuid);

//        LeafNodeTest.TestSpeed(tree, new RandomTest(), 500, 512);
//    }
//}
