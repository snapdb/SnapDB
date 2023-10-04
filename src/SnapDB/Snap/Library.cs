//******************************************************************************************************
//  Library.cs - Gbtc
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
//  05/16/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//
//  09/22/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Reflection;
using Gemstone.Diagnostics;
using SnapDB.Snap.Definitions;
using SnapDB.Snap.Encoding;
using SnapDB.Snap.Filters;
using SnapDB.Snap.Streaming;
using SnapDB.Snap.Tree;

namespace SnapDB.Snap;

/// <summary>
/// A library of <see cref="SnapTypeBase"/> types. This
/// library will dynamically register types via reflection if possible.
/// </summary>
public static class Library
{
    #region [ Constructors ]

    static Library()
    {
        try
        {
            s_filterAssemblyNames = new HashSet<string>();
            s_loadedAssemblies = new HashSet<Assembly>();
            Encodings = new EncodingLibrary();
            Filters = new FilterLibrary();
            s_syncRoot = new object();
            s_typeLookup = new Dictionary<Guid, Type>();
            s_registeredType = new Dictionary<Type, Guid>();
            s_keyValueMethodsList = new Dictionary<Tuple<Type, Type>, object>();

            s_filterAssemblyNames.Add(typeof(IndividualEncodingDefinitionBase).Assembly.GetName().Name);
            s_filterAssemblyNames.Add(typeof(PairEncodingDefinitionBase).Assembly.GetName().Name);
            s_filterAssemblyNames.Add(typeof(MatchFilterDefinitionBase).Assembly.GetName().Name);
            s_filterAssemblyNames.Add(typeof(SeekFilterDefinitionBase).Assembly.GetName().Name);
            s_filterAssemblyNames.Add(typeof(SnapTypeBase).Assembly.GetName().Name);
            s_filterAssemblyNames.Add(typeof(KeyValueMethods).Assembly.GetName().Name);

            ReloadNewAssemblies();
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.Critical, "Static Constructor Error", null, null, ex);
        }
    }

    #endregion

    #region [ Static ]

    private static readonly LogPublisher s_log = Logger.CreatePublisher(typeof(Library), MessageClass.Framework);

    /// <summary>
    /// Gets all of the encoding data.
    /// </summary>
    public static readonly EncodingLibrary Encodings;

    /// <summary>
    /// Gets all of the filters.
    /// </summary>
    public static readonly FilterLibrary Filters;

    private static readonly object s_syncRoot;
    private static readonly Dictionary<Guid, Type> s_typeLookup;
    private static readonly Dictionary<Type, Guid> s_registeredType;

    /// <summary>
    /// The assembly must reference one of these assembly names in order to be scanned for matching types.
    /// </summary>
    private static readonly HashSet<string> s_filterAssemblyNames;

    private static readonly HashSet<Assembly> s_loadedAssemblies;

    private static readonly Dictionary<Tuple<Type, Type>, object> s_keyValueMethodsList;

    private static void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        lock (s_syncRoot)
        {
            s_log.Publish(MessageLevel.Debug, "Reloading Assembly", args.LoadedAssembly.FullName);
            ReloadNewAssemblies();
        }
    }

    /// <summary>
    /// Will attempt to reload any type that
    /// inherits from <see cref="SnapTypeBase"/> in
    /// any new assemblies.
    /// </summary>
    private static void ReloadNewAssemblies()
    {
        Type typeCreateSingleValueEncodingBase = typeof(IndividualEncodingDefinitionBase);
        Type typeCreateDoubleValueEncodingBase = typeof(PairEncodingDefinitionBase);
        Type typeCreateFilterBase = typeof(MatchFilterDefinitionBase);
        Type typeCreateSeekFilterBase = typeof(SeekFilterDefinitionBase);
        Type typeSnapTypeBase = typeof(SnapTypeBase);
        Type typeKeyValueMethods = typeof(KeyValueMethods);

        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
                if (!s_loadedAssemblies.Contains(assembly))
                {
                    s_loadedAssemblies.Add(assembly);

                    if (s_filterAssemblyNames.Contains(assembly.GetName().Name) || assembly.GetReferencedAssemblies().Any(x => s_filterAssemblyNames.Contains(x.Name)))
                    {
                        s_log.Publish(MessageLevel.Debug, "Loading Assembly", assembly.GetName().Name);

                        Module[] modules = assembly.GetModules(false);
                        foreach (Module module in modules)
                            try
                            {
                                Type[] types;
                                try
                                {
                                    types = module.GetTypes();
                                }
                                catch (ReflectionTypeLoadException ex)
                                {
                                    s_log.Publish(MessageLevel.Debug, "Reflection Load Error Occurred", assembly.GetName().Name, ex + Environment.NewLine + string.Join(Environment.NewLine, ex.LoaderExceptions.Select(x => x.ToString())));
                                    types = ex.Types;
                                }

                                foreach (Type assemblyType in types)
                                    try
                                    {
                                        if ((object)assemblyType is not null && !assemblyType.IsAbstract && !assemblyType.ContainsGenericParameters)
                                        {
                                            if (typeCreateSingleValueEncodingBase.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Individual Encoding Method", assemblyType.AssemblyQualifiedName);
                                                Encodings.Register((IndividualEncodingDefinitionBase)Activator.CreateInstance(assemblyType));
                                            }
                                            else if (typeCreateDoubleValueEncodingBase.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Pair Encoding Method", assemblyType.AssemblyQualifiedName);
                                                Encodings.Register((PairEncodingDefinitionBase)Activator.CreateInstance(assemblyType));
                                            }
                                            else if (typeCreateFilterBase.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Match Filter", assemblyType.AssemblyQualifiedName);
                                                Filters.Register((MatchFilterDefinitionBase)Activator.CreateInstance(assemblyType));
                                            }
                                            else if (typeCreateSeekFilterBase.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Seek Filter", assemblyType.AssemblyQualifiedName);
                                                Filters.Register((SeekFilterDefinitionBase)Activator.CreateInstance(assemblyType));
                                            }
                                            else if (typeSnapTypeBase.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Snap Type", assemblyType.AssemblyQualifiedName);
                                                Register((SnapTypeBase)Activator.CreateInstance(assemblyType));
                                            }
                                            else if (typeKeyValueMethods.IsAssignableFrom(assemblyType))
                                            {
                                                s_log.Publish(MessageLevel.Debug, "Loading Key Value Methods", assemblyType.AssemblyQualifiedName);
                                                KeyValueMethods obj = (KeyValueMethods)Activator.CreateInstance(assemblyType);
                                                Tuple<Type, Type> ttypes = Tuple.Create(obj.KeyType, obj.ValueType);
                                                if (!s_keyValueMethodsList.ContainsKey(ttypes))
                                                    s_keyValueMethodsList.Add(ttypes, obj);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        s_log.Publish(MessageLevel.Critical, "Static Constructor Error", null, null, ex);
                                    }
                            }
                            catch (Exception ex)
                            {
                                s_log.Publish(MessageLevel.Critical, "Static Constructor Error", null, null, ex);
                            }
                    }
                }
        }
        catch (Exception ex)
        {
            s_log.Publish(MessageLevel.Critical, "Static Constructor Error", null, null, ex);
        }
    }

    /// <summary>
    /// Retrieves the Type associated with a given Guid identifier in the SortedTree type lookup.
    /// </summary>
    /// <param name="id">The unique identifier (Guid) associated with the SortedTree Type.</param>
    /// <returns>The Type corresponding to the provided identifier.</returns>
    public static Type GetSortedTreeType(Guid id)
    {
        lock (s_syncRoot)
        {
            return s_typeLookup[id];
        }
    }

    /// <summary>
    /// Retrieves or creates the KeyValueMethods instance for a specific TKey and TValue type combination.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the KeyValueMethods instance.</typeparam>
    /// <typeparam name="TValue">The type of values in the KeyValueMethods instance.</typeparam>
    /// <returns>A KeyValueMethods instance for the specified TKey and TValue types.</returns>
    /// <remarks>
    /// If an existing KeyValueMethods instance is found for the specified types, it is returned.
    /// Otherwise, a new KeyValueMethods instance is created and returned.
    /// </remarks>
    public static KeyValueMethods<TKey, TValue> GetKeyValueMethods<TKey, TValue>() where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        Tuple<Type, Type> t = Tuple.Create(typeof(TKey), typeof(TValue));
        lock (s_syncRoot)
        {
            if (s_keyValueMethodsList.TryGetValue(t, out object obj))
                return (KeyValueMethods<TKey, TValue>)obj;
        }

        return new KeyValueMethods<TKey, TValue>();
    }

    /// <summary>
    /// Creates a new instance of StreamEncodingBase for the specified TKey and TValue types and encoding method.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the StreamEncodingBase instance.</typeparam>
    /// <typeparam name="TValue">The type of values in the StreamEncodingBase instance.</typeparam>
    /// <param name="encodingMethod">The encoding method to use for data serialization.</param>
    /// <returns>A new StreamEncodingBase instance configured with the specified encoding method.</returns>
    internal static StreamEncodingBase<TKey, TValue> CreateStreamEncoding<TKey, TValue>(EncodingDefinition encodingMethod) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        return new StreamEncodingGeneric<TKey, TValue>(encodingMethod);
    }

    /// <summary>
    /// Creates a new instance of SortedTreeNodeBase for the specified TKey and TValue types, encoding method, and level.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the SortedTreeNodeBase instance.</typeparam>
    /// <typeparam name="TValue">The type of values in the SortedTreeNodeBase instance.</typeparam>
    /// <param name="encodingMethod">The encoding method to use for data serialization.</param>
    /// <param name="level">The level of the tree node in the tree hierarchy.</param>
    /// <returns>A new SortedTreeNodeBase instance configured with the specified encoding method and level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the encodingMethod is null.</exception>
    internal static SortedTreeNodeBase<TKey, TValue> CreateTreeNode<TKey, TValue>(EncodingDefinition encodingMethod, byte level) where TKey : SnapTypeBase<TKey>, new() where TValue : SnapTypeBase<TValue>, new()
    {
        if (encodingMethod is null)
            throw new ArgumentNullException(nameof(encodingMethod));

        if (encodingMethod.IsFixedSizeEncoding)
            return new FixedSizeNode<TKey, TValue>(level);

        return new GenericEncodedNode<TKey, TValue>(Encodings.GetEncodingMethod<TKey, TValue>(encodingMethod), level);
    }

    /// <summary>
    /// Registers a SnapTypeBase derived type by associating it with a unique GUID.
    /// </summary>
    /// <param name="snapType">The SnapTypeBase derived type to be registered.</param>
    /// <exception cref="Exception">
    /// Thrown when the provided SnapTypeBase type is already associated with a different GUID or when another type with the same GUID is already registered.
    /// </exception>
    private static void Register(SnapTypeBase snapType)
    {
        Type type = snapType.GetType();
        Guid id = snapType.GenericTypeGuid;

        lock (s_syncRoot)
        {
            if (s_registeredType.TryGetValue(type, out Guid existingId))
            {
                if (existingId != id)
                    throw new Exception("Existing type does not match Guid: " + type.FullName + " ID: " + id);

                //Type is already registered.
                return;
            }

            if (s_typeLookup.TryGetValue(id, out Type existingType))
            {
                if (existingType != type)
                    throw new Exception("Existing type does not have a unique Guid. Type1:" + type.FullName + " Type2: " + existingType.FullName + " ID: " + id);

                //Type is already registered.
                return;
            }

            s_registeredType.Add(type, id);
            s_typeLookup.Add(id, type);
        }
    }

    #endregion
}