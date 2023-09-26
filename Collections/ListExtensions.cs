//******************************************************************************************************
//  ListExtensions.cs - Gbtc
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
//  06/08/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/14/2023 - Lillian Gensolin
//       Converted code to .NET core. 
//
//******************************************************************************************************

namespace SnapDB.Collections;

/// <summary>
/// Extensions for <see cref="IList{T}"/>
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Parses through the provided list and assigns <see cref="item"/> to the first null field. 
    /// Otherwise, it will be added to the end of the list.
    /// </summary>
    /// <param name="list">The list to iterate through.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>The index of the added item.</returns>
    public static int ReplaceFirstNullOrAdd<T>(this IList<T?> list, T item)
        where T : class
    {
        if (list is null)
            throw new ArgumentNullException(nameof(list));

        if (list is List<T?> instance)
            return instance.ReplaceFirstNullOrAdd(item);

        for (int x = 0; x < list.Count; x++)
        {
            if (list[x] is not null)
                continue;
            
            list[x] = item;

            return x;
        }

        list.Add(item);

        return list.Count - 1;
    }

    /// <summary>
    /// Parses through the provided list and assigns <see cref="item"/> to the first null field. 
    /// Otherwise, it will be added to the end of the list.
    /// </summary>
    /// <param name="list">The list to iterate through.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>Th index of the added item.</returns>
    public static int ReplaceFirstNullOrAdd<T>(this List<T?> list, T item)
       where T : class
    {
        if (list is null)
            throw new ArgumentNullException(nameof(list));

        for (int x = 0; x < list.Count; x++)
        {
            if (list[x] is not null)
                continue;
            
            list[x] = item;

            return x;
        }

        list.Add(item);
           
        return list.Count - 1;
    }
}
