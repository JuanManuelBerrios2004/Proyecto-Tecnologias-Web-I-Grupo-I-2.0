namespace ControlInventario.Core.Interfaces
{
    public interface IDapperContext
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
        Task<int> ExecuteAsync(string sql, object? param = null);
    }
}