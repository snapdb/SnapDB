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

#pragma warning disable CA2245

using Gemstone.Diagnostics;
using SnapDB.IO;
using SnapDB.Security.Authentication;
using static SnapDB.Snap.Filters.AccessControlSeekPosition;

namespace SnapDB.Snap.Filters;

// Wrapper around any SeekFilterBase to apply access control to the filter
internal sealed class AccessControlledSeekFilter<TKey> : SeekFilterBase<TKey> where TKey : SnapTypeBase<TKey>, new()
{
    private readonly SeekFilterBase<TKey> m_seekFilter;
    private readonly Func<string, TKey, AccessControlSeekPosition, bool> m_userCanSeek;
    private readonly IntegratedSecurityUserCredential m_user;

    public AccessControlledSeekFilter(SeekFilterBase<TKey> seekFilter, IntegratedSecurityUserCredential user, Func<string, TKey, AccessControlSeekPosition, bool> userCanSeek)
    {
        m_seekFilter = seekFilter;
        m_user = user;
        m_userCanSeek = userCanSeek;

        // Reapply possibly pre-existing TKey values to ensure access control is applied
        EndOfFrame = EndOfFrame;
        EndOfRange = EndOfRange;
        StartOfFrame = StartOfFrame;
        StartOfRange = StartOfRange;
    }

    public override TKey EndOfFrame
    {
        get => m_seekFilter.EndOfFrame;
        protected internal set
        {
            try
            {
                if (m_userCanSeek(m_user.UserId, value, End))
                    m_seekFilter.EndOfFrame = value;
                else
                    m_seekFilter.EndOfFrame.SetMin();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex, "Error in provided user read access control function for seek filters");
                m_seekFilter.EndOfFrame.SetMin();
            }
        }
    }

    public override TKey EndOfRange

    {
        get => m_seekFilter.EndOfRange;
        protected internal set
        {
            try
            {
                if (m_userCanSeek(m_user.UserId, value, End))
                    m_seekFilter.EndOfRange = value;
                else
                    m_seekFilter.EndOfRange.SetMin();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex, "Error in provided user read access control function for seek filters");
                m_seekFilter.EndOfRange.SetMin();
            }
        }
    }

    public override TKey StartOfFrame
    {
        get => m_seekFilter.StartOfFrame;
        protected internal set
        {
            try
            {
                if (m_userCanSeek(m_user.UserId, value, Start))
                    m_seekFilter.StartOfFrame = value;
                else
                    m_seekFilter.StartOfFrame.SetMax();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex, "Error in provided user read access control function for seek filters");
                m_seekFilter.StartOfFrame.SetMax();
            }
        }
    }

    public override TKey StartOfRange
    {
        get => m_seekFilter.StartOfRange;
        protected internal set
        {
            try
            {
                if (m_userCanSeek(m_user.UserId, value, Start))
                    m_seekFilter.StartOfRange = value;
                else
                    m_seekFilter.StartOfRange.SetMax();
            }
            catch (Exception ex)
            {
                Logger.SwallowException(ex, "Error in provided user read access control function for seek filters");
                m_seekFilter.StartOfRange.SetMax();
            }
        }
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
