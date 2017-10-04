/// <summary>
/// Classes that implement this interface provide the ability to serialize given objects and deserialize given payloads.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes the given object and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the given object.</typeparam>
    /// <param name="objectToSerialize">The object to serialize.</param>
    string Serialize<T>(T objectToSerialize) where T : class, new();

    /// <summary>
    /// Deserializes the given string to the given object type and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the result object.</typeparam>
    /// <param name="stringToDeserialize">The payload to deserialize.</param>
    T Deserialize<T>(string stringToDeserialize) where T : class, new();
}
