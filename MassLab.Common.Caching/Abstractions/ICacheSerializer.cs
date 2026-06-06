namespace MassLab.Common.Caching.Abstractions;

/// <summary>
/// Abstraction for cache value serialization/deserialization.
/// </summary>
public interface ICacheSerializer
{
    /// <summary>Serializes a value to string.</summary>
    string Serialize<T>(T value);

    /// <summary>Deserializes a string to a value.</summary>
    T? Deserialize<T>(string data, string? key = null);
}
