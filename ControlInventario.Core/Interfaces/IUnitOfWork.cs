using System.Data;

namespace ControlInventario.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductoRepository ProductoRepository { get; }
        IHistorialProductoRepository HistorialProductoRepository { get; }
        ICategoriaRepository CategoriaRepository { get; }
        IUsuarioRepository UsuarioRepository { get; }
        IMovimientoInventarioRepository MovimientoInventarioRepository { get; }

        void SaveChanges();
        Task<int> SaveChangesAsync();

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        IDbConnection? GetDbConnection();
        IDbTransaction? GetDbTransaction();
    }
}
