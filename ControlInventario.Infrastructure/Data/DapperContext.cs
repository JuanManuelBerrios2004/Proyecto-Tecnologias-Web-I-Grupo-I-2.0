using System.Data;
using ControlInventario.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector; // o Microsoft.Data.SqlClient según tu proveedor

namespace ControlInventario.Infrastructure.Data
{
    public class DapperContext : IDapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>(sql, param);
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteAsync(sql, param);
        }
    }
}