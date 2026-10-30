namespace EchoEye.Core.Data;

/// <summary>
/// Generic repository interface for data access
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// Add new entity
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Update entity
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Delete entity
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Count entities
    /// </summary>
    Task<int> CountAsync();
}
