//******************************************************************************************************
//  WeakDelegateBase.cs - Gbtc
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
//  01/26/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Reflection;

namespace SnapDB.Threading;

/// <summary>
/// Represents a base class for weak delegate wrappers with a specified delegate type.
/// </summary>
/// <typeparam name="T">The type constraint for the delegate.</typeparam>
public abstract class WeakDelegateBase<T> : WeakReference where T : class
{
    #region [ Members ]

    private readonly MethodInfo m_method;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakDelegateBase{T}"/> class with the specified delegate target.
    /// </summary>
    /// <param name="target">The delegate target.</param>
    protected WeakDelegateBase(Delegate target) : base(target is null ? null : target.Target)
    {
        if (target is not null)
            m_method = target.Method;
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Determines whether the current <see cref="WeakDelegateBase{T}"/> instance is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is WeakDelegateBase<T> typeObject)
            return Equals(typeObject);

        return false;
    }

    /// <summary>
    /// Tries to invoke the delegate associated with this weak reference object.
    /// </summary>
    /// <param name="parameters">An array of parameters to pass to the delegate.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if the delegate has been garbage collected.</returns>
    protected bool TryInvokeInternal(object[] parameters)
    {
        object? target = base.Target;
        if (target is null)
            return false;

        m_method.Invoke(target, parameters);
        return true;
    }

    /// <summary>
    /// Determines whether the current <see cref="WeakDelegateBase{T}"/> instance is equal to another <see cref="WeakDelegateBase{T}"/> instance.
    /// </summary>
    /// <param name="obj">The <see cref="WeakDelegateBase{T}"/> instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    protected virtual bool Equals(WeakDelegateBase<T> obj)
    {
        if (obj is null)
            return false;

        return m_method == obj.m_method && ReferenceEquals(Target, obj.Target);
    }

    #endregion
}