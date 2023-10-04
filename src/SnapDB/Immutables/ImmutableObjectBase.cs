//******************************************************************************************************
//  ImmutableObjectBase`1.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/24/2016 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Data;

namespace SnapDB.Immutables;

/// <summary>
/// Represents an object that can be configured as read only and thus made immutable.
/// The original contents of this class will not be editable once <see cref="IsReadOnly"/> is set to true.
/// In order to modify the contest of this object, a clone of the object must be created with <see cref="CloneEditable"/>.
/// </summary>
/// <typeparam name="T">Object type.</typeparam>
/// <remarks>
/// For a classes that implement this, all setters should call <see cref="TestForEditable"/> before
/// setting the value.
/// </remarks>
public abstract class ImmutableObjectBase<T> : IImmutableObject<T> where T : ImmutableObjectBase<T>
{
    #region [ Members ]

    private bool m_isReadOnly;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets or sets if this class is immutable and thus read-only. Once
    /// setting to read-only, the class becomes immutable.
    /// </summary>
    public bool IsReadOnly
    {
        get => m_isReadOnly;
        set
        {
            if (!(value ^ m_isReadOnly))
                return; // If values are different.

            if (m_isReadOnly)
                throw new ReadOnlyException("Object has been set as read only and cannot be reversed");

            m_isReadOnly = true;
            SetMembersAsReadOnly();
        }
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Test if the class has been marked as read-only. Throws an exception if editing cannot occur.
    /// </summary>
    protected void TestForEditable()
    {
        if (m_isReadOnly)
            ThrowReadOnly();
    }

    /// <summary>
    /// Requests that member fields be set to read-only.
    /// </summary>
    protected abstract void SetMembersAsReadOnly();

    /// <summary>
    /// Request that member fields be cloned and marked as editable.
    /// </summary>
    protected abstract void CloneMembersAsEditable();

    /// <summary>
    /// Creates a clone of this class that is editable.
    /// A clone is always created, even if this class is already editable.
    /// </summary>
    /// <returns>The newly created instance of type 'T'.</returns>
    public virtual T CloneEditable()
    {
        T initializer = (T)MemberwiseClone();

        initializer.m_isReadOnly = false;
        initializer.CloneMembersAsEditable();

        return initializer;
    }

    /// <summary>
    /// Makes a "read-only" clone of this object. Returns the same object if it is already marked as read-only.
    /// </summary>
    /// <returns>The non-editable clone.</returns>
    object IImmutableObject.CloneReadonly()
    {
        return CloneReadonly();
    }

    /// <summary>
    /// Makes a clone of this object and allows it to be edited.
    /// </summary>
    /// <returns>The editable clone.</returns>
    object IImmutableObject.CloneEditable()
    {
        return CloneEditable();
    }

    /// <summary>
    /// Creates a read-only clone of the object.
    /// </summary>
    /// <returns>
    /// A new instance with the same state as the original, marked as read-only.
    /// </returns>
    /// <remarks>
    /// This method is used to create a copy of the object with read-only access.
    /// If the object is already read-only, it returns itself.
    /// </remarks>
    public virtual T CloneReadonly()
    {
        if (IsReadOnly)
            return (T)this;

        T copy = CloneEditable();
        copy.IsReadOnly = true;

        return copy;
    }

    /// <summary>
    /// Creates a clone of the object, either as a read-only instance or an editable one.
    /// </summary>
    /// <returns>
    /// If the object is read-only, it returns itself. If the object is editable, it returns
    /// a new instance with the same state as the original.
    /// </returns>
    /// <remarks>
    /// This method is used to create a copy of the object, allowing either read-only or
    /// editable access depending on the object's current state.
    /// </remarks>
    public object Clone()
    {
        return IsReadOnly ? this : CloneEditable();
    }

    #endregion

    #region [ Static ]

    private static void ThrowReadOnly()
    {
        throw new ReadOnlyException("Object has been set as read only");
    }

    #endregion
}