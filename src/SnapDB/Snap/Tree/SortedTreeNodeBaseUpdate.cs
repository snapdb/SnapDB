//******************************************************************************************************
//  SortedTreeNodeBase_Update`2.cs - Gbtc
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
//  04/16/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Snap.Tree;

public unsafe partial class SortedTreeNodeBase<TKey, TValue>
{
    #region [ Methods ]

    /// <summary>
    /// Updates the key of a non-leaf node.
    /// </summary>
    /// <param name="oldKey">The old key to be updated.</param>
    /// <param name="newKey">The new key to replace the old key.</param>
    public void UpdateKey(TKey oldKey, TKey newKey)
    {
        if (Level == 0)
            throw new NotSupportedException("Cannot update key at the leaf level.");

        NavigateToNode(oldKey);

        if (newKey.IsLessThan(LowerKey))
            throw new Exception("Should never be here");
        if (newKey.IsGreaterThan(UpperKey))
            throw new Exception("Should never be here");

        InternalUpdateKey(oldKey, newKey);
    }

    /// <summary>
    /// Updates the value associated with a key in a non-leaf node.
    /// </summary>
    /// <param name="key">The key associated with the value to update.</param>
    /// <param name="value">The new value to replace the old value.</param>
    public void UpdateValue(TKey key, TValue value)
    {
        if (Level == 0)
            throw new NotSupportedException("Cannot update key at the leaf level.");

        NavigateToNode(key);

        InternalUpdateValue(key, value);
    }

    /// <summary>
    /// Performs the internal update of a key in a non-leaf node.
    /// </summary>
    /// <param name="oldKey">The old key to be updated.</param>
    /// <param name="newKey">The new key to replace the old key.</param>
    private void InternalUpdateKey(TKey oldKey, TKey newKey)
    {
        //ToDo: Make this implementation generic
        byte* ptr = GetReadPointer();
        int index = KeyMethods.BinarySearch(ptr + HeaderSize, oldKey, RecordCount, KeyValueSize); // BinarySearch(key);

        if (index < 0)
            throw new KeyNotFoundException("Missing key on update.");

        ptr = GetWritePointer() + HeaderSize + index * KeyValueSize;

        if (index > 0)
            if (!KeyMethods.IsGreaterThan(ptr - KeyValueSize, newKey))
                throw new Exception("Cannot update the key because the sorting gets messed up");

        if (index < RecordCount - 1)
            if (!KeyMethods.IsGreaterThan(newKey, ptr + KeyValueSize))
                throw new Exception("Cannot update the key because the sorting gets messed up");

        newKey.Write(ptr);
    }

    private void InternalUpdateValue(TKey key, TValue value)
    {
        // ToDo: Make this implementation generic
        byte* ptr = GetReadPointer();
        int index = KeyMethods.BinarySearch(ptr + HeaderSize, key, RecordCount, KeyValueSize); // BinarySearch(key);
        if (index < 0)
            throw new KeyNotFoundException();

        ptr = GetWritePointer() + HeaderSize + index * KeyValueSize + KeySize;
        value.Write(ptr);
    }

    #endregion
}