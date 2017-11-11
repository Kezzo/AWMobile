namespace AWM.System
{
    /// <summary>
    /// Classes that implement this interface are able to store given serialized data and return that data based on a given key.
    /// It's assumed that the storage is non-volatile.
    /// </summary>
    public interface IStorageHelper
    {
        /// <summary>
        /// Stores the given data under the given key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="dataToStore">The data to store.</param>
        void StoreData(StorageKey key, string dataToStore);

        /// <summary>
        /// Returns previously stored data based on the given key.
        /// If no data can be found null is returned.
        /// </summary>
        /// <param name="key">The key the data was previously stored with.</param>
        string GetData(StorageKey key);
    }
}
