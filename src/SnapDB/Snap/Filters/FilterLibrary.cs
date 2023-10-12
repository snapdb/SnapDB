//******************************************************************************************************
//  FilterLibrary.cs - Gbtc
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

using Gemstone.Diagnostics;
using SnapDB.IO;
using SnapDB.Snap.Definitions;

namespace SnapDB.Snap.Filters;

/// <summary>
/// Contains all of the filters for the <see cref="Snap"/>.
/// </summary>
public class FilterLibrary
{
    #region [ Members ]

    private readonly Dictionary<Guid, MatchFilterDefinitionBase> m_filters;
    private readonly Dictionary<Guid, SeekFilterDefinitionBase> m_seekFilters;

    private readonly object m_syncRoot;

    #endregion

    #region [ Constructors ]

    internal FilterLibrary()
    {
        m_syncRoot = new object();
        m_filters = new Dictionary<Guid, MatchFilterDefinitionBase>();
        m_seekFilters = new Dictionary<Guid, SeekFilterDefinitionBase>();
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Registers a match filter definition to the collection of filters.
    /// </summary>
    /// <param name="encoding">The match filter definition to be registered.</param>
    public void Register(MatchFilterDefinitionBase encoding)
    {
        lock (m_syncRoot)
            m_filters.Add(encoding.FilterType, encoding);
    }

    /// <summary>
    /// Registers a seek filter definition to the collection of seek filters.
    /// </summary>
    /// <param name="encoding">The seek filter definition to be registered.</param>
    public void Register(SeekFilterDefinitionBase encoding)
    {
        lock (m_syncRoot)
            m_seekFilters.Add(encoding.FilterType, encoding);
    }

    /// <summary>
    /// Retrieves a match filter based on the specified filter GUID and binary stream.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used in the match filter.</typeparam>
    /// <typeparam name="TValue">The type of values used in the match filter.</typeparam>
    /// <param name="filter">The GUID identifying the desired match filter.</param>
    /// <param name="stream">The binary stream to associate with the match filter.</param>
    /// <returns>The match filter instance if found; otherwise, an exception is thrown.</returns>
    /// <exception cref="Exception">Thrown when the match filter is not found.</exception>
    public MatchFilterBase<TKey, TValue> GetMatchFilter<TKey, TValue>(Guid filter, BinaryStreamBase stream) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        try
        {
            lock (m_syncRoot)
            {
                if (m_filters.TryGetValue(filter, out MatchFilterDefinitionBase? encoding))
                    return encoding.Create<TKey, TValue>(stream);
            }
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.Error, "Match Filter Exception", $"ID: {filter.ToString()} Key: {typeof(TKey)} Value: {typeof(TValue)}", null, ex);

            throw;
        }

        s_log.Publish(MessageLevel.Info, "Missing Match Filter", $"ID: {filter.ToString()} Key: {typeof(TKey)} Value: {typeof(TValue)}");

        throw new Exception("Filter not found");
    }

    /// <summary>
    /// Retrieves a seek filter based on the specified filter GUID and binary stream.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used in the seek filter.</typeparam>
    /// <param name="filter">The GUID identifying the desired seek filter.</param>
    /// <param name="stream">The binary stream to associate with the seek filter.</param>
    /// <returns>The seek filter instance if found; otherwise, an exception is thrown.</returns>
    /// <exception cref="Exception">Thrown when the seek filter is not found.</exception>
    public SeekFilterBase<TKey> GetSeekFilter<TKey>(Guid filter, BinaryStreamBase stream) where TKey : SnapTypeBase<TKey>, new()
    {
        try
        {
            lock (m_syncRoot)
            {
                if (m_seekFilters.TryGetValue(filter, out SeekFilterDefinitionBase? encoding))
                    return encoding.Create<TKey>(stream);
            }
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.Error, "Seek Filter Exception", $"ID: {filter.ToString()} Key: {typeof(TKey)}", null, ex);

            throw;
        }

        s_log.Publish(MessageLevel.Info, "Missing Seek Filter", $"ID: {filter.ToString()} Key: {typeof(TKey)}");

        throw new Exception("Filter not found");
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(FilterLibrary), MessageClass.Framework);

    #endregion
}