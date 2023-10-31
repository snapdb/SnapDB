//******************************************************************************************************
//  AccessControlledMatchFilter.cs - Gbtc
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
using SnapDB.Security.Authentication;

namespace SnapDB.Snap.Filters;

// Wrapper around any MatchFilterBase to apply access control to the filter
internal class AccessControlledMatchFilter<TKey, TValue> : MatchFilterBase<TKey, TValue>
{
    private readonly MatchFilterBase<TKey, TValue> m_matchFilter;
    private readonly Func<string, TKey, TValue, bool> m_userCanMatch;
    private readonly IntegratedSecurityUserCredential m_user;

    public AccessControlledMatchFilter(MatchFilterBase<TKey, TValue> matchFilter, IntegratedSecurityUserCredential user, Func<string, TKey, TValue, bool> userCanMatch)
    {
        m_matchFilter = matchFilter;
        m_user = user;
        m_userCanMatch = userCanMatch;
    }

    public override Guid FilterType => m_matchFilter.FilterType;

    public override void Save(BinaryStreamBase stream)
    {
        m_matchFilter.Save(stream);
    }

    public override bool Contains(TKey key, TValue value)
    {
        return m_userCanMatch(m_user.UserId, key, value) && m_matchFilter.Contains(key, value);
    }
}
