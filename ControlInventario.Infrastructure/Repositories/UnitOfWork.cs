using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ControlInventario.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InventarioContext _context;
        private IDbContextTransaction? _transaction;

        private IProductoRepository? _productoRepository;
        private ICategoriaRepository? _categoriaRepository;
        private IMovimientoInventarioRepository? _movimientoRepository;
        private IUsuarioRepository? _usuarioRepository;

        public UnitOfWork(InventarioContext context)
        {
            _context = context;
        }

        public IProductoRepository ProductoRepository =>
            _productoRepository ??= new ProductoRepository(_context);

        public ICategoriaRepository CategoriaRepository =>
            _categoriaRepository ??= new CategoriaRepository(_context);

        public IMovimientoInventarioRepository MovimientoInventarioRepository =>
            _movimientoRepository ??= new MovimientoInventarioRepository(_context);

        public IUsuarioRepository UsuarioRepository =>
            _usuarioRepository ??= new UsuarioRepository(_context);

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
        }

        public void Dispose() => _context.Dispose();
    }
}