//******************************************************************************************************
//  LeafNodeTest.cs - Gbtc
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
using System.Text;

namespace UnitTests.SortedTreeStore.Tree.Generic;

public abstract class TreeNodeRandomizerBase<TKey, TValue>
    where TKey : class, new()
    where TValue : class, new()
{
    public abstract void Reset(int maxCount);
    public abstract void Next();
    public abstract void GetRandom(int index, TKey key, TValue value);
    public abstract void GetInSequence(int index, TKey key, TValue value);
}

public class LeafNodeTest
{
    private const int Max = 1000000;

    public static void TestNode<TKey, TValue>(SortedTreeNodeBase<TKey, TValue> node, TreeNodeRandomizerBase<TKey, TValue> randomizer, int count)
        where TKey : SnapTypeBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        int Max = count;
        uint rootKey = 0;
        byte rootLevel = 0;

        uint nextKeyIndex = 2;
        bool hasChanged = false;
        Func<uint> getNextKey = () =>
        {
            nextKeyIndex++;
            return nextKeyIndex - 1;
        };

        StringBuilder sb = new StringBuilder();

        using (BinaryStream bs = new BinaryStream())
        {
            const int pageSize = 512;
            SparseIndex<TKey> sparse = new SparseIndex<TKey>();
            sparse.Initialize(bs, pageSize, getNextKey, 0, 1);
            node.Initialize(bs, pageSize, getNextKey, sparse);
            node.CreateEmptyNode(1);

            TKey key = new TKey();
            TKey key2 = new TKey();
            TValue value = new TValue();
            TValue value2 = new TValue();

            randomizer.Reset(Max);
            for (int x = 0; x < Max; x++)
            {
                randomizer.Next();
                //Add the next point
                randomizer.GetRandom(x, key, value);

                //node.WriteNodeData(sb);

                if (!node.TryInsert(key, value))
                    throw new Exception();

                //node.WriteNodeData(sb);
                //File.WriteAllText("c:\\temp\\temp.log", sb.ToString());


                //Check if all points exist
                for (int y = 0; y <= x; y++)
                {
                    randomizer.GetRandom(y, key, value);
                    if (!node.TryGet(key, value2))
                        throw new Exception();
                    if (!value.IsEqualTo(value2))
                        throw new Exception();
                }

                //Check if scanner works.
                SortedTreeScannerBase<TKey, TValue> scanner = node.CreateTreeScanner();
                scanner.SeekToStart();
                for (int y = 0; y <= x; y++)
                {
                    randomizer.GetInSequence(y, key, value);
                    if (!scanner.Read(key2, value2))
                        throw new Exception();
                    if (!key.IsEqualTo(key2))
                        throw new Exception();
                    if (!value.IsEqualTo(value2))
                        throw new Exception();
                }
                if (scanner.Read(key2, value2))
                    throw new Exception();
            }

            node = node;
        }
    }


    internal static void TestSpeed<TKey, TValue>(SortedTreeNodeBase<TKey, TValue> nodeInitializer, TreeNodeRandomizerBase<TKey, TValue> randomizer, int count, int pageSize)
        where TKey : SnapTypeBase<TKey>, new()
        where TValue : SnapTypeBase<TValue>, new()
    {
        int Max = count;

        uint nextKeyIndex = 2;
        Func<uint> getNextKey = () =>
        {
            nextKeyIndex++;
            return nextKeyIndex - 1;
        };


        using (BinaryStream bs = new BinaryStream())
        {
            randomizer.Reset(Max);
            for (int x = 0; x < Max; x++)
            {
                randomizer.Next();
            }

            TKey key = new TKey();
            TValue value = new TValue();
            SortedTreeNodeBase<TKey, TValue> node = null;

            System.Console.WriteLine(StepTimer.Time(count, (sw) =>
            {
                nextKeyIndex = 2;
                node = nodeInitializer.Clone(0);
                SparseIndex<TKey> sparse = new SparseIndex<TKey>();
                sparse.Initialize(bs, pageSize, getNextKey, 0, 1);
                node.Initialize(bs, pageSize, getNextKey, sparse);
                node.CreateEmptyNode(1);
                sw.Start();
                for (int x = 0; x < Max; x++)
                {
                    //Add the next point
                    randomizer.GetRandom(x, key, value);

                    if (!node.TryInsert(key, value))
                        throw new Exception();
                }
                sw.Stop();
            }));


            System.Console.WriteLine(StepTimer.Time(count, () =>
            {
                for (int x = 0; x < Max; x++)
                {
                    //Add the next point
                    randomizer.GetRandom(x, key, value);

                    if (!node.TryGet(key, value))
                        throw new Exception();
                }
            }));



            System.Console.WriteLine(StepTimer.Time(count, () =>
            {
                SortedTreeScannerBase<TKey, TValue> scanner = node.CreateTreeScanner();
                scanner.SeekToStart();
                while (scanner.Read(key, value))
                    ;
            }));

            node = node;
        }
    }
}