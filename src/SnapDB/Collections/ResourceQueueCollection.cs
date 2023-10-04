//******************************************************************************************************
//  ResourceQueueCollection.cs - Gbtc
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
//  09/22/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

namespace SnapDB.Collections;

/// <summary>
/// Provides a thread-safe collection of many different resources of the same type.
/// </summary>
/// <typeparam name="TKey">An IComparable type key that is used to distinguish different resource queues.</typeparam>
/// <typeparam name="TResource">The type of the resource queue.</typeparam>
public class ResourceQueueCollection<TKey, TResource> where TResource : class where TKey : notnull
{
    #region [ Members ]

    private readonly Func<TKey, int> m_initialCount;
    private readonly Func<TKey, Func<TResource>> m_instanceObject;
    private readonly SortedList<TKey, ResourceQueue<TResource>?> m_list;
    private readonly Func<TKey, int> m_maximumCount;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceQueueCollection{TKey, TResource}"/> class
    /// with a default instance creation function and initial and maximum counts.
    /// </summary>
    /// <param name="instance">A function to create instances of the resource queues.</param>
    /// <param name="initialCount">The initial number of resources in each queue.</param>
    /// <param name="maximumCount">The maximum number of resources in each queue.</param>
    public ResourceQueueCollection(Func<TResource> instance, int initialCount, int maximumCount) : this(_ => instance, _ => initialCount, _ => maximumCount)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceQueueCollection{TKey, TResource}"/> class
    /// with a custom instance creation function and initial and maximum counts.
    /// </summary>
    /// <param name="instance">A function that returns the instance creation function for each key.</param>
    /// <param name="initialCount">The initial number of resources in each queue.</param>
    /// <param name="maximumCount">The maximum number of resources in each queue.</param>
    public ResourceQueueCollection(Func<TKey, TResource> instance, int initialCount, int maximumCount) : this(key => () => instance(key), _ => initialCount, _ => maximumCount)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceQueueCollection{TKey, TResource}"/> class
    /// with a custom instance creation function and initial and maximum counts.
    /// </summary>
    /// <param name="instance">A function that returns the instance creation function for each key.</param>
    /// <param name="initialCount">The initial number of resources in each queue.</param>
    /// <param name="maximumCount">The maximum number of resources in each queue.</param>
    public ResourceQueueCollection(Func<TKey, Func<TResource>> instance, int initialCount, int maximumCount) : this(instance, _ => initialCount, _ => maximumCount)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceQueueCollection{TKey, TResource}"/> class
    /// with a custom instance creation function, initial counts, and maximum counts.
    /// </summary>
    /// <param name="instance">A function that returns the instance creation function for each key.</param>
    /// <param name="initialCount">A function that specifies the initial count for each queue based on the key.</param>
    /// <param name="maximumCount">A function that specifies the maximum count for each queue based on the key.</param>
    public ResourceQueueCollection(Func<TKey, Func<TResource>> instance, Func<TKey, int> initialCount, Func<TKey, int> maximumCount)
    {
        m_instanceObject = instance;
        m_initialCount = initialCount;
        m_maximumCount = maximumCount;
        m_list = new SortedList<TKey, ResourceQueue<TResource>?>();
    }

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the resource queue for a key of <c>this</c>.
    /// </summary>
    /// <param name="key">The key identifying the resource queue to pull from.</param>
    public ResourceQueue<TResource> this[TKey key] => GetResourceQueue(key);

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Gets a resource queue associated with the specified key or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="key">The key associated with the resource queue.</param>
    /// <returns>
    /// A <see cref="ResourceQueue{TResource}"/> instance associated with the specified key.
    /// If the resource queue doesn't exist, a new one is created and added to the collection.
    /// </returns>
    /// <remarks>
    /// This method provides thread-safe access to resource queues. It attempts to retrieve an
    /// existing resource queue associated with the given key. If the queue doesn't exist, it creates
    /// a new resource queue based on the provided initialization parameters and adds it to the collection.
    /// </remarks>
    public ResourceQueue<TResource> GetResourceQueue(TKey key)
    {
        ResourceQueue<TResource>? resourceQueue;

        // Locking ensures thread safety while accessing or creating resource queues.
        lock (m_list)
        {
            // Try to retrieve an existing resource queue for the specified key.
            if (!m_list.TryGetValue(key, out resourceQueue))
            {
                // If it doesn't exist, create a new resource queue and add it to the collection.
                resourceQueue = new ResourceQueue<TResource>(m_instanceObject(key), m_initialCount(key), m_maximumCount(key));
                m_list.Add(key, resourceQueue);
            }
        }

        return resourceQueue ?? throw new NullReferenceException("Null resource is unexpected");
    }

    #endregion
}