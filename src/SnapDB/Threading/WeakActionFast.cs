//******************************************************************************************************
//  WeakActionFast.cs - Gbtc
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
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;

namespace SnapDB.Threading;

/// <summary>
/// Provides a high speed weak referenced action delegate.
/// This one does not use reflection, so calls will be faster.
/// 
/// HOWEVER: a strong reference MUST be maintained for 
/// the <see cref="Action"/> delegate passed to this class. 
/// This reference is outputted in the constructor.
/// 
/// Careful consideration must be made when deciding 
/// where to store this strong reference, as this strong reference
/// will need to also lose reference. A good place would be 
/// as a member variable of the object of the target method.
/// </summary>
public class WeakActionFast
    : WeakReference
{
    /// <summary>
    /// Creates a high speed weak action 
    /// </summary>
    /// <param name="target">The action to create a weak reference to.</param>
    /// <param name="localStrongReference">An out parameter that receives a local strong reference to the action.</param>
    public WeakActionFast(Action target, out object localStrongReference)
        : base(target)
    {
        localStrongReference = target;
    }

    /// <summary>
    /// Attempts to invoke the delegate to a weak reference object.
    /// </summary>
    /// <returns><c>true</c> if successful, <c>false</c> if not.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryInvoke()
    {
        Action target = (Action)Target;
        if (target is null)
            return false;
        target();
        return true;
    }

    public void Clear()
    {
        Target = null;
    }
}

/// <summary>
/// Provides a high speed weak referenced action delegate.
/// This one does not use reflection, so calls will be faster.
/// 
/// HOWEVER: a strong reference MUST be maintained for 
/// the <see cref="Action"/> delegate passed to this class. 
/// This reference is outputted in the constructor.
/// 
/// Careful consideration must be made when deciding 
/// where to store this strong reference, as this strong reference
/// will need to also lose reference. A good place would be 
/// as a member variable of the object of the target method.
/// </summary>
public class WeakActionFast<T>
    : WeakReference
{
    /// <summary>
    /// Creates a high speed weak action. 
    /// </summary>
    /// <param name="target">the callback</param>
    /// <param name="localStrongReference">a strong reference that must be 
    /// maintained in the class that is the target of the action</param>
    public WeakActionFast(Action<T> target, out object localStrongReference)
        : base(target)
    {
        localStrongReference = target;
    }

    /// <summary>
    /// Attempts to invoke the delegate associated with this weak reference object.
    /// </summary>
    /// <param name="args">The argument to be passed to the delegate.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if the delegate has been garbage collected.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryInvoke(T args)
    {
        Action<T> target = (Action<T>)Target;
        if (target is null)
            return false;
            
        target(args);
        return true;
    }


Here's structured comments for the WeakActionFast<T> class:

csharp
Copy code
/// <summary>
/// Represents a high-speed weak action with a specified delegate type.
/// </summary>
/// <typeparam name="T">The type of the delegate's parameter.</typeparam>
public class WeakActionFast<T> : WeakReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeakActionFast{T}"/> class with the specified action target.
    /// </summary>
    /// <param name="target">The callback action.</param>
    /// <param name="localStrongReference">A strong reference that must be maintained in the class that is the target of the action.</param>
    public WeakActionFast(Action<T> target, out object localStrongReference)
        : base(target)
    {
        localStrongReference = target;
    }

    /// <summary>
    /// Attempts to invoke the delegate associated with this weak reference object.
    /// </summary>
    /// <param name="args">The argument to be passed to the delegate.</param>
    /// <returns>True if successful, false if the delegate has been garbage collected.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryInvoke(T args)
    {
        Action<T> target = (Action<T>)Target;
        if (target is null)
            return false;
        target(args);
        return true;
    }

    /// <summary>
    /// Clears the weak reference to the delegate.
    /// </summary>
    public void Clear()
    {
        Target = null;
    }

}
