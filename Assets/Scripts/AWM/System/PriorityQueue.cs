using System.Collections.Generic;

namespace AWM.System
{
    /// <summary>
    /// Class to provice the functionality of a PriorityQueue.
    /// A PriorityQueue enqueues an object to the queue with a given priority.
    /// When dequeuing the PriorityQueue returns the first queued object object of 
    /// the queue with the lowest priority value.
    /// Lower priority values are considered first.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PriorityQueue<T> where T : struct
    {
        private Dictionary<int, Queue<T>> m_queuedQueuesByPriority;
        public bool IsEmpty { get { return m_queuedQueuesByPriority == null || m_queuedQueuesByPriority.Count == 0; } }

        /// <summary>
        /// Enqueues an object to the queue with a given priority.
        /// </summary>
        /// <param name="objectToQueue">The object to queue.</param>
        /// <param name="priority">The priority.</param>
        public void Enqueue(T objectToQueue, int priority)
        {
            if (m_queuedQueuesByPriority == null)
            {
                m_queuedQueuesByPriority = new Dictionary<int, Queue<T>>();
            }

            if (m_queuedQueuesByPriority.ContainsKey(priority))
            {
                m_queuedQueuesByPriority[priority].Enqueue(objectToQueue);
            }
            else
            {
                Queue<T> queueToStore = new Queue<T>();
                queueToStore.Enqueue(objectToQueue);

                m_queuedQueuesByPriority.Add(priority, queueToStore);
            }
        }

        /// <summary>
        /// Returns the first queued object object of the queue with the lowest priority value.
        /// Lower priority values are considered first.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            if (m_queuedQueuesByPriority == null || m_queuedQueuesByPriority.Count == 0)
            {
                return default(T);
            }

            Queue<T> queueWithLowestPriority = null;
            int currentlyStoredLowestPriority = 0;

            foreach (var storedQueue in m_queuedQueuesByPriority)
            {
                if (queueWithLowestPriority == null || storedQueue.Key < currentlyStoredLowestPriority)
                {
                    queueWithLowestPriority = storedQueue.Value;
                    currentlyStoredLowestPriority = storedQueue.Key;
                }
            }

            if (queueWithLowestPriority == null)
            {
                return default(T);
            }

            T itemToReturn = queueWithLowestPriority.Dequeue();

            if (queueWithLowestPriority.Count == 0)
            {
                m_queuedQueuesByPriority.Remove(currentlyStoredLowestPriority);
            }

            return itemToReturn;
        }
    }
}
