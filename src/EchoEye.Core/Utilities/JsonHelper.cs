using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoEye.Core.Utilities;

/// <summary>
/// JSON serialization utilities
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serialize object to JSON
    /// </summary>
    public static string Serialize<T>(T obj, bool indented = true)
    {
        return JsonSerializer.Serialize(obj, indented ? Options : CompactOptions);
    }

    /// <summary>
    /// Deserialize JSON to object
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Serialize object to JSON bytes
    /// </summary>
    public static byte[] SerializeToBytes<T>(T obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, CompactOptions);
    }

    /// <summary>
    /// Deserialize JSON bytes to object
    /// </summary>
    public static T? DeserializeFromBytes<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, Options);
    }
}
