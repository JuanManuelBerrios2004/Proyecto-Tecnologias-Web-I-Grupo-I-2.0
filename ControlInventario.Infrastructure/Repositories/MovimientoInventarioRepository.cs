using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class MovimientoInventarioRepository : IMovimientoInventarioRepository
    {
        private readonly InventarioContext _inventarioContext;

        public MovimientoInventarioRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public async Task<IEnumerable<MovimientoInventario>> GetAllMovimientosAsync()
        {
            var movimientos = await _inventarioContext.MovimientosInventario
                .AsNoTracking()
                .OrderByDescending(m => m.FechaRegistro)
                .ThenByDescending(m => m.Id)
                .ToListAsync();
            return movimientos;
        }

        public async Task<MovimientoInventario?> GetMovimientoByIdAsync(int id)
        {
            var movimiento = await _inventarioContext.MovimientosInventario
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            return movimiento;
        }

        public async Task<IEnumerable<MovimientoInventario>> GetMovimientosByProductoAsync(int productoId)
        {
            var movimientos = await _inventarioContext.MovimientosInventario
                .AsNoTracking()
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.FechaRegistro)
                .ThenByDescending(m => m.Id)
                .ToListAsync();
            return movimientos;
        }

        public async Task InsertMovimiento(MovimientoInventario movimiento)
        {
            _inventarioContext.MovimientosInventario.Add(movimiento);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task<ResultadoRegistroMovimiento> RegistrarMovimientoAsync(MovimientoInventario movimiento)
        {
            // El movimiento y el stock se confirman o se revierten juntos (RN-06).
            await using var transaccion = await _inventarioContext.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            // Producto: debe existir y estar activo (RN-08).
            var producto = await _inventarioContext.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == movimiento.ProductoId);

            if (producto == null)
            {
                await transaccion.RollbackAsync();
                return ResultadoRegistroMovimiento.Rechazado(MotivoRechazoMovimiento.ProductoNoEncontrado);
            }

            if (!producto.Activo)
            {
                await transaccion.RollbackAsync();
                return ResultadoRegistroMovimiento.Rechazado(
                    MotivoRechazoMovimiento.ProductoInactivo, producto.StockActual);
            }

            // Usuario: debe existir.
            var existeUsuario = await _inventarioContext.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Id == movimiento.UsuarioId);

            if (!existeUsuario)
            {
                await transaccion.RollbackAsync();
                return ResultadoRegistroMovimiento.Rechazado(MotivoRechazoMovimiento.UsuarioNoEncontrado);
            }

            // Actualización condicional atómica del stock (RN-09): la condición se
            // evalúa en la propia sentencia, por lo que dos salidas simultáneas no
            // pueden descontar sobre el mismo stock inicial.
            var esEntrada = string.Equals(movimiento.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase);

            var filasAfectadas = esEntrada
                ? await _inventarioContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE productos SET StockActual = StockActual + {movimiento.Cantidad} WHERE Id = {movimiento.ProductoId} AND Activo = 1")
                : await _inventarioContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE productos SET StockActual = StockActual - {movimiento.Cantidad} WHERE Id = {movimiento.ProductoId} AND Activo = 1 AND StockActual >= {movimiento.Cantidad}");

            if (filasAfectadas == 0)
            {
                await transaccion.RollbackAsync();

                // Se relee el producto para distinguir el motivo real del rechazo.
                var actual = await _inventarioContext.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == movimiento.ProductoId);

                if (actual == null)
                    return ResultadoRegistroMovimiento.Rechazado(MotivoRechazoMovimiento.ProductoNoEncontrado);

                if (!actual.Activo)
                    return ResultadoRegistroMovimiento.Rechazado(
                        MotivoRechazoMovimiento.ProductoInactivo, actual.StockActual);

                return ResultadoRegistroMovimiento.Rechazado(
                    MotivoRechazoMovimiento.StockInsuficiente, actual.StockActual);
            }

            // Se persiste el movimiento en la misma transacción que la actualización del stock.
            _inventarioContext.MovimientosInventario.Add(movimiento);
            await _inventarioContext.SaveChangesAsync();

            // Se lee el stock resultante dentro de la transacción, antes de confirmarla.
            var stockResultante = await _inventarioContext.Productos
                .AsNoTracking()
                .Where(p => p.Id == movimiento.ProductoId)
                .Select(p => p.StockActual)
                .FirstAsync();

            await transaccion.CommitAsync();

            return ResultadoRegistroMovimiento.Exito(stockResultante);
        }
    }
}