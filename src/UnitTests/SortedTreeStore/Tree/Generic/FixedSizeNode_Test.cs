﻿//******************************************************************************************************
//  FixedSizeNode_Test.cs - Gbtc
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
using System.Diagnostics;
using NUnit.Framework;
using SnapDB.IO.Unmanaged;
using SnapDB.Snap.Tree;
using SnapDB.Snap.Types;

namespace SnapDB.UnitTests.SortedTreeStore.Tree.Generic;

public static class ExtensionFixedSizeNodeUintUint
{
    #region [ Static ]

    public static uint Get(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key)
    {
        SnapUInt32 k = new(key);
        SnapUInt32 v = new();
        if (!tree.TryGet(k, v))
            throw new Exception();
        return v.Value;
    }

    public static bool TryGet(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key, out uint value)
    {
        SnapUInt32 k = new(key);
        SnapUInt32 v = new();
        bool rv = tree.TryGet(k, v);
        value = v.Value;
        return rv;
    }

    public static uint GetOrGetNext(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key)
    {
        SnapUInt32 k = new(key);
        SnapUInt32 v = new();
        tree.GetOrGetNext(k, v);
        return v.Value;
    }

    public static bool TryInsert(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key, uint value)
    {
        SnapUInt32 k = new(key);
        SnapUInt32 v = new(value);
        bool rv = tree.TryInsert(k, v);
        return rv;
    }

    public static void Insert(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key, uint value)
    {
        SnapUInt32 k = new(key);
        SnapUInt32 v = new(value);
        if (!tree.TryInsert(k, v))
            throw new Exception();
    }

    public static void GetFirstKeyValue(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, out uint key, out uint value)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetFirstRecord(k, v);
        key = k.Value;
        value = v.Value;
    }

    public static uint GetFirstKey(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetFirstRecord(k, v);
        return k.Value;
    }

    public static uint GetFirstValue(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetFirstRecord(k, v);
        return v.Value;
    }

    public static void GetLastKeyValue(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, out uint key, out uint value)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetLastRecord(k, v);
        key = k.Value;
        value = v.Value;
    }

    public static uint GetLastKey(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetLastRecord(k, v);
        return k.Value;
    }

    public static uint GetLastValue(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        SnapUInt32 k = new();
        SnapUInt32 v = new();
        tree.TryGetLastRecord(k, v);
        return v.Value;
    }

    public static bool KeyInsideBounds(this FixedSizeNode<SnapUInt32, SnapUInt32> tree, uint key)
    {
        SnapUInt32 k = new(key);
        return tree.IsKeyInsideBounds(k);
    }

    public static uint UpperKey(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        return tree.UpperKey.Value;
    }

    public static uint LowerKey(this FixedSizeNode<SnapUInt32, SnapUInt32> tree)
    {
        return tree.LowerKey.Value;
    }

    #endregion
}

[TestFixture]
public class FixedSizeNodeTest
{
    #region [ Members ]

    private const int Max = 1000000;

    #endregion

    #region [ Methods ]

    [Test]
    public void Test_CreateRootNode_Get()
    {
        uint rootKey = 0;
        byte rootLevel = 0;

        uint nextKeyIndex = 0;

        uint GetNextKey()
        {
            nextKeyIndex++;
            return nextKeyIndex - 1;
        }

        void AddToParent(SnapUInt32 int32, uint u, byte arg3)
        {
            int32 = int32;
        }

        uint FindLeafNode(SnapUInt32 int32)
        {
            return 0;
        }

        Stopwatch swWrite = new();
        Stopwatch swRead = new();
        using BinaryStream bs = new();
        uint k, v;
        FixedSizeNode<SnapUInt32, SnapUInt32> node = new(0);

        node.Initialize(bs, 1024, GetNextKey, null);

        node.CreateEmptyNode(0);
        node.Insert(1, 100);
        node.Insert(2, 200);

        Assert.AreEqual(0u, node.NodeIndex);
        Assert.AreEqual(uint.MaxValue, node.LeftSiblingNodeIndex);
        Assert.AreEqual(uint.MaxValue, node.RightSiblingNodeIndex);

        Assert.AreEqual(1u, node.GetFirstKey());
        Assert.AreEqual(100u, node.GetFirstValue());
        Assert.AreEqual(2u, node.GetLastKey());
        Assert.AreEqual(200u, node.GetLastValue());

        Assert.AreEqual(100u, node.Get(1));
        Assert.AreEqual(200u, node.Get(2));
        Assert.AreEqual(100u, node.GetOrGetNext(1));
        Assert.AreEqual(200u, node.GetOrGetNext(2));
        Assert.AreEqual(200u, node.GetOrGetNext(3));

        node.SetNodeIndex(0);

        Assert.AreEqual(0u, node.NodeIndex);
        Assert.AreEqual(uint.MaxValue, node.LeftSiblingNodeIndex);
        Assert.AreEqual(uint.MaxValue, node.RightSiblingNodeIndex);

        Assert.AreEqual(1u, node.GetFirstKey());
        Assert.AreEqual(100u, node.GetFirstValue());
        Assert.AreEqual(2u, node.GetLastKey());
        Assert.AreEqual(200u, node.GetLastValue());

        Assert.AreEqual(100u, node.Get(1));
        Assert.AreEqual(200u, node.Get(2));
        Assert.AreEqual(100u, node.GetOrGetNext(1));
        Assert.AreEqual(200u, node.GetOrGetNext(2));
        Assert.AreEqual(200u, node.GetOrGetNext(3));
    }

    #endregion

    //[Test]
    //public void Test_Split()
    //{
    //    uint rootKey = 0;
    //    byte rootLevel = 0;

    //    uint nextKeyIndex = 0;
    //    Func<uint> getNextKey = () =>
    //    {
    //        nextKeyIndex++;
    //        return nextKeyIndex - 1;
    //    };

    //    Stopwatch swWrite = new Stopwatch();
    //    Stopwatch swRead = new Stopwatch();
    //    using (var bs = new BinaryStream())
    //    {
    //        var node = new SparseIndex<KeyValue256, KeyValue256Methods>.Node(bs, 1024, 1);
    //        var k0 = new KeyValue256();
    //        var k1 = new KeyValue256();
    //        var k2 = new KeyValue256();
    //        var k3 = new KeyValue256();
    //        var k4 = new KeyValue256();
    //        var k5 = new KeyValue256();
    //        k0.Key1 = 1;
    //        k1.Key1 = 1000;
    //        k2.Key1 = 2000;
    //        k3.Key1 = 3000;
    //        k4.Key1 = 4000;
    //        k5.Key1 = 5000;

    //        node.CreateRootNode(0, 1, 100, k2, 2000);
    //        node.Insert(k5, 5000);
    //        node.Insert(k3, 3000);
    //        node.Insert(k1, 1000);
    //        node.Insert(k4, 4000);

    //        node.Split(1);

    //        Assert.AreEqual(uint.MaxValue, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(1u, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(100u, node.GetFirstPointer());
    //        Assert.AreEqual(2000u, node.GetLastPointer());
    //        Assert.AreEqual(0ul, node.LowerKey.Key1);
    //        Assert.AreEqual(3000ul, node.UpperKey.Key1);

    //        Assert.AreEqual(100u, node.Get(k0));
    //        Assert.AreEqual(1000u, node.Get(k1));
    //        Assert.AreEqual(2000u, node.Get(k2));
    //        Assert.AreEqual(2000u, node.Get(k3));
    //        Assert.AreEqual(2000u, node.Get(k4));
    //        Assert.AreEqual(2000u, node.Get(k5));

    //        node.SeekToRightSibling();

    //        Assert.AreEqual(0u, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(uint.MaxValue, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(3000u, node.GetFirstPointer());
    //        Assert.AreEqual(5000u, node.GetLastPointer());
    //        Assert.AreEqual(3000ul, node.LowerKey.Key1);
    //        Assert.AreEqual(0ul, node.UpperKey.Key1);

    //        Assert.AreEqual(3000u, node.Get(k0));
    //        Assert.AreEqual(3000u, node.Get(k1));
    //        Assert.AreEqual(3000u, node.Get(k2));
    //        Assert.AreEqual(3000u, node.Get(k3));
    //        Assert.AreEqual(4000u, node.Get(k4));
    //        Assert.AreEqual(5000u, node.Get(k5));

    //        node.SeekToLeftSibling();

    //        Assert.AreEqual(uint.MaxValue, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(1u, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(100u, node.GetFirstPointer());
    //        Assert.AreEqual(2000u, node.GetLastPointer());
    //        Assert.AreEqual(0ul, node.LowerKey.Key1);
    //        Assert.AreEqual(3000ul, node.UpperKey.Key1);

    //        Assert.AreEqual(100u, node.Get(k0));
    //        Assert.AreEqual(1000u, node.Get(k1));
    //        Assert.AreEqual(2000u, node.Get(k2));
    //        Assert.AreEqual(2000u, node.Get(k3));
    //        Assert.AreEqual(2000u, node.Get(k4));
    //        Assert.AreEqual(2000u, node.Get(k5));


    //        node.Insert(k3, 303);
    //        node.Insert(k4, 404);
    //        node.Insert(k5, 505);

    //        node.Split(2);

    //        Assert.AreEqual(uint.MaxValue, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(2u, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(100u, node.GetFirstPointer());
    //        Assert.AreEqual(2000u, node.GetLastPointer());
    //        Assert.AreEqual(0ul, node.LowerKey.Key1);
    //        Assert.AreEqual(3000ul, node.UpperKey.Key1);

    //        Assert.AreEqual(100u, node.Get(k0));
    //        Assert.AreEqual(1000u, node.Get(k1));
    //        Assert.AreEqual(2000u, node.Get(k2));
    //        Assert.AreEqual(2000u, node.Get(k3));
    //        Assert.AreEqual(2000u, node.Get(k4));
    //        Assert.AreEqual(2000u, node.Get(k5));

    //        node.SeekToRightSibling();

    //        Assert.AreEqual(0u, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(1u, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(303u, node.GetFirstPointer());
    //        Assert.AreEqual(505u, node.GetLastPointer());
    //        Assert.AreEqual(3000ul, node.LowerKey.Key1);
    //        Assert.AreEqual(3000ul, node.UpperKey.Key1);

    //        Assert.AreEqual(303u, node.Get(k0));
    //        Assert.AreEqual(303u, node.Get(k1));
    //        Assert.AreEqual(303u, node.Get(k2));
    //        Assert.AreEqual(303u, node.Get(k3));
    //        Assert.AreEqual(404u, node.Get(k4));
    //        Assert.AreEqual(505u, node.Get(k5));

    //        node.SeekToRightSibling();

    //        Assert.AreEqual(2u, node.LeftSiblingNodeIndex);
    //        Assert.AreEqual(uint.MaxValue, node.RightSiblingNodeIndex);
    //        Assert.AreEqual((ushort)2, node.RecordCount);
    //        Assert.AreEqual(3000u, node.GetFirstPointer());
    //        Assert.AreEqual(5000u, node.GetLastPointer());
    //        Assert.AreEqual(3000ul, node.LowerKey.Key1);
    //        Assert.AreEqual(0ul, node.UpperKey.Key1);

    //        Assert.AreEqual(3000u, node.Get(k0));
    //        Assert.AreEqual(3000u, node.Get(k1));
    //        Assert.AreEqual(3000u, node.Get(k2));
    //        Assert.AreEqual(3000u, node.Get(k3));
    //        Assert.AreEqual(4000u, node.Get(k4));
    //        Assert.AreEqual(5000u, node.Get(k5));
    //    }
    //}
}