using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly InventarioContext _inventarioContext;

        public ProductoRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public async Task<IEnumerable<Producto>> GetAllProductosAsync()
        {
            var productos = await _inventarioContext.Productos
                .Include(p => p.Categoria)
                .ToListAsync();
            return productos;
        }

        public async Task<Producto?> GetProductoByIdAsync(int id)
        {
            var producto = await _inventarioContext.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(x => x.Id == id);
            return producto;
        }

        public async Task<IEnumerable<Producto>> GetProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null)
        {
            var query = _inventarioContext.Productos.AsQueryable();

            var texto = nombre?.Trim();
            if (!string.IsNullOrEmpty(texto))
            {
                var busqueda = texto.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(busqueda));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (soloActivos == true)
            {
                query = query.Where(p => p.Activo);
            }

            var productos = await query
                .Include(p => p.Categoria)
                .ToListAsync();
            return productos;
        }

        public async Task InsertProducto(Producto producto)
        {
            _inventarioContext.Productos.Add(producto);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task UpdateProducto(Producto producto)
        {
            var existente = await _inventarioContext.Productos
                .FirstOrDefaultAsync(p => p.Id == producto.Id);

            if (existente == null)
            {
                return;
            }

            existente.CategoriaId = producto.CategoriaId;
            existente.Nombre = producto.Nombre;
            existente.PrecioCompra = producto.PrecioCompra;
            existente.PrecioVenta = producto.PrecioVenta;
            // RN-02: el stock nunca se sobrescribe desde el catálogo; solo cambia con movimientos.
            existente.Activo = producto.Activo;
            // `CreadoEn` no se modifica: conserva la fecha de alta original.
            existente.ActualizadoEn = DateTime.UtcNow;

            await _inventarioContext.SaveChangesAsync();
        }

        public async Task<bool> DesactivarProductoAsync(int id)
        {
            var producto = await _inventarioContext.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return false;
            }

            producto.Activo = false;
            producto.ActualizadoEn = DateTime.UtcNow;

            await _inventarioContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null, int? stockMinimo = null)
        {
            var query = _inventarioContext.Productos.AsNoTracking().AsQueryable();

            var texto = nombre?.Trim();
            if (!string.IsNullOrEmpty(texto))
            {
                var busqueda = texto.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(busqueda));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (soloActivos == true)
            {
                query = query.Where(p => p.Activo);
            }

            if (stockMinimo.HasValue)
            {
                query = query.Where(p => p.StockActual >= stockMinimo.Value);
            }

            var stock = await query
                .Select(p => new ProductoStockDto
                {
                    Id = p.Id,
                    CategoriaId = p.CategoriaId,
                    Nombre = p.Nombre,
                    PrecioVenta = p.PrecioVenta,
                    StockActual = p.StockActual,
                    Activo = p.Activo,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null
                })
                .ToListAsync();

            return stock;
        }
    }
}