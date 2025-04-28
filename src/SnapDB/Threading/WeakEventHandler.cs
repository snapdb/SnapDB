//******************************************************************************************************
//  WeakEventHandler.cs - Gbtc
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
//  01/16/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Threading;

/// <summary>
/// Represents a weak event handler for events with <see cref="EventArgs"/> of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of <see cref="EventArgs"/> used by the event.</typeparam>
public class WeakEventHandler<T> : WeakDelegateBase<EventHandler<T>> where T : EventArgs
{
    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakEventHandler{T}"/> class with the specified event handler target.
    /// </summary>
    /// <param name="target">The event handler target.</param>
    public WeakEventHandler(EventHandler<T> target) : base(target)
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Tries to invoke the event handler associated with this weak reference object.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if the event handler has been garbage collected.</returns>
    public bool TryInvoke(object sender, T e)
    {
        return TryInvokeInternal([sender, e]);
    }

    #endregion
}