using ControlInventario.Core.Entities;

namespace ControlInventario.Core.Interfaces
{
    public interface IHistorialProductoRepository
    {
        Task AgregarAsync(IEnumerable<HistorialProducto> registros);
    }
}
