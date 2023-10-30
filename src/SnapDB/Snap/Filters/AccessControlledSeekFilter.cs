//******************************************************************************************************
//  AccessControlledSeekFilter.cs - Gbtc
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
//  10/30/2023 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using SnapDB.IO;

namespace SnapDB.Snap.Filters;

// Wrapper around any SeekFilterBase to apply access control to the filter
internal class AccessControlledSeekFilter<TKey> : SeekFilterBase<TKey>
{
    private readonly SeekFilterBase<TKey> m_seekFilter;
    private readonly Func<TKey, bool /* isStart */, TKey> m_aclFilter;

    public AccessControlledSeekFilter(SeekFilterBase<TKey> seekSeekFilter, Func<TKey, bool, TKey> aclFilter)
    {
        m_seekFilter = seekSeekFilter;
        m_aclFilter = aclFilter;
    }

    /// <summary>
    /// the end of the frame to search [Inclusive]
    /// </summary>
    public new TKey EndOfFrame
    {
        get => m_seekFilter.EndOfFrame; 
        protected internal set => m_seekFilter.EndOfFrame = m_aclFilter(value, false);
    }

    /// <summary>
    /// the end of the entire range to search [Inclusive]
    /// </summary>
    public new TKey EndOfRange

    {
        get => m_seekFilter.EndOfRange; 
        protected internal set => m_seekFilter.EndOfRange = m_aclFilter(value, false);
    }

    /// <summary>
    /// the start of the frame to search [Inclusive]
    /// </summary>
    public new TKey StartOfFrame
    {
        get => m_seekFilter.StartOfFrame; 
        protected internal set => m_seekFilter.StartOfFrame = m_aclFilter(value, true);
    }

    /// <summary>
    /// the start of the entire range to search [Inclusive]
    /// </summary>
    public new TKey StartOfRange
    {
        get => m_seekFilter.StartOfRange; 
        protected internal set => m_seekFilter.StartOfRange = m_aclFilter(value, true);
    }

    public override Guid FilterType => m_seekFilter.FilterType;

    public override void Save(BinaryStreamBase stream)
    {
        m_seekFilter.Save(stream);
    }

    public override void Reset()
    {
        m_seekFilter.Reset();
    }

    public override bool NextWindow()
    {
        return m_seekFilter.NextWindow();
    }
}
