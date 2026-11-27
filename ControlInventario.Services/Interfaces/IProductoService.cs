using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;

namespace ControlInventario.Services.Interfaces
{
    public interface IProductoService
    {
        Task<IEnumerable<Producto>> GetProductosAsync(string? nombre, int? categoriaId, bool? soloActivos);
        Task<Producto> GetProductoByIdAsync(int id);
        Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre, int? categoriaId);
        Task<ConsultaStockResponseDto> ConsultarStockAsync(string? nombre, int? categoriaId);
        Task<Producto> InsertProducto(Producto producto);
        Task<Producto> UpdateProducto(Producto cambios, int usuarioId);
        Task DesactivarProductoAsync(int id, int usuarioId);
    }
}
