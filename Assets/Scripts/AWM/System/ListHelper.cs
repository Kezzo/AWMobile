using System.Collections.Generic;
using UnityEngine;

namespace AWM.System
{
    public class ListHelper
    {
        /// <summary>
        /// Shuffles a given list.
        /// </summary>
        /// <param name="listToShuffle">The list to shuffle.</param>
        public static void ShuffleList<T>(ref List<T> listToShuffle) where T : class
        {
            int n = listToShuffle.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = listToShuffle[k];
                listToShuffle[k] = listToShuffle[n];
                listToShuffle[n] = value;
            }
        }

    }
}
