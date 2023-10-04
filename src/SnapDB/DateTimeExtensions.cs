//******************************************************************************************************
//  DateTimeExtensions.cs - Gbtc
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
//  08/29/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/25/2023 - Lillian Gensolin
//       Convert code to .NET core.
//
//******************************************************************************************************

namespace SnapDB;

/// <summary>
/// Helper methods for type <see cref="DateTime"/>
/// </summary>
public static class DateTimeExtensions
{
    #region [ Static ]

    /// <summary>
    /// Rounds down a <see cref="DateTime"/> value to the nearest day.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to round down.</param>
    /// <returns>The rounded-down <see cref="DateTime"/> value.</returns>
    /// <remarks>
    /// This method rounds down the input <paramref name="value"/> to the nearest day by removing the time component.
    /// </remarks>
    public static DateTime RoundDownToNearestDay(this DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerDay, value.Kind);
    }

    /// <summary>
    /// Rounds down a <see cref="DateTime"/> value to the nearest hour.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to round down.</param>
    /// <returns>The rounded-down <see cref="DateTime"/> value.</returns>
    /// <remarks>
    /// This method rounds down the input <paramref name="value"/> to the nearest hour by removing the minutes, seconds, and milliseconds components.
    /// </remarks>
    public static DateTime RoundDownToNearestHour(this DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerHour, value.Kind);
    }

    /// <summary>
    /// Rounds down a <see cref="DateTime"/> value to the nearest minute.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to round down.</param>
    /// <returns>The rounded-down <see cref="DateTime"/> value.</returns>
    /// <remarks>
    /// This method rounds down the input <paramref name="value"/> to the nearest minute by removing the seconds and milliseconds components.
    /// </remarks>
    public static DateTime RoundDownToNearestMinute(this DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, value.Kind);
    }

    /// <summary>
    /// Rounds down a <see cref="DateTime"/> value to the nearest second.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to round down.</param>
    /// <returns>The rounded-down <see cref="DateTime"/> value.</returns>
    /// <remarks>
    /// This method rounds down the input <paramref name="value"/> to the nearest second by removing the milliseconds component.
    /// </remarks>
    public static DateTime RoundDownToNearestSecond(this DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
    }

    /// <summary>
    /// Rounds down a <see cref="DateTime"/> value to the nearest millisecond.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to round down.</param>
    /// <returns>The rounded-down <see cref="DateTime"/> value.</returns>
    /// <remarks>
    /// This method rounds down the input <paramref name="value"/> to the nearest millisecond by removing the microseconds component.
    /// </remarks>
    public static DateTime RoundDownToNearestMillisecond(this DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);
    }

    #endregion
}