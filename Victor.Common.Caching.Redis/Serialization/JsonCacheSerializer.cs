using System.Text.Json;
using Victor.Common.Caching.Abstractions;
using Victor.Common.Caching.Exceptions;

namespace Victor.Common.Caching.Redis.Serialization;

/// <summary>
/// JSON serializer for Redis cache operations.
/// </summary>
public class JsonCacheSerializer : ICacheSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonCacheSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public string Serialize<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value, _options);
        }
        catch (Exception ex)
        {
            throw new CacheSerializationException(
                $"Failed to serialize object of type '{typeof(T).FullName}'.", ex);
        }
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string json, string? key = null)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex)
        {
            throw new CacheSerializationException(
                $"Failed to deserialize value for key '{key}'.", ex);
        }
    }
}
