using System.Data;

namespace EchoEye.Core.Utilities;

public static class DataReaderExtensions
{
    public static bool IsDBNull(this IDataRecord reader, string name)
    {
        return reader.IsDBNull(reader.GetOrdinal(name));
    }

    public static string GetString(this IDataRecord reader, string name)
    {
        return reader.GetString(reader.GetOrdinal(name));
    }

    public static int GetInt32(this IDataRecord reader, string name)
    {
        return reader.GetInt32(reader.GetOrdinal(name));
    }

    public static long GetInt64(this IDataRecord reader, string name)
    {
        return reader.GetInt64(reader.GetOrdinal(name));
    }

    public static double GetDouble(this IDataRecord reader, string name)
    {
        return reader.GetDouble(reader.GetOrdinal(name));
    }

    public static bool GetBoolean(this IDataRecord reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.GetValue(ordinal) switch
        {
            bool value => value,
            long value => value != 0,
            int value => value != 0,
            string value => bool.TryParse(value, out var parsed)
                ? parsed
                : value == "1",
            _ => Convert.ToBoolean(reader.GetValue(ordinal))
        };
    }
}
