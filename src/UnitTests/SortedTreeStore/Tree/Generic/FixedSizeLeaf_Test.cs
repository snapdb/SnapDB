﻿//******************************************************************************************************
//  FixedSizeLeaf_Test.cs - Gbtc
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
using NUnit.Framework;
using SnapDB.Snap;
using SnapDB.Snap.Tree;
using SnapDB.Snap.Types;

namespace SnapDB.UnitTests.SortedTreeStore.Tree.Generic;

internal class SequentialTest : TreeNodeRandomizerBase<SnapUInt32, SnapUInt32>
{
    #region [ Members ]

    private uint m_current;
    private readonly List<KeyValuePair<uint, uint>> m_items = new();

    private int m_maxCount;
    private readonly SortedList<uint, uint> m_sortedItems = new();

    #endregion

    #region [ Methods ]

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

    public override void GetRandom(int index, SnapUInt32 key, SnapUInt32 value)
    {
        KeyValuePair<uint, uint> kvp = m_items[index];
        key.Value = kvp.Key;
        value.Value = kvp.Value;
    }

    public override void GetInSequence(int index, SnapUInt32 key, SnapUInt32 value)
    {
        key.Value = m_sortedItems.Keys[index];
        value.Value = m_sortedItems.Values[index];
    }

    #endregion
}

internal class ReverseSequentialTest : TreeNodeRandomizerBase<SnapUInt32, SnapUInt32>
{
    #region [ Members ]

    private uint m_current;
    private readonly List<KeyValuePair<uint, uint>> m_items = new();

    private int m_maxCount;
    private readonly SortedList<uint, uint> m_sortedItems = new();

    #endregion

    #region [ Methods ]

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

    public override void GetRandom(int index, SnapUInt32 key, SnapUInt32 value)
    {
        KeyValuePair<uint, uint> kvp = m_items[index];
        key.Value = kvp.Key;
        value.Value = kvp.Value;
    }

    public override void GetInSequence(int index, SnapUInt32 key, SnapUInt32 value)
    {
        key.Value = m_sortedItems.Keys[index];
        value.Value = m_sortedItems.Values[index];
    }

    #endregion
}

internal class RandomTest : TreeNodeRandomizerBase<SnapUInt32, SnapUInt32>
{
    #region [ Members ]

    private readonly List<KeyValuePair<uint, uint>> m_items = new();

    private Random m_r;
    private readonly SortedList<uint, uint> m_sortedItems = new();

    #endregion

    #region [ Methods ]

    public override void Reset(int maxCount)
    {
        m_r = new Random(1);
        m_sortedItems.Clear();
        m_items.Clear();
    }

    public override void Next()
    {
        uint rand = (uint)m_r.Next();
        m_sortedItems.Add(rand, rand * 2);
        m_items.Add(new KeyValuePair<uint, uint>(rand, rand * 2));
    }

    public override void GetRandom(int index, SnapUInt32 key, SnapUInt32 value)
    {
        KeyValuePair<uint, uint> kvp = m_items[index];
        key.Value = kvp.Key;
        value.Value = kvp.Value;
    }

    public override void GetInSequence(int index, SnapUInt32 key, SnapUInt32 value)
    {
        key.Value = m_sortedItems.Keys[index];
        value.Value = m_sortedItems.Values[index];
    }

    #endregion
}

[TestFixture]
public class FixedSizeLeafNodeTest
{
    #region [ Members ]

    private const int Max = 1000000;

    #endregion

    #region [ Methods ]

    [Test]
    public void TestSequentially()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestNode(tree, new SequentialTest(), 5000);
    }

    [Test]
    public void TestReverseSequentially()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestNode(tree, new ReverseSequentialTest(), 5000);
    }

    [Test]
    public void TestRandom()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestNode(tree, new RandomTest(), 2000);
    }

    [Test]
    public void BenchmarkSequentially()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestSpeed(tree, new SequentialTest(), 500, 512);
    }

    [Test]
    public void BenchmarkReverseSequentially()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestSpeed(tree, new ReverseSequentialTest(), 500, 512);
    }

    [Test]
    public void BenchmarkRandom()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestSpeed(tree, new RandomTest(), 500, 512);
    }

    [Test]
    public void BenchmarkBigRandom()
    {
        SortedTreeNodeBase<SnapUInt32, SnapUInt32> tree = Library.CreateTreeNode<SnapUInt32, SnapUInt32>(EncodingDefinition.FixedSizeCombinedEncoding, 0);

        LeafNodeTest.TestSpeed(tree, new RandomTest(), 5000, 4096);
    }

    #endregion
}