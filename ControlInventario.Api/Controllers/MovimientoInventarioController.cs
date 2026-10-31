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

        public MovimientoInventarioController(
            IMovimientoInventarioRepository movimientoInventarioRepository,
            CrearMovimientoDtoValidator crearValidator)
        {
            _movimientoInventarioRepository = movimientoInventarioRepository;
            _crearValidator = crearValidator;
        }
        
        /// <summary>
        /// Registra una entrada o una salida de inventario y actualiza el stock del
        /// producto dentro de la misma transacción (RN-05, RN-06).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] CrearMovimientoDto movimientoDto)
        {
            var validationResult = await _crearValidator.ValidateAsync(movimientoDto);
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

                var resultado = await _movimientoInventarioRepository.RegistrarMovimientoAsync(movimiento);

                if (!resultado.Registrado)
                {
                    switch (resultado.Motivo)
                    {
                        case MotivoRechazoMovimiento.ProductoNoEncontrado:
                            return NotFound(new { message = "Producto no encontrado." });

                        case MotivoRechazoMovimiento.UsuarioNoEncontrado:
                            return NotFound(new { message = "Usuario no encontrado." });

                        case MotivoRechazoMovimiento.ProductoInactivo:
                            return BadRequest(new { message = "El producto está desactivado; no admite movimientos de inventario." });

                        case MotivoRechazoMovimiento.StockInsuficiente:
                            return Conflict(new
                            {
                                message = "Stock insuficiente para registrar la salida.",
                                stockDisponible = resultado.StockDisponible
                            });

                        default:
                            return BadRequest(new { message = "No fue posible registrar el movimiento." });
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

                return CreatedAtAction(nameof(GetMovimientoById), new { id = movimientoCreado.Id }, movimientoCreado);
            }
            catch (Exception)
            {
                // No se expone el detalle interno de la base de datos al cliente.
                return StatusCode(500, new { message = "Error al registrar el movimiento de inventario" });
            }
        }

        /// <summary>
        /// Consulta un movimiento por su identificador.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMovimientoById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "El ID debe ser mayor que cero." });

            try
            {
                var movimiento = await _movimientoInventarioRepository.GetMovimientoByIdAsync(id);
                if (movimiento == null)
                    return NotFound(new { message = "Movimiento no encontrado." });

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
                return StatusCode(500, new { message = "Error al obtener el movimiento de inventario" });
            }
        }
    }
}