//******************************************************************************************************
//  SeekFilterUniverse.cs - Gbtc
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
//  11/09/2013 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using SnapDB.IO;

namespace SnapDB.Snap.Filters;

/// <summary>
/// Represents no filter
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class SeekFilterUniverse<TKey> : SeekFilterBase<TKey> where TKey : SnapTypeBase<TKey>, new()
{
    #region [ Members ]

    private bool m_isEndReached;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the SeekFilterUniverse class.
    /// </summary>
    public SeekFilterUniverse()
    {
        StartOfFrame = new TKey();
        EndOfFrame = new TKey();
        StartOfRange = StartOfFrame;
        EndOfRange = EndOfFrame;
        Reset();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Initializes a new instance of the SeekFilterUniverse class.
    /// </summary>
    public override Guid FilterType => Guid.Empty;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// This method is not supported in this filter.
    /// </summary>
    /// <param name="stream">The binary stream to save to.</param>
    /// <exception cref="NotSupportedException">Thrown because saving is not supported for this filter.</exception>
    public override void Save(BinaryStreamBase stream)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Resets the filter to its initial state.
    /// </summary>
    public override void Reset()
    {
        m_isEndReached = false;
        StartOfRange.SetMin();
        EndOfRange.SetMax();
    }

    /// <summary>
    /// Moves to the next window in the filter's data range.
    /// </summary>
    /// <returns><c>true</c> if the next window exists; otherwise, <c>false</c>.</returns>
    public override bool NextWindow()
    {
        if (m_isEndReached)
            return false;

        StartOfRange.SetMin();
        EndOfRange.SetMax();
        m_isEndReached = true;

        return true;
    }

    #endregion
}