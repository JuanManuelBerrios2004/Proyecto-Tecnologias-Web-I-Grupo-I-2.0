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
    public class ProductoRepository : BaseRepository<Producto>, IProductoRepository
    {
        public ProductoRepository(InventarioContext inventarioContext) : base(inventarioContext)
        {
        }

        public async Task<IEnumerable<Producto>> GetAllProductosAsync()
        {
            var productos = await Entity
                .AsNoTracking()
                .Include(p => p.Categoria)
                .ToListAsync();
            return productos;
        }

        public async Task<Producto?> GetProductoByIdAsync(int id)
        {
            var producto = await Entity
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(x => x.Id == id);
            return producto;
        }

        public async Task<IEnumerable<Producto>> GetProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null)
        {
            var query = Entity.AsNoTracking();

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

        public async Task<Producto?> ObtenerParaModificarAsync(int id)
        {
            if (Context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException("La lectura para modificar requiere una transacción activa.");
            }

            // La consulta por clave primaria debe mantener FOR UPDATE en el SELECT exterior.
            var filas = await Entity
                .FromSqlInterpolated($"SELECT * FROM productos WHERE id = {id} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync();
            var producto = filas.SingleOrDefault();
            if (producto == null)
            {
                return null;
            }

            var entrada = Context.ChangeTracker.Entries<Producto>().SingleOrDefault(x => x.Entity.Id == id);
            if (entrada == null)
            {
                Entity.Attach(producto);
                return producto;
            }

            if (entrada.State != EntityState.Unchanged)
            {
                throw new InvalidOperationException("El producto ya tiene cambios pendientes en la unidad de trabajo.");
            }

            // La comparación parte de la versión bloqueada, incluso si ya se leyó en este contexto.
            entrada.CurrentValues.SetValues(producto);
            entrada.OriginalValues.SetValues(producto);
            entrada.State = EntityState.Unchanged;
            return entrada.Entity;
        }

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
        {
            var query = Entity.Where(producto => producto.Nombre == nombre);
            if (excluirId is int id)
            {
                query = query.Where(producto => producto.Id != id);
            }

            return query.AnyAsync();
        }

        public void UpdateProducto(Producto existente, Producto cambios)
        {
            existente.CategoriaId = cambios.CategoriaId;
            existente.Nombre = cambios.Nombre;
            existente.PrecioCompra = cambios.PrecioCompra;
            existente.PrecioVenta = cambios.PrecioVenta;
            // El catálogo conserva el stock, el estado y la fecha de alta.
        }

        public void DesactivarProducto(Producto producto)
        {
            producto.Activo = false;
        }

        public async Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre = null, int? categoriaId = null)
        {
            
            var query = Entity
                .AsNoTracking()
                .Where(p => p.Activo);

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

            var stock = await query
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoStockDto
                {
                    Id = p.Id,
                    CategoriaId = p.CategoriaId,
                    Nombre = p.Nombre,
                    PrecioVenta = p.PrecioVenta,
                    StockActual = p.StockActual,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : null
                })
                .ToListAsync();

            return stock;
        }
    }
}