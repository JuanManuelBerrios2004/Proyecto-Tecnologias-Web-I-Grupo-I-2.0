using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.DTOs;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientoInventarioController
        : ControllerBase
    {
        private readonly IMovimientoInventarioService
            _movimientoService;

        private readonly CrearMovimientoDtoValidator
            _crearValidator;

        private readonly RegistrarEntradaDtoValidator
            _entradaValidator;

        public MovimientoInventarioController(
            IMovimientoInventarioService movimientoService,
            CrearMovimientoDtoValidator crearValidator,
            RegistrarEntradaDtoValidator entradaValidator)
        {
            _movimientoService = movimientoService;
            _crearValidator = crearValidator;
            _entradaValidator = entradaValidator;
        }
        
        /// <summary>
        /// CU-03: Registrar entrada de mercadería.
        /// </summary>
        [HttpPost("/api/inventory/inbound")]
        public async Task<IActionResult> RegistrarEntrada(
            [FromBody] RegistrarEntradaDto entradaDto)
        {
            var validationResult =
                await _entradaValidator
                    .ValidateAsync(entradaDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validación",

                    errors =
                        validationResult.Errors
                            .Select(x => new
                            {
                                field =
                                    x.PropertyName,

                                error =
                                    x.ErrorMessage
                            })
                });
            }

            var resultado =
                await _movimientoService
                    .RegistrarEntradaAsync(
                        entradaDto);

            return Ok(new
            {
                message =
                    "Entrada de mercadería registrada correctamente.",

                movimientoId =
                    resultado.Id,

                productoId =
                    resultado.ProductoId,

                usuarioId =
                    resultado.UsuarioId,

                tipoMovimiento =
                    resultado.TipoMovimiento,

                motivo =
                    resultado.Motivo,

                cantidadIngresada =
                    resultado.Cantidad,

                stockResultante =
                    resultado.StockResultante,

                observaciones =
                    resultado.Observaciones,

                fechaRegistro =
                    resultado.FechaRegistro
            });
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento(
            [FromBody] CrearMovimientoDto movimientoDto)
        {
            var validationResult =
                await _crearValidator
                    .ValidateAsync(movimientoDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validación",

                    errors =
                        validationResult.Errors
                            .Select(x => new
                            {
                                field =
                                    x.PropertyName,

                                error =
                                    x.ErrorMessage
                            })
                });
            }

            var resultado =
                await _movimientoService
                    .RegistrarMovimientoAsync(
                        movimientoDto);

            return CreatedAtAction(
                nameof(GetMovimientoById),
                new
                {
                    id = resultado.Id
                },
                resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult>
            GetMovimientoById(int id)
        {
            var movimiento =
                await _movimientoService
                    .ObtenerPorIdAsync(id);

            return Ok(movimiento);
        }
    }
}