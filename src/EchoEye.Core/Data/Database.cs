using Microsoft.Data.Sqlite;
using System.Data;

namespace EchoEye.Core.Data;

/// <summary>
/// Base database context for SQLite operations
/// </summary>
public class Database : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public Database(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// Get or create database connection
    /// </summary>
    public SqliteConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    /// <summary>
    /// Execute a non-query command
    /// </summary>
    public int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
    {
        using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// Execute a scalar query
    /// </summary>
    public object? ExecuteScalar(string sql, params SqliteParameter[] parameters)
    {
        using var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return command.ExecuteScalar();
    }

    /// <summary>
    /// Execute a query and return a reader
    /// </summary>
    public SqliteDataReader ExecuteReader(string sql, params SqliteParameter[] parameters)
    {
        var command = GetConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return command.ExecuteReader();
    }

    /// <summary>
    /// Execute a query and return results as a list
    /// </summary>
    public List<T> ExecuteQuery<T>(string sql, Func<IDataReader, T> map, params SqliteParameter[] parameters)
    {
        var results = new List<T>();
        using var reader = ExecuteReader(sql, parameters);
        while (reader.Read())
        {
            results.Add(map(reader));
        }
        return results;
    }

    /// <summary>
    /// Begin a transaction
    /// </summary>
    public SqliteTransaction BeginTransaction()
    {
        return GetConnection().BeginTransaction();
    }

    /// <summary>
    /// Initialize database schema
    /// </summary>
    public void InitializeSchema(string schemaSql)
    {
        ExecuteNonQuery(schemaSql);
    }

    /// <summary>
    /// Check if table exists
    /// </summary>
    public bool TableExists(string tableName)
    {
        var result = ExecuteScalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name",
            new SqliteParameter("@name", tableName));
        return Convert.ToInt32(result) > 0;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
