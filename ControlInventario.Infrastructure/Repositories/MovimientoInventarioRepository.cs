using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

namespace ControlInventario.Infrastructure.Repositories
{
    public class MovimientoInventarioRepository
        : BaseRepository<MovimientoInventario>,
          IMovimientoInventarioRepository
    {
        public MovimientoInventarioRepository(
            InventarioContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<MovimientoInventario>>
            GetAllMovimientosAsync()
        {
            return await Context.MovimientosInventario
                .AsNoTracking()
                .OrderByDescending(x => x.FechaRegistro)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<MovimientoInventario?>
            GetMovimientoByIdAsync(int id)
        {
            return await Context.MovimientosInventario
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<MovimientoInventario>>
            GetMovimientosByProductoAsync(int productoId)
        {
            return await Context.MovimientosInventario
                .AsNoTracking()
                .Where(x => x.ProductoId == productoId)
                .OrderByDescending(x => x.FechaRegistro)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<ResultadoRegistroMovimiento>
            RegistrarMovimientoAsync(
                MovimientoInventario movimiento)
        {
            await using var transaccion =
                await Context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var producto = await Context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == movimiento.ProductoId);

                if (producto == null)
                {
                    await transaccion.RollbackAsync();

                    return ResultadoRegistroMovimiento.Rechazado(
                        MotivoRechazoMovimiento.ProductoNoEncontrado);
                }

                if (!producto.Activo)
                {
                    await transaccion.RollbackAsync();

                    return ResultadoRegistroMovimiento.Rechazado(
                        MotivoRechazoMovimiento.ProductoInactivo,
                        producto.StockActual);
                }

                var existeUsuario = await Context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == movimiento.UsuarioId);

                if (!existeUsuario)
                {
                    await transaccion.RollbackAsync();

                    return ResultadoRegistroMovimiento.Rechazado(
                        MotivoRechazoMovimiento.UsuarioNoEncontrado);
                }

                bool esEntrada = string.Equals(
                    movimiento.TipoMovimiento,
                    "ENTRADA",
                    StringComparison.OrdinalIgnoreCase);

                int filasAfectadas;

                if (esEntrada)
                {
                    filasAfectadas =
                        await Context.Database
                            .ExecuteSqlInterpolatedAsync($@"
                                UPDATE productos
                                SET stock_actual =
                                    stock_actual + {movimiento.Cantidad}
                                WHERE id = {movimiento.ProductoId}
                                AND activo = 1
                            ");
                }
                else
                {
                    filasAfectadas =
                        await Context.Database
                            .ExecuteSqlInterpolatedAsync($@"
                                UPDATE productos
                                SET stock_actual =
                                    stock_actual - {movimiento.Cantidad}
                                WHERE id = {movimiento.ProductoId}
                                AND activo = 1
                                AND stock_actual >= {movimiento.Cantidad}
                            ");
                }

                if (filasAfectadas == 0)
                {
                    await transaccion.RollbackAsync();

                    var productoActual =
                        await Context.Productos
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x => x.Id == movimiento.ProductoId);

                    if (productoActual == null)
                    {
                        return ResultadoRegistroMovimiento.Rechazado(
                            MotivoRechazoMovimiento.ProductoNoEncontrado);
                    }

                    if (!productoActual.Activo)
                    {
                        return ResultadoRegistroMovimiento.Rechazado(
                            MotivoRechazoMovimiento.ProductoInactivo,
                            productoActual.StockActual);
                    }

                    return ResultadoRegistroMovimiento.Rechazado(
                        MotivoRechazoMovimiento.StockInsuficiente,
                        productoActual.StockActual);
                }

                await Context.MovimientosInventario
                    .AddAsync(movimiento);

                await Context.SaveChangesAsync();

                int stockResultante =
                    await Context.Productos
                        .AsNoTracking()
                        .Where(
                            x => x.Id == movimiento.ProductoId)
                        .Select(
                            x => x.StockActual)
                        .FirstAsync();

                await transaccion.CommitAsync();

                return ResultadoRegistroMovimiento.Exito(
                    stockResultante);
            }
            catch
            {
                await transaccion.RollbackAsync();

                throw;
            }
        }
    }
}