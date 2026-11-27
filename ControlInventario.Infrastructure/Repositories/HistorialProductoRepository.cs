using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

namespace ControlInventario.Infrastructure.Repositories
{
    public class HistorialProductoRepository : IHistorialProductoRepository
    {
        private readonly InventarioContext _context;

        public HistorialProductoRepository(InventarioContext context)
        {
            _context = context;
        }

        public Task AgregarAsync(IEnumerable<HistorialProducto> registros)
        {
            return _context.HistorialProductos.AddRangeAsync(registros);
        }
    }
}
