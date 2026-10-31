using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class SalidaMercaderiaController : ControllerBase
    {
        private readonly ISalidaMercaderiaService _salidaService;
        private readonly IMapper _mapper;
        private readonly RegistrarSalidaDtoValidator _validator;

        public SalidaMercaderiaController(
            IMapper mapper,
            ISalidaMercaderiaService salidaService,
            RegistrarSalidaDtoValidator validator)
        {
            _salidaService = salidaService;
            _mapper = mapper;
            _validator = validator;
        }

        // TODO (CU-01): agregar [Authorize] cuando exista el login con JWT.
        [HttpGet("exit/{productoId:int}")]
        public async Task<IActionResult> GetProductoParaSalida(int productoId)
        {
            var producto = await _salidaService.ObtenerProductoParaSalidaAsync(productoId);
            var productoDto = _mapper.Map<ProductoSalidaInfoDto>(producto);

            var response = new ApiResponse<ProductoSalidaInfoDto>(productoDto);
            return Ok(response);
        }

        // TODO (CU-01): agregar [Authorize] y tomar UsuarioId del token en lugar del body.
        [HttpPost("exit")]
        public async Task<IActionResult> RegistrarSalida([FromBody] RegistrarSalidaDto salidaDto)
        {
            // Si no es válido lanza ValidationException -> el middleware responde 400.
            await _validator.ValidateAndThrowAsync(salidaDto);

            var salida = _mapper.Map<MovimientoInventario>(salidaDto);
            var registrada = await _salidaService.RegistrarSalidaAsync(salida);

            var resultDto = _mapper.Map<MovimientoInventarioDto>(registrada);

            var response = new ApiResponse<MovimientoInventarioDto>(resultDto);
            return Created($"api/MovimientoInventario/{registrada.Id}", response);
        }
    }
}