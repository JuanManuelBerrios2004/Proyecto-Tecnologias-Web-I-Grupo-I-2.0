using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
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
        public async Task<IActionResult> GetProductos(
            [FromQuery] string? nombre,
            [FromQuery] int? categoriaId,
            [FromQuery] bool? soloActivos)
        {
            try
            {
                var productos = await _productoService.GetProductosAsync(nombre, categoriaId, soloActivos);
                var productosDto = _mapper.Map<IEnumerable<ProductoDto>>(productos);

                var response = new ApiResponse<IEnumerable<ProductoDto>>(productosDto);
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al obtener los productos" });
            }
        }

        /// <summary>
        /// Consulta un producto por su identificador (CU-02).
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductoById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "El ID debe ser mayor que cero." });

            try
            {
                var producto = await _productoService.GetProductoByIdAsync(id);
                if (producto == null)
                    return NotFound(new { message = "Producto no encontrado." });

                var productoDto = _mapper.Map<ProductoDto>(producto);

                var response = new ApiResponse<ProductoDto>(productoDto);
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al obtener el producto" });
            }
        }

        /// <summary>
        /// Consulta el stock de productos activos por nombre y categoría (CU-05).
        /// Solo admite soloActivos=true. El parámetro stockMinimo no está soportado.
        /// </summary>
        [HttpGet("stock")]
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

            try
            {
                var stock = await _productoService.GetStockProductosAsync(query.Nombre, query.CategoriaId);

                var response = new ApiResponse<IEnumerable<ProductoStockDto>>(stock);
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al obtener el stock de productos" });
            }
        }

        #endregion

        #region Escritura (POST / PUT)

        /// <summary>
        /// Crea un producto en el catálogo (CU-02).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> InsertProducto([FromBody] CrearProductoDto productoDto)
        {
            // Validar el DTO
            var validationResult = await _crearValidator.ValidateAsync(productoDto);
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
                var producto = _mapper.Map<Producto>(productoDto);
                var creado = await _productoService.InsertProducto(producto);
                var resultDto = _mapper.Map<ProductoDto>(creado);

                var response = new ApiResponse<ProductoDto>(resultDto);
                return Created($"api/producto/{creado.Id}", response);
            }
            catch (BusinessException ex)
            {
                return StatusCode((int)ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al crear el producto" });
            }
        }

        /// <summary>
        /// Actualiza un producto existente (CU-02).
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProducto(int id, [FromBody] ActualizarProductoDto productoDto)
        {
            if (id <= 0)
                return BadRequest(new { message = "El ID debe ser mayor que cero." });

            // Validar el DTO
            var validationResult = await _actualizarValidator.ValidateAsync(productoDto);
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
                var cambios = _mapper.Map<Producto>(productoDto);
                cambios.Id = id;

                var actualizado = await _productoService.UpdateProducto(cambios);
                var resultDto = _mapper.Map<ProductoDto>(actualizado);

                var response = new ApiResponse<ProductoDto>(resultDto);
                return Ok(response);
            }
            catch (BusinessException ex)
            {
                return StatusCode((int)ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al actualizar el producto" });
            }
        }

        #endregion

        #region Desactivación (DELETE)

        /// <summary>
        /// Desactiva lógicamente un producto; la fila nunca se elimina físicamente (CU-02).
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "El ID debe ser mayor que cero." });

            try
            {
                var desactivado = await _productoService.DesactivarProductoAsync(id);
                if (!desactivado)
                    return NotFound(new { message = "Producto no encontrado." });

                return NoContent(); // 204 sin contenido
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al desactivar el producto" });
            }
        }

        #endregion
    }
}