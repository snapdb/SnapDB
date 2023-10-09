//******************************************************************************************************
//  InsertStreamHelper`2.cs - Gbtc
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

/// <summary>
/// Helper class for inserting key-value pairs into a tree stream.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the tree stream.</typeparam>
/// <typeparam name="TValue">The type of the values in the tree stream.</typeparam>
public class InsertStreamHelper<TKey, TValue> where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
{
    #region [ Members ]

    /// <summary>
    /// Gets or sets a value indicating whether the current operation involves Key-Value Pair 1 (KVP1).
    /// </summary>
    public bool IsKvp1;

    /// <summary>
    /// Gets or sets a value indicating whether the stream is still sequential.
    /// </summary>
    public bool IsStillSequential;

    /// <summary>
    /// Gets or sets a value indicating whether the current state is valid.
    /// </summary>
    public bool IsValid;

    /// <summary>
    /// Gets or sets the first key.
    /// </summary>
    public TKey Key1;

    /// <summary>
    /// Gets or sets the second key.
    /// </summary>
    public TKey Key2;

    /// <summary>
    /// Gets or sets the tree stream for insertion.
    /// </summary>
    public TreeStream<TKey, TValue> Stream;

    /// <summary>
    /// Gets or sets the first value.
    /// </summary>
    public TValue Value1;

    /// <summary>
    /// Gets or sets the second value.
    /// </summary>
    public TValue Value2;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertStreamHelper{TKey, TValue}"/> class.
    /// </summary>
    public InsertStreamHelper(TreeStream<TKey, TValue> stream)
    {
        Stream = stream;
        Key1 = new TKey();
        Key2 = new TKey();
        Value1 = new TValue();
        Value2 = new TValue();
        IsKvp1 = false;
        IsStillSequential = true;

        if (IsKvp1)
        {
            IsValid = Stream.Read(Key2, Value2);
            IsKvp1 = false;
        }
        else
        {
            IsValid = Stream.Read(Key1, Value1);
            IsKvp1 = true;
        }
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the current key.
    /// </summary>
    public TKey Key => IsKvp1 ? Key1 : Key2;

    /// <summary>
    /// Gets the previous key.
    /// </summary>
    public TKey PrevKey => IsKvp1 ? Key2 : Key1;

    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public TValue PrevValue => IsKvp1 ? Value2 : Value1;

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public TValue Value => IsKvp1 ? Value1 : Value2;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Advances to the next key-value pair.
    /// </summary>
    public void Next()
    {
        if (IsKvp1)
        {
            IsValid = Stream.Read(Key2, Value2);
            IsStillSequential = Key1.IsLessThan(Key2);
            IsKvp1 = false;
        }
        else
        {
            IsValid = Stream.Read(Key1, Value1);
            IsStillSequential = Key2.IsLessThan(Key1);
            IsKvp1 = true;
        }
    }

    /// <summary>
    /// Advances to the next key-value pair without checking for sequential order.
    /// </summary>
    public void NextDoNotCheckSequential()
    {
        if (IsKvp1)
        {
            IsValid = Stream.Read(Key2, Value2);
            IsStillSequential = false;
            IsKvp1 = false;
        }
        else
        {
            IsValid = Stream.Read(Key1, Value1);
            IsStillSequential = false;
            IsKvp1 = true;
        }
    }

    #endregion
}