namespace KRSDealerManagement.Domain.Repositories
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// Base interface for all domain repositories
    /// </summary>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get entity by ID
        /// </summary>
        Task<T> GetByIdAsync(int id);

        /// <summary>
        /// Get all entities
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Add new entity
        /// </summary>
        Task<int> AddAsync(T entity);

        /// <summary>
        /// Update existing entity
        /// </summary>
        Task<bool> UpdateAsync(T entity);

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Check if entity exists by ID
        /// </summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Get count of all entities
        /// </summary>
        Task<int> CountAsync();
    }
}
