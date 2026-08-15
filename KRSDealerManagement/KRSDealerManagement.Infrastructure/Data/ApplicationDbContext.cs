using System.Data;
using Microsoft.Data.SqlClient;

namespace KRSDealerManagement.Infrastructure.Data
{
    /// <summary>
    /// Manages database connections using Microsoft.Data.SqlClient (modern, supports latest SQL Server)
    /// </summary>
    public class ApplicationDbContext
    {
        private readonly string _connectionString;
        private SqlConnection? _scopedConnection;
        private IDbTransaction? _scopedTransaction;

        public ApplicationDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool IsInTransaction => _scopedTransaction != null;

        public IDbTransaction? CurrentTransaction => _scopedTransaction;

        public IDbConnection GetConnection()
        {
            // Always return a new connection. Callers dispose it; never hand out the scoped transaction connection.
            return new SqlConnection(_connectionString);
        }

        public (IDbConnection Connection, bool ShouldDispose) LeaseConnection()
        {
            if (_scopedConnection != null && _scopedConnection.State != System.Data.ConnectionState.Closed)
                return (_scopedConnection, false);

            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return (connection, true);
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await Task.CompletedTask;
            return connection;
        }

        public void BeginScopedTransaction()
        {
            if (_scopedTransaction != null)
                throw new InvalidOperationException("A transaction is already active.");

            _scopedConnection = new SqlConnection(_connectionString);
            _scopedConnection.Open();
            _scopedTransaction = _scopedConnection.BeginTransaction();
        }

        public void CommitScopedTransaction()
        {
            _scopedTransaction?.Commit();
            DisposeScopedTransaction();
        }

        public void RollbackScopedTransaction()
        {
            _scopedTransaction?.Rollback();
            DisposeScopedTransaction();
        }

        private void DisposeScopedTransaction()
        {
            _scopedTransaction?.Dispose();
            _scopedConnection?.Dispose();
            _scopedTransaction = null;
            _scopedConnection = null;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await operation(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
