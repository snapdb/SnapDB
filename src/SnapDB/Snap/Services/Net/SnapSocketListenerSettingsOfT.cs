//******************************************************************************************************
//  SnapSocketListenerSettingsOfT.cs - Gbtc
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
//  11/01/2023 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using SnapDB.Snap.Filters;

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// Contains the typed basic config for a socket interface.
/// </summary>
/// <typeparam name="TKey">The type of keys used in the socket interface.</typeparam>
/// <typeparam name="TValue">The type of values used in the socket interface.</typeparam>
public class SnapSocketListenerSettings<TKey, TValue> : SnapSocketListenerSettings
{
    #region [ Members ]

    private Func<string, TKey, AccessControlSeekPosition, bool>? m_userCanSeek;
    private Func<string, TKey, TValue, bool>? m_userCanMatch;
    private Func<string, TKey, TValue, bool>? m_userCanWrite;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets or sets any defined user read access control function for seek filters.
    /// </summary>
    /// <remarks>
    /// Function parameters are: <br/>
    /// <c>string UserId</c> - The user security ID (SID) of the user attempting to seek.<br/>
    /// <c>TKey instance</c> - The key of the record being sought.<br/>
    /// <c>AccessControlSeekPosition</c> - The position of the seek. i.e., <c>Start</c> or <c>End</c>.<br/>
    /// <c>bool</c> - Return <c>true</c> if user is allowed to seek; otherwise, <c>false</c>.
    /// </remarks>
    public new Func<string /*UserId*/, TKey, AccessControlSeekPosition, bool>? UserCanSeek
    {
        get => m_userCanSeek;
        set
        {
            m_userCanSeek = value;

            if (m_userCanSeek is null)
                base.UserCanSeek = null;
            else
                base.UserCanSeek = (userId, key, position) => m_userCanSeek(userId, (TKey)key, position);
        }
    }

    /// <summary>
    /// Gets or sets any defined user read access control function for match filters.
    /// </summary>
    /// <remarks>
    /// Function parameters are: <br/>
    /// <c>string UserId</c> - The user security ID (SID) of the user attempting to match.<br/>
    /// <c>TKey instance</c> - The key of the record being matched.<br/>
    /// <c>TValue instance</c> - The value of the record being matched.<br/>
    /// <c>bool</c> - Return <c>true</c> if user is allowed to match; otherwise, <c>false</c>.
    /// </remarks>
    public new Func<string /*UserId*/, TKey, TValue, bool>? UserCanMatch
    {
        get => m_userCanMatch;
        set
        {
            m_userCanMatch = value;

            if (m_userCanMatch is null)
                base.UserCanMatch = null;
            else
                base.UserCanMatch = (userId, key, value) => m_userCanMatch(userId, (TKey)key, (TValue)value);
        }
    }

    /// <summary>
    /// Gets or sets any defined user write access control function.
    /// </summary>
    /// <remarks>
    /// Function parameters are: <br/>
    /// <c>string UserId</c> - The user security ID (SID) of the user attempting to write.<br/>
    /// <c>TKey instance</c> - The key of the record being written.<br/>
    /// <c>TValue instance</c> - The value of the record being written.<br/>
    /// <c>bool</c> - Return <c>true</c> if user is allowed to write; otherwise, <c>false</c>.
    /// </remarks>
    public new Func<string /*UserId*/, TKey, TValue, bool>? UserCanWrite
    {
        get => m_userCanWrite;
        set
        {
            m_userCanWrite = value;

            if (m_userCanWrite is null)
                base.UserCanWrite = null;
            else
                base.UserCanWrite = (userId, key, value) => m_userCanWrite(userId, (TKey)key, (TValue)value);
        }
    }
    
    #endregion
}
