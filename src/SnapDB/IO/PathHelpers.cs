//******************************************************************************************************
//  PathHelpers.cs - Gbtc
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
//  10/03/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//       
//******************************************************************************************************

namespace SnapDB.IO;

/// <summary>
/// Helper methods for path strings.
/// </summary>
public static class PathHelpers
{
    #region [ Static ]

    /// <summary>
    /// Ensures that the provided extension is in the provided format:  .exe
    /// </summary>
    /// <param name="extension">the extension to format. Can be *.exe, or .exe, or exe</param>
    /// <returns>
    /// A vaild extension.
    /// </returns>
    /// <remarks>
    /// Throws a series of exceptions if the <see cref="extension"/> is invalid.
    /// </remarks>
    public static string FormatExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentNullException(nameof(extension), "Extension cannot be null or empty space");

        extension = extension.Trim();

        if (extension.StartsWith("*."))
            extension = extension.Substring(1);

        if (!extension.StartsWith("."))
            extension = "." + extension;

        if (extension.IndexOf('.', 1) >= 0)
            throw new ArgumentException("Invalid Extension. Contains too many periods.");

        if (extension.Length == 1)
            throw new ArgumentException("Invalid Extension. Must contain more than just a period.", nameof(extension));

        if (extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Extension has invalid characters.", nameof(extension));

        return extension;
    }

    /// <summary>
    /// Ensures the supplied file name is valid.
    /// </summary>
    /// <param name="fileName">any file name.</param>
    /// <remarks>
    /// Throws a series of exceptions if the <see cref="fileName"/> is invalid.
    /// </remarks>
    public static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "Extension cannot be null or empty space");

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("filename has invalid characters.", "value");
    }

    /// <summary>
    /// Ensures the supplied path name is valid.
    /// </summary>
    /// <param name="pathName">Any path.</param>
    /// <remarks>
    /// Throws a series of exceptions if the <see cref="pathName"/> is invalid.
    /// </remarks>
    public static void ValidatePathName(string pathName)
    {
        if (string.IsNullOrWhiteSpace(pathName))
            throw new ArgumentNullException(nameof(pathName), "Extension cannot be null or empty space");

        if (pathName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException("filename has invalid characters.", "value");
    }

    #endregion
}