using System.Data;
using Dapper;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Generic Dapper repository - supports custom table names and primary key column names
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly string _tableName;
        protected readonly string _pkColumn;

        public Repository(ApplicationDbContext context, string tableName, string pkColumn = "Id")
        {
            _context = context;
            _tableName = tableName;
            _pkColumn = pkColumn;
        }

        protected async Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, IDbTransaction?, Task<TResult>> action)
        {
            var (connection, shouldDispose) = _context.LeaseConnection();
            try
            {
                return await action(connection, _context.CurrentTransaction);
            }
            finally
            {
                if (shouldDispose)
                    connection.Dispose();
            }
        }

        public virtual async Task<int> AddAsync(T entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.Name != _pkColumn && p.CanRead && p.CanWrite)
                    .ToList();

                var columnNames = string.Join(", ", properties.Select(p => p.Name));
                var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                var sql = $"INSERT INTO {_tableName} ({columnNames}) VALUES ({paramNames}); SELECT CAST(SCOPE_IDENTITY() as int)";

                return await connection.ExecuteScalarAsync<int>(sql, entity, transaction);
            });
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var sql = $"SELECT * FROM {_tableName} WHERE {_pkColumn} = @Id";
                return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, transaction);
            });
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<T>($"SELECT * FROM {_tableName}", transaction: transaction));
        }

        public virtual async Task<bool> UpdateAsync(T entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.Name != _pkColumn && p.CanRead && p.CanWrite)
                    .ToList();

                var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                var sql = $"UPDATE {_tableName} SET {setClause} WHERE {_pkColumn} = @{_pkColumn}";

                var rows = await connection.ExecuteAsync(sql, entity, transaction);
                return rows > 0;
            });
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var rows = await connection.ExecuteAsync(
                    $"DELETE FROM {_tableName} WHERE {_pkColumn} = @Id", new { Id = id }, transaction);
                return rows > 0;
            });
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var count = await connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(1) FROM {_tableName} WHERE {_pkColumn} = @Id", new { Id = id }, transaction);
                return count > 0;
            });
        }

        public virtual async Task<int> CountAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {_tableName}", transaction: transaction));
        }
    }
}
