//******************************************************************************************************
//  CustomCommandNotSupportedException.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
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
//  06/23/2026 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

namespace SnapDB.Snap.Services.Net;

/// <summary>
/// Exception thrown when a client invokes a custom command that the server does not support, either because no
/// handler is registered for the command name or because the server is an older version that does not recognize
/// the custom-command protocol.
/// </summary>
/// <remarks>
/// Callers can catch this exception to degrade gracefully when connecting to servers that lack support for a
/// given custom command.
/// </remarks>
public class CustomCommandNotSupportedException : Exception
{
    /// <summary>
    /// Gets the name of the custom command that was not supported, when known.
    /// </summary>
    public string? CommandName { get; }

    /// <summary>
    /// Creates a new <see cref="CustomCommandNotSupportedException"/>.
    /// </summary>
    /// <param name="commandName">The name of the unsupported custom command, when known.</param>
    public CustomCommandNotSupportedException(string? commandName) : base($"Custom command '{commandName}' is not supported by the server.")
    {
        CommandName = commandName;
    }
}
