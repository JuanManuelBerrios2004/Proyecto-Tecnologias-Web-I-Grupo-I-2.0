using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.Interfaces
{
    public interface IProductoRepository
    {
        /// <summary>
        /// Obtiene todos los productos sin aplicar filtros (CU-02).
        /// </summary>
        Task<IEnumerable<Producto>> GetAllProductosAsync();

        /// <summary>
        /// Obtiene un producto por su identificador.
        /// </summary>
        Task<Producto?> GetProductoByIdAsync(int id);

        /// <summary>
        /// Busca productos por nombre y, opcionalmente, por categoría (CU-02).
        /// Todos los parámetros son opcionales: sin argumentos devuelve el catálogo completo.
        /// </summary>
        /// <param name="nombre">Texto a buscar en `productos.Nombre`. Si es null o vacío no filtra por nombre.</param>
        /// <param name="categoriaId">Filtra por `productos.CategoriaId`. Si es null no filtra por categoría.</param>
        /// <param name="soloActivos">Si es true, devuelve únicamente productos con `Activo = true`.</param>
        Task<IEnumerable<Producto>> GetProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null);

        /// <summary>
        /// Crea un producto en el catálogo (CU-02).
        /// </summary>
        Task InsertProducto(Producto producto);

        /// <summary>
        /// Actualiza un producto existente (CU-02).
        /// </summary>
        Task UpdateProducto(Producto producto);

        /// <summary>
        /// Desactiva lógicamente un producto existente, sin eliminar la fila (CU-02).
        /// Devuelve false si el producto no existe.
        /// </summary>
        Task<bool> DesactivarProductoAsync(int id);

        /// <summary>
        /// Consulta los productos y sus cantidades de stock disponible (CU-05).
        /// Proyección de solo lectura: no expone columnas de auditoría ni datos de compra.
        /// </summary>
        /// <param name="nombre">Texto a buscar en `productos.Nombre`. Si es null o vacío no filtra por nombre.</param>
        /// <param name="categoriaId">Filtra por `productos.CategoriaId`. Si es null no filtra por categoría.</param>
        /// <param name="soloActivos">Si es true, devuelve únicamente productos con `Activo = true`.</param>
        /// <param name="stockMinimo">Si no es null, devuelve únicamente productos con `StockActual` mayor o igual al valor indicado.</param>
        Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null, int? stockMinimo = null);
    }
}