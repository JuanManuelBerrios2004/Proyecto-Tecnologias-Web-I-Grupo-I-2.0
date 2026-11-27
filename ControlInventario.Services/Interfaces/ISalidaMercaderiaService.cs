using System.Threading.Tasks;
using ControlInventario.Core.Entities;

namespace ControlInventario.Services.Interfaces
{
    public interface ISalidaMercaderiaService
    {
        Task<Producto> ObtenerProductoParaSalidaAsync(
            int productoId);

        Task<MovimientoInventario> RegistrarSalidaAsync(
            MovimientoInventario salida);
    }
}
