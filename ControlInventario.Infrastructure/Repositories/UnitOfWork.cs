using System.Data;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ControlInventario.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InventarioContext _context;
        private IProductoRepository? _productoRepository;
        private IHistorialProductoRepository? _historialProductoRepository;
        private ICategoriaRepository? _categoriaRepository;
        private IUsuarioRepository? _usuarioRepository;
        private IMovimientoInventarioRepository? _movimientoInventarioRepository;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(InventarioContext context)
        {
            _context = context;
        }

        public IProductoRepository ProductoRepository =>
            _productoRepository ??= new ProductoRepository(_context);

        public IHistorialProductoRepository HistorialProductoRepository =>
            _historialProductoRepository ??= new HistorialProductoRepository(_context);

        public ICategoriaRepository CategoriaRepository =>
            _categoriaRepository ??= new CategoriaRepository(_context);

        public IUsuarioRepository UsuarioRepository =>
            _usuarioRepository ??= new UsuarioRepository(_context);

        public IMovimientoInventarioRepository MovimientoInventarioRepository =>
            _movimientoInventarioRepository ??= new MovimientoInventarioRepository(_context);

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                throw new InvalidOperationException("El contexto ya tiene una transacción activa.");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            var transaction = _transaction
                ?? throw new InvalidOperationException("No hay una transacción de la unidad de trabajo para confirmar.");

            try
            {
                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            finally
            {
                _transaction = null;
                await transaction.DisposeAsync();
            }
        }

        public async Task RollbackAsync()
        {
            var transaction = _transaction;
            if (transaction == null)
            {
                return;
            }

            try
            {
                await transaction.RollbackAsync();
            }
            finally
            {
                _transaction = null;
                _context.ChangeTracker.Clear();
                await transaction.DisposeAsync();
            }
        }

        public IDbConnection? GetDbConnection()
        {
            return _context.Database.GetDbConnection();
        }

        public IDbTransaction? GetDbTransaction()
        {
            return _transaction?.GetDbTransaction();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }
}
