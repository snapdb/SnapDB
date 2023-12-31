﻿//******************************************************************************************************
//  EncodingLibrary.cs - Gbtc
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
//  02/22/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.Snap.Definitions;

namespace SnapDB.Snap.Encoding;

/// <summary>
/// Contains all of the fundamental encoding methods. Types implementing <see cref="SnapTypeBase{T}"/>
/// will automatically register when passed to one of the child methods.
/// </summary>
public class EncodingLibrary
{
    #region [ Members ]

    private readonly PairEncodingDictionary m_doubleEncoding;
    private readonly IndividualEncodingDictionary m_individualEncoding;

    #endregion

    #region [ Constructors ]

    internal EncodingLibrary()
    {
        m_individualEncoding = new IndividualEncodingDictionary();
        m_doubleEncoding = new PairEncodingDictionary();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Gets the single encoding method if it exists in the database.
    /// </summary>
    /// <typeparam name="T">The type parameter specifying the data type.</typeparam>
    /// <param name="encodingMethod">The encoding method identifier.</param>
    /// <returns>
    /// An instance of the <see cref="IndividualEncodingBase{T}"/> representing the encoding method.
    /// </returns>
    /// <exception cref="Exception">Thrown if the type is not registered.</exception>
    public IndividualEncodingBase<T> GetEncodingMethod<T>(Guid encodingMethod) where T : SnapTypeBase<T>, new()
    {
        if (encodingMethod == EncodingDefinition.FixedSizeIndividualGuid)
            return new IndividualEncodingFixedSize<T>();

        if (m_individualEncoding.TryGetEncodingMethod<T>(encodingMethod, out IndividualEncodingDefinitionBase encoding))
            return encoding.Create<T>();

        throw new Exception("Type is not registered");
    }

    /// <summary>
    /// Gets the Double encoding method
    /// </summary>
    /// <typeparam name="TKey">The type parameter specifying the key data type.</typeparam>
    /// <typeparam name="TValue">The type parameter specifying the value data type.</typeparam>
    /// <param name="encodingMethod">The encoding method identifier.</param>
    /// <returns>
    /// An instance of the <see cref="PairEncodingBase{TKey, TValue}"/> representing the encoding method.
    /// </returns>
    /// <exception cref="Exception">Thrown if the type is not registered.</exception>
    public PairEncodingBase<TKey, TValue> GetEncodingMethod<TKey, TValue>(EncodingDefinition encodingMethod) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (encodingMethod.IsFixedSizeEncoding)
            return new PairEncodingFixedSize<TKey, TValue>();

        if (m_doubleEncoding.TryGetEncodingMethod<TKey, TValue>(encodingMethod, out PairEncodingDefinitionBase encoding))
            return encoding.Create<TKey, TValue>();

        if (encodingMethod.IsKeyValueEncoded)
            throw new Exception("Type is not registered");

        return new PairEncodingGeneric<TKey, TValue>(encodingMethod);
    }

    /// <summary>
    /// Registers the provided type in the encoding library.
    /// </summary>
    /// <param name="encoding">the encoding to register</param>
    internal void Register(IndividualEncodingDefinitionBase encoding)
    {
        m_individualEncoding.Register(encoding);
    }

    /// <summary>
    /// Registers the provided type in the encoding library.
    /// </summary>
    /// <param name="encoding">the encoding to register</param>
    internal void Register(PairEncodingDefinitionBase encoding)
    {
        m_doubleEncoding.Register(encoding);
    }

    #endregion
}