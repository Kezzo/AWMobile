using Newtonsoft.Json;

namespace AWM.System
{
    /// <summary>
    /// Serializes and deserializes data using the JSON implementation of Newtonsoft.
    /// The underlying library can be found here: https://github.com/SaladLab/Json.Net.Unity3D/releases
    /// </summary>
    public class NewtonsoftJsonSerializer : ISerializer
    {
        /// <summary>
        /// Serializes the given object and returns the JSON string.
        /// </summary>
        /// <typeparam name="T">The type of the given object.</typeparam>
        /// <param name="objectToSerialize">The object to serialize.</param>
        public string Serialize<T>(T objectToSerialize) where T : class, new()
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        /// <summary>
        /// Deserializes the given JSON string to the given object type and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the result object.</typeparam>
        /// <param name="stringToDeserialize">The payload to deserialize.</param>
        public T Deserialize<T>(string stringToDeserialize) where T : class, new()
        {
            return JsonConvert.DeserializeObject<T>(stringToDeserialize);
        }
    }
}
