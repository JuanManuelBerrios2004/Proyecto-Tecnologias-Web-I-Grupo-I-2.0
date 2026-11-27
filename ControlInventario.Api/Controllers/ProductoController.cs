using System.Globalization;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;
using ControlInventario.Core.Security;

namespace ControlInventario.Api.Controllers
{
    // Fase 6: aplicar la política de Dueño / Administrador al catálogo; stock tiene permisos propios.
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly IMapper _mapper;
        private readonly CrearProductoDtoValidator _crearValidator;
        private readonly ActualizarProductoDtoValidator _actualizarValidator;
        private readonly ConsultaStockQueryDtoValidator _stockValidator;

        public ProductoController(IMapper mapper,
            IProductoService productoService,
            CrearProductoDtoValidator crearValidator,
            ActualizarProductoDtoValidator actualizarValidator,
            ConsultaStockQueryDtoValidator stockValidator)
        {
            _productoService = productoService;
            _mapper = mapper;
            _crearValidator = crearValidator;
            _actualizarValidator = actualizarValidator;
            _stockValidator = stockValidator;
        }

        #region Consultas (GET)

        /// <summary>
        /// Lista el catálogo de productos. Todos los filtros son opcionales (CU-02).
        /// </summary>
        [HttpGet]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> GetProductos(
            [FromQuery] string? nombre,
            [FromQuery] int? categoriaId,
            [FromQuery] bool? soloActivos)
        {
            var productos = await _productoService.GetProductosAsync(nombre, categoriaId, soloActivos);
            var datos = _mapper.Map<IEnumerable<ProductoDto>>(productos);
            return Ok(new ApiResponse<IEnumerable<ProductoDto>>(datos));
        }

        /// <summary>
        /// Consulta un producto por su identificador (CU-02).
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> GetProductoById(int id)
        {
            ValidarId(id);
            var producto = await _productoService.GetProductoByIdAsync(id);
            var dto = _mapper.Map<ProductoDto>(producto);
            return Ok(new ApiResponse<ProductoDto>(dto));
        }

        /// <summary>
        /// Consulta el stock de productos activos por nombre y categoría (CU-05).
        /// Solo admite soloActivos=true. El parámetro stockMinimo no está soportado.
        /// </summary>
        [HttpGet("stock")]
        [Authorize(Policy = RoleNames.ConsultaStockPolicy)]
        public async Task<IActionResult> GetStockProductos([FromQuery] ConsultaStockQueryDto query)
        {
            var validationResult = await _stockValidator.ValidateAsync(query);
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

            var response = await _productoService.ConsultarStockAsync(query.Nombre, query.CategoriaId);
            return Ok(new ApiResponse<ConsultaStockResponseDto>(response));
        }

        #endregion

        #region Escritura (POST / PUT)

        /// <summary>
        /// Crea un producto en el catálogo (CU-02).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> InsertProducto([FromBody] CrearProductoDto productoDto)
        {
            await _crearValidator.ValidateAndThrowAsync(productoDto);
            var producto = _mapper.Map<Producto>(productoDto);
            var creado = await _productoService.InsertProducto(producto);
            return CreatedAtAction(nameof(GetProductoById), new { id = creado.Id }, _mapper.Map<ProductoDto>(creado));
        }

        /// <summary>
        /// Actualiza un producto existente (CU-02).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> UpdateProducto(int id, [FromBody] ActualizarProductoDto productoDto)
        {
            var usuarioId = ObtenerUsuarioIdAutenticado();
            ValidarId(id);
            await _actualizarValidator.ValidateAndThrowAsync(productoDto);
            var cambios = _mapper.Map<Producto>(productoDto);
            cambios.Id = id;

            var actualizado = await _productoService.UpdateProducto(cambios, usuarioId);
            return Ok(_mapper.Map<ProductoDto>(actualizado));
        }

        #endregion

        #region Desactivación (DELETE)

        /// <summary>
        /// Desactiva lógicamente un producto; la fila nunca se elimina físicamente (CU-02).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var usuarioId = ObtenerUsuarioIdAutenticado();
            ValidarId(id);
            await _productoService.DesactivarProductoAsync(id, usuarioId);
            return NoContent();
        }

        #endregion

        private int ObtenerUsuarioIdAutenticado()
        {
            var identificadores = User?.Identities
                .Where(identidad => identidad.IsAuthenticated)
                .SelectMany(identidad => identidad.FindAll(ClaimTypes.NameIdentifier))
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (identificadores is not { Length: 1 }
                || !int.TryParse(identificadores[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                || id <= 0)
            {
                throw new BusinessException("Se requiere una identidad de usuario autenticada válida.",
                    HttpStatusCode.Unauthorized);
            }

            return id;
        }

        private static void ValidarId(int id)
        {
            if (id <= 0)
            {
                throw new BusinessException("El ID debe ser mayor que cero.");
            }
        }
    }
}