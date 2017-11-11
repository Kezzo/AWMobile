using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWM.System
{
    /// <summary>
    /// Class to store existing MonoBehaviour instances and provide access to them.
    /// </summary>
    public class MonoBehaviourRegistry
    {
        private readonly Dictionary<Type, MonoBehaviour> m_registeredMonoBehaviours = new Dictionary<Type, MonoBehaviour>();

        /// <summary>
        /// Registers the specified mono behaviour.
        /// </summary>
        /// <param name="monoBehaviour">The mono behaviour.</param>
        public void Register<T>(T monoBehaviour) where T : MonoBehaviour
        {
            Type type = monoBehaviour.GetType();

            if (m_registeredMonoBehaviours.ContainsKey(type))
            {
                m_registeredMonoBehaviours[type] = monoBehaviour;
            }
            else
            {
                m_registeredMonoBehaviours.Add(type, monoBehaviour);
            }
        }

        /// <summary>
        /// Tries to get a registered MonoBehaviour and returns it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public bool TryGet<T>(out T monoBehaviour) where T : MonoBehaviour
        {
            MonoBehaviour monoBehaviourToReturn = null;

            Type typeToGet = typeof(T);

            bool typeFound = m_registeredMonoBehaviours.TryGetValue(typeToGet, out monoBehaviourToReturn);

            monoBehaviour = typeFound ? (T) monoBehaviourToReturn : null;

            return typeFound;
        }

        /// <summary>
        /// Returns a registered instance.
        /// </summary>
        public T Get<T>() where T : MonoBehaviour
        {
            Type typeToGet = typeof (T);

            return (T) m_registeredMonoBehaviours[typeToGet];
        }

        /// <summary>
        /// Removes this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Remove<T>() where T : MonoBehaviour
        {
            m_registeredMonoBehaviours.Remove(typeof(T));
        }
    }
}
