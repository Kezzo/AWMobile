using UnityEngine;

namespace AWM.System
{
    /// <summary>
    /// Provides funtionality to store and return given data using Unitys PlayerPref class.
    /// </summary>
    public class PlayerPrefsStorageHelper : IStorageHelper
    {
        /// <summary>
        /// Stores the given data under the given key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="dataToStore">The data to store.</param>
        public void StoreData(StorageKey key, string dataToStore)
        {
            string castedKey = key.ToString();

            Debug.Log(!PlayerPrefs.HasKey(castedKey)
                ? string.Format("Storing new key: '{0}'", key)
                : string.Format("Updating data with key: '{0}'", key));

            PlayerPrefs.SetString(castedKey, dataToStore);
        }

        /// <summary>
        /// Returns previously stored data based on the given key.
        /// If no data can be found null is returned.
        /// </summary>
        /// <param name="key">The key the data was previously stored with.</param>
        public string GetData(StorageKey key)
        {
            string castedKey = key.ToString();

            if (!PlayerPrefs.HasKey(castedKey))
            {
                Debug.Log("Under the given key no stored data could be found.");
                return null;
            }

            return PlayerPrefs.GetString(castedKey);
        }
    }
}
