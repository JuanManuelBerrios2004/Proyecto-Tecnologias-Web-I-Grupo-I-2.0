namespace ControlInventario.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductoRepository ProductoRepository { get; }
        ICategoriaRepository CategoriaRepository { get; }
        IMovimientoInventarioRepository MovimientoInventarioRepository { get; }
        IUsuarioRepository UsuarioRepository { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}