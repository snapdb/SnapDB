//******************************************************************************************************
//  ResourceQueue.cs - Gbtc
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
//  4/18/2012 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/15/2023 - Lillian Gensolin
//       Converted code to .NET core.
//******************************************************************************************************

using System.Collections.Concurrent;

namespace SnapDB.Collections;

/// <summary>
/// Provides a specialized, thread-safe queue that acts as a quazi buffer pool. 
/// </summary>
/// <typeparam name="T">The type of resources managed by the queue (must be a reference type)</typeparam>
public class ResourceQueue<T>
    where T : class
{
    private readonly ConcurrentQueue<T> m_queue;
    private readonly Func<T> m_instanceObject;
    private readonly int m_maximumCount;


    /// <summary>
    /// Creates a new Resource Queue with the specified initial resources and maximum capacity.
    /// </summary>
    /// <param name="instance">A delegate that will return the necessary queue.</param>
    /// <param name="initialCount">The initial number of resources to have in the queue.</param>
    /// <param name="maximumCount">The maximum number of resources the queue can hold.</param>
    public ResourceQueue(Func<T> instance, int initialCount, int maximumCount)
    {
        if (instance is null)
            throw new ArgumentNullException("instance");
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException("initialCount", "Must be positive");
        if (maximumCount < initialCount)
            throw new ArgumentOutOfRangeException("maximumCount", "Must be greater than or equal to initialCount");

        m_instanceObject = instance;
        m_queue = new ConcurrentQueue<T>();
        m_maximumCount = maximumCount;

        // Initialize the queue with the specified initialCount of resources.
        for (int x = 0; x < initialCount; x++)
            m_queue.Enqueue(m_instanceObject());
    }

    /// <summary>
    /// Removes an item from the queue. If one does not exist, a new item is created.
    /// </summary>
    /// <returns>The dequeued or newly recreated item of type T.</returns>
    public T Dequeue()
    {
        if (m_queue.TryDequeue(result: out T item))
            return item;

        // Create a new instance of T using the provided delegate if the queue is empty.
        return m_instanceObject();
    }

    /// <summary>
    /// Adds an item back to the queue, so long as queue is not at maximum capacity.
    /// </summary>
    /// <param name="resource">The resource of type T to enqueue.</param>
    public void Enqueue(T resource)
    {
        //If a race condition exists, too many items will be added to the queue. 
        //Since it matters little that too many items are queued, that's not
        //worth the extra complexity of synchronizing.
        if (m_queue.Count < m_maximumCount)
            m_queue.Enqueue(resource);
    }
}