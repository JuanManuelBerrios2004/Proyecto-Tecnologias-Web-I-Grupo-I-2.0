using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;

namespace ControlInventario.Services.Services
{
    public class MovimientoInventarioService
        : IMovimientoInventarioService
    {
        private readonly IMovimientoInventarioRepository
            _movimientoRepository;

        public MovimientoInventarioService(
            IMovimientoInventarioRepository movimientoRepository)
        {
            _movimientoRepository = movimientoRepository;
        }

        public async Task<MovimientoInventarioDto>
            RegistrarEntradaAsync(
                RegistrarEntradaDto entradaDto)
        {
            var movimiento = new MovimientoInventario
            {
                ProductoId = entradaDto.ProductoId,
                UsuarioId = entradaDto.UsuarioId,
                TipoMovimiento = "ENTRADA",
                Motivo = "COMPRA_PROVEEDOR",
                Cantidad = entradaDto.Cantidad,
                Observaciones = entradaDto.Observacion,
                FechaRegistro = DateTime.UtcNow
            };

            var resultado =
                await _movimientoRepository
                    .RegistrarMovimientoAsync(movimiento);

            ValidarResultado(resultado);

            return CrearDto(
                movimiento,
                resultado.StockResultante);
        }

        public async Task<MovimientoInventarioDto>
            RegistrarMovimientoAsync(
                CrearMovimientoDto movimientoDto)
        {
            var movimiento = new MovimientoInventario
            {
                ProductoId = movimientoDto.ProductoId,
                UsuarioId = movimientoDto.UsuarioId,

                TipoMovimiento =
                    movimientoDto.TipoMovimiento
                        .Trim()
                        .ToUpperInvariant(),

                Motivo =
                    movimientoDto.Motivo
                        .Trim()
                        .ToUpperInvariant(),

                Cantidad = movimientoDto.Cantidad,

                Observaciones =
                    movimientoDto.Observacion,

                FechaRegistro = DateTime.UtcNow
            };

            var resultado =
                await _movimientoRepository
                    .RegistrarMovimientoAsync(movimiento);

            ValidarResultado(resultado);

            return CrearDto(
                movimiento,
                resultado.StockResultante);
        }

        public async Task<MovimientoInventarioDto>
            ObtenerPorIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new BusinessException(
                    "El ID debe ser mayor que cero.",
                    400);
            }

            var movimiento =
                await _movimientoRepository
                    .GetMovimientoByIdAsync(id);

            if (movimiento == null)
            {
                throw new BusinessException(
                    "Movimiento no encontrado.",
                    404);
            }

            return CrearDto(
                movimiento,
                movimiento.StockResultante);
        }

        private static void ValidarResultado(
            ResultadoRegistroMovimiento resultado)
        {
            if (resultado.Registrado)
                return;

            switch (resultado.Motivo)
            {
                case MotivoRechazoMovimiento
                    .ProductoNoEncontrado:

                    throw new BusinessException(
                        "Producto no encontrado.",
                        404);

                case MotivoRechazoMovimiento
                    .UsuarioNoEncontrado:

                    throw new BusinessException(
                        "Usuario no encontrado.",
                        404);

                case MotivoRechazoMovimiento
                    .ProductoInactivo:

                    throw new BusinessException(
                        "El producto está desactivado y no admite movimientos de inventario.",
                        400);

                case MotivoRechazoMovimiento
                    .StockInsuficiente:

                    throw new BusinessException(
                        "Stock insuficiente para registrar la salida.",
                        409,
                        new
                        {
                            stockDisponible =
                                resultado.StockDisponible
                        });

                default:

                    throw new BusinessException(
                        "No fue posible registrar el movimiento de inventario.",
                        400);
            }
        }

        private static MovimientoInventarioDto CrearDto(
            MovimientoInventario movimiento,
            int stockResultante)
        {
            return new MovimientoInventarioDto
            {
                Id = movimiento.Id,
                ProductoId = movimiento.ProductoId,
                UsuarioId = movimiento.UsuarioId,
                TipoMovimiento = movimiento.TipoMovimiento,
                Motivo = movimiento.Motivo,
                Cantidad = movimiento.Cantidad,
                Observaciones = movimiento.Observaciones,
                FechaRegistro = movimiento.FechaRegistro,
                StockResultante = stockResultante
            };
        }
    }
}