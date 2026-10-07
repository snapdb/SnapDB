//******************************************************************************************************
//  NearestTimestampSeekFilterDefinition.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance. All Rights Reserved.
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
//******************************************************************************************************

using System;
using SnapDB.IO;
using SnapDB.Snap.Definitions;

namespace SnapDB.Snap.Filters
{
    /// <summary>Wire definition for nearest timestamp sampling. Shared by GSF and SnapDB.</summary>
    public sealed class NearestTimestampSeekFilterDefinition : SeekFilterDefinitionBase
    {
        /// <summary>Stable identifier for the nearest-sample wire format.</summary>
        public static readonly Guid FilterGuid = new Guid("f906dfb8-692b-4a28-98ed-ff78ae74e4b5");

        /// <inheritdoc/>
        public override Guid FilterType => FilterGuid;

        /// <inheritdoc/>
        public override SeekFilterBase<TKey> Create<TKey>(BinaryStreamBase stream)
        {
            Type filterType = typeof(NearestTimestampSeekFilter<>).MakeGenericType(typeof(TKey));
            System.Reflection.MethodInfo method = filterType.GetMethod("Load", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Nearest filter loader is missing.");
            return (SeekFilterBase<TKey>)(method.Invoke(null, new object[] { stream })
                ?? throw new InvalidOperationException("Nearest filter loader returned no filter."));
        }
    }
}
