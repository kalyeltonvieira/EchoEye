using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Management.Configuration;

/// <summary>
/// Provides configuration management
/// </summary>
public class ConfigurationProvider : IConfigurationProvider
{
    private readonly Database _database;
    private readonly ILogger _logger;
    private readonly Subject<ConfigurationChange> _changeSubject;
    private Dictionary<string, Dictionary<string, object?>> _cache;

    public ConfigurationProvider(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
        _changeSubject = new Subject<ConfigurationChange>();
        _cache = new Dictionary<string, Dictionary<string, object?>>();
    }

    public T GetValue<T>(string section, string key, T? defaultValue = default)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(section, out var sectionCache) && sectionCache.TryGetValue(key, out var value))
            {
                return ConvertValue<T>(value);
            }

            // Load from database
            var sql = @"
                SELECT Value, ValueType FROM Configuration
                WHERE Section = @Section AND Key = @Key";

            var parameters = new[]
            {
                new SqliteParameter("@Section", section),
                new SqliteParameter("@Key", key)
            };

            var result = _database.ExecuteQuery(sql, reader =>
            {
                var val = reader.GetString("Value");
                var valueType = reader.GetString("ValueType");

                return ConvertValueType(val, valueType);
            }, parameters).FirstOrDefault();

            if (result != null)
            {
                // Update cache
                if (!_cache.ContainsKey(section))
                {
                    _cache[section] = new Dictionary<string, object?>();
                }
                _cache[section][key] = result;

                return ConvertValue<T>(result);
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration value: {Section}/{Key}", section, key);
            return defaultValue;
        }
    }

    public void SetValue(string section, string key, object value)
    {
        try
        {
            var oldValue = GetValue<object>(section, key);
            var valueType = GetValueTypeName(value);

            var sql = @"
                INSERT OR REPLACE INTO Configuration (
                    ConfigId, Section, Key, Value, ValueType, ModifiedAt, ModifiedBy
                ) VALUES (
                    @ConfigId, @Section, @Key, @Value, @ValueType, @ModifiedAt, @ModifiedBy
                )";

            var parameters = new[]
            {
                new SqliteParameter("@ConfigId", CryptoHelper.GenerateUuid()),
                new SqliteParameter("@Section", section),
                new SqliteParameter("@Key", key),
                new SqliteParameter("@Value", value.ToString() ?? string.Empty),
                new SqliteParameter("@ValueType", valueType),
                new SqliteParameter("@ModifiedAt", DateTime.UtcNow.ToString("o")),
                new SqliteParameter("@ModifiedBy", "Management")
            };

            _database.ExecuteNonQuery(sql, parameters);

            // Update cache
            if (!_cache.ContainsKey(section))
            {
                _cache[section] = new Dictionary<string, object?>();
            }
            _cache[section][key] = value;

            // Notify subscribers
            _changeSubject.OnNext(new ConfigurationChange
            {
                Section = section,
                Key = key,
                OldValue = oldValue,
                NewValue = value,
                ChangedAt = DateTime.UtcNow
            });

            _logger.LogDebug("Configuration value set: {Section}/{Key}", section, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration value: {Section}/{Key}", section, key);
        }
    }

    public IDictionary<string, object> GetSection(string section)
    {
        try
        {
            if (_cache.TryGetValue(section, out var sectionCache))
            {
                return new Dictionary<string, object>(sectionCache);
            }

            var sql = "SELECT Key, Value, ValueType FROM Configuration WHERE Section = @Section";
            var parameters = new[] { new SqliteParameter("@Section", section) };

            var result = _database.ExecuteQuery(sql, reader =>
            {
                var key = reader.GetString("Key");
                var value = reader.GetString("Value");
                var valueType = reader.GetString("ValueType");

                return new
                {
                    Key = key,
                    Value = ConvertValueType(value, valueType)
                };
            }, parameters);

            var sectionDict = result.ToDictionary(x => x.Key, x => x.Value);
            _cache[section] = sectionDict;

            return sectionDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration section: {Section}", section);
            return new Dictionary<string, object>();
        }
    }

    public async Task ReloadAsync()
    {
        _cache.Clear();
        _logger.LogInformation("Configuration cache cleared and reloaded");
        await Task.CompletedTask;
    }

    public IObservable<ConfigurationChange> WatchChanges(string? section = null)
    {
        if (string.IsNullOrEmpty(section))
        {
            return _changeSubject.AsObservable();
        }

        return _changeSubject
            .Where(change => change.Section == section)
            .AsObservable();
    }

    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true, Errors = new List<string>(), Warnings = new List<string>() };

        // Validate critical configuration values
        var protectionEnabled = GetValue<bool>("protection", "enabled");
        if (!protectionEnabled)
        {
            result.Warnings.Add("Protection is disabled");
        }

        var confidenceThreshold = GetValue<int>("protection", "confidenceThreshold");
        if (confidenceThreshold < 0 || confidenceThreshold > 100)
        {
            result.IsValid = false;
            result.Errors.Add("Confidence threshold must be between 0 and 100");
        }

        var recoveryEnabled = GetValue<bool>("recovery", "enabled");
        if (!recoveryEnabled)
        {
            result.Warnings.Add("Recovery is disabled");
        }

        return result;
    }

    private T ConvertValue<T>(object value)
    {
        try
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)Convert.ToBoolean(value);
            }
            if (typeof(T) == typeof(int))
            {
                return (T)(object)Convert.ToInt32(value);
            }
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Convert.ToDouble(value);
            }
            if (typeof(T) == typeof(string))
            {
                return (T)(object)Convert.ToString(value)!;
            }

            return (T)value;
        }
        catch
        {
            return default!;
        }
    }

    private object ConvertValueType(string value, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "bool" or "boolean" => bool.Parse(value),
            "int" or "integer" => int.Parse(value),
            "double" or "float" => double.Parse(value),
            "json" => value,
            _ => value
        };
    }

    private string GetValueTypeName(object value)
    {
        return value switch
        {
            bool => "bool",
            int => "int",
            double => "double",
            string => "string",
            _ => "string"
        };
    }
}
