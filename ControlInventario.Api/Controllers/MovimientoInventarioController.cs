using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientoInventarioController : ControllerBase
    {
        private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
        private readonly CrearMovimientoDtoValidator _crearValidator;
        private readonly RegistrarEntradaDtoValidator _entradaValidator;

        public MovimientoInventarioController(
            IMovimientoInventarioRepository movimientoInventarioRepository,
            CrearMovimientoDtoValidator crearValidator,
            RegistrarEntradaDtoValidator entradaValidator)
        {
            _movimientoInventarioRepository = movimientoInventarioRepository;
            _crearValidator = crearValidator;
            _entradaValidator = entradaValidator;
        }

        /// <summary>
        /// CU-03: Registrar Entrada de Mercadería.
        /// </summary>
        [HttpPost("/api/inventory/inbound")]
        public async Task<IActionResult> RegistrarEntrada(
            [FromBody] RegistrarEntradaDto entradaDto)
        {
            var validationResult =
                await _entradaValidator.ValidateAsync(entradaDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validación",
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        error = e.ErrorMessage
                    })
                });
            }

            try
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
                    await _movimientoInventarioRepository
                        .RegistrarMovimientoAsync(movimiento);

                if (!resultado.Registrado)
                {
                    switch (resultado.Motivo)
                    {
                        case MotivoRechazoMovimiento.ProductoNoEncontrado:
                            return NotFound(new
                            {
                                message = "Producto no encontrado."
                            });

                        case MotivoRechazoMovimiento.UsuarioNoEncontrado:
                            return NotFound(new
                            {
                                message = "Usuario no encontrado."
                            });

                        case MotivoRechazoMovimiento.ProductoInactivo:
                            return BadRequest(new
                            {
                                message = "El producto está desactivado y no admite entradas de inventario."
                            });

                        default:
                            return BadRequest(new
                            {
                                message = "No fue posible registrar la entrada de mercadería."
                            });
                    }
                }

                return Ok(new
                {
                    message = "Entrada de mercadería registrada correctamente.",
                    movimientoId = movimiento.Id,
                    productoId = movimiento.ProductoId,
                    usuarioId = movimiento.UsuarioId,
                    tipoMovimiento = movimiento.TipoMovimiento,
                    motivo = movimiento.Motivo,
                    cantidadIngresada = movimiento.Cantidad,
                    stockResultante = resultado.StockResultante,
                    observaciones = movimiento.Observaciones,
                    fechaRegistro = movimiento.FechaRegistro
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Error al registrar la entrada de mercadería."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento(
            [FromBody] CrearMovimientoDto movimientoDto)
        {
            var validationResult =
                await _crearValidator.ValidateAsync(movimientoDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validación",
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        error = e.ErrorMessage
                    })
                });
            }

            try
            {
                var movimiento = new MovimientoInventario
                {
                    ProductoId = movimientoDto.ProductoId,
                    UsuarioId = movimientoDto.UsuarioId,
                    TipoMovimiento = movimientoDto.TipoMovimiento.Trim().ToUpperInvariant(),
                    Motivo = movimientoDto.Motivo.Trim().ToUpperInvariant(),
                    Cantidad = movimientoDto.Cantidad,
                    Observaciones = movimientoDto.Observacion,
                    FechaRegistro = DateTime.UtcNow
                };

                var resultado =
                    await _movimientoInventarioRepository
                        .RegistrarMovimientoAsync(movimiento);

                if (!resultado.Registrado)
                {
                    switch (resultado.Motivo)
                    {
                        case MotivoRechazoMovimiento.ProductoNoEncontrado:
                            return NotFound(new { message = "Producto no encontrado." });

                        case MotivoRechazoMovimiento.UsuarioNoEncontrado:
                            return NotFound(new { message = "Usuario no encontrado." });

                        case MotivoRechazoMovimiento.ProductoInactivo:
                            return BadRequest(new
                            {
                                message = "El producto está desactivado; no admite movimientos de inventario."
                            });

                        case MotivoRechazoMovimiento.StockInsuficiente:
                            return Conflict(new
                            {
                                message = "Stock insuficiente para registrar la salida.",
                                stockDisponible = resultado.StockDisponible
                            });

                        default:
                            return BadRequest(new
                            {
                                message = "No fue posible registrar el movimiento."
                            });
                    }
                }

                var movimientoCreado = new MovimientoInventarioDto
                {
                    Id = movimiento.Id,
                    ProductoId = movimiento.ProductoId,
                    UsuarioId = movimiento.UsuarioId,
                    TipoMovimiento = movimiento.TipoMovimiento,
                    Motivo = movimiento.Motivo,
                    Cantidad = movimiento.Cantidad,
                    Observaciones = movimiento.Observaciones,
                    FechaRegistro = movimiento.FechaRegistro,
                    StockResultante = resultado.StockResultante
                };

                return CreatedAtAction(
                    nameof(GetMovimientoById),
                    new { id = movimientoCreado.Id },
                    movimientoCreado);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Error al registrar el movimiento de inventario"
                });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMovimientoById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "El ID debe ser mayor que cero."
                });
            }

            try
            {
                var movimiento =
                    await _movimientoInventarioRepository
                        .GetMovimientoByIdAsync(id);

                if (movimiento == null)
                {
                    return NotFound(new
                    {
                        message = "Movimiento no encontrado."
                    });
                }

                return Ok(new MovimientoInventarioDto
                {
                    Id = movimiento.Id,
                    ProductoId = movimiento.ProductoId,
                    UsuarioId = movimiento.UsuarioId,
                    TipoMovimiento = movimiento.TipoMovimiento,
                    Motivo = movimiento.Motivo,
                    Cantidad = movimiento.Cantidad,
                    Observaciones = movimiento.Observaciones,
                    FechaRegistro = movimiento.FechaRegistro
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "Error al obtener el movimiento de inventario"
                });
            }
        }
    }
}