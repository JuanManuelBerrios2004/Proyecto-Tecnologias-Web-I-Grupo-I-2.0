using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IMapper _mapper;
        private readonly InventarioContext _inventarioContext;
        private readonly CrearProductoDtoValidator _crearValidator;
        private readonly ActualizarProductoDtoValidator _actualizarValidator;

        public ProductoController(IMapper mapper,
            IProductoRepository productoRepository,
            InventarioContext inventarioContext,
            CrearProductoDtoValidator crearValidator,
            ActualizarProductoDtoValidator actualizarValidator)
        {
            _productoRepository = productoRepository;
            _mapper = mapper;
            _inventarioContext = inventarioContext;
            _crearValidator = crearValidator;
            _actualizarValidator = actualizarValidator;
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
                var productos = await _productoRepository.GetProductosAsync(nombre, categoriaId, soloActivos);
                var productosDto = _mapper.Map<IEnumerable<ProductoDto>>(productos);

                var response = new ApiResponse<IEnumerable<ProductoDto>>(productosDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener los productos", error = ex.Message });
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
                var producto = await _productoRepository.GetProductoByIdAsync(id);
                if (producto == null)
                    return NotFound(new { message = "Producto no encontrado." });

                var productoDto = _mapper.Map<ProductoDto>(producto);

                var response = new ApiResponse<ProductoDto>(productoDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el producto", error = ex.Message });
            }
        }

        /// <summary>
        /// Consulta el stock disponible de los productos (CU-05).
        /// Si no se indica `soloActivos`, se muestran únicamente los productos activos.
        /// </summary>
        [HttpGet("stock")]
        public async Task<IActionResult> GetStockProductos(
            [FromQuery] string? nombre,
            [FromQuery] int? categoriaId,
            [FromQuery] bool? soloActivos,
            [FromQuery] int? stockMinimo)
        {
            try
            {
                // RN-08: por defecto solo se listan productos activos; `soloActivos=false` debe enviarse
                // explícitamente para consultar también los inactivos.
                var filtrarSoloActivos = soloActivos ?? true;

                // El repositorio ya proyecta a ProductoStockDto: no hace falta mapear de nuevo.
                var stock = await _productoRepository.GetStockProductosAsync(nombre, categoriaId, filtrarSoloActivos, stockMinimo);

                var response = new ApiResponse<IEnumerable<ProductoStockDto>>(stock);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener el stock de productos", error = ex.Message });
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

            if (!await ExisteCategoriaAsync(productoDto.CategoriaId))
            {
                return BadRequest(new { message = "La categoría indicada no existe." });
            }

            try
            {
                // RN-01: ProductoProfile fija stock 0, Activo = true y las fechas de auditoría.
                // La mercadería inicial se registra después mediante un movimiento de tipo ENTRADA.
                var producto = _mapper.Map<Producto>(productoDto);

                await _productoRepository.InsertProducto(producto);

                // Se vuelve a consultar para que la navegación `Categoria` esté cargada.
                var creado = await _productoRepository.GetProductoByIdAsync(producto.Id);
                var resultDto = _mapper.Map<ProductoDto>(creado ?? producto);

                var response = new ApiResponse<ProductoDto>(resultDto);
                return Created($"api/producto/{producto.Id}", response);
            }
            catch (DbUpdateException ex) when (EsErrorMySql(ex, 1062))
            {
                // Índice único `IX_productos_Nombre` violado.
                return Conflict(new { message = "Ya existe un producto registrado con ese nombre." });
            }
            catch (DbUpdateException ex) when (EsErrorMySql(ex, 1452))
            {
                // Llave foránea `FK_productos_categorias` inexistente.
                return BadRequest(new { message = "La categoría indicada no existe." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el producto", error = ex.Message });
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

            var productoExistente = await _productoRepository.GetProductoByIdAsync(id);
            if (productoExistente == null)
                return NotFound(new { message = "Producto no encontrado." });

            if (!await ExisteCategoriaAsync(productoDto.CategoriaId))
            {
                return BadRequest(new { message = "La categoría indicada no existe." });
            }

            try
            {
                // Se mapea sobre la entidad existente: ProductoProfile ignora StockActual (RN-02),
                // Activo, CreadoEn y ActualizadoEn, por lo que conservan su valor actual.
                _mapper.Map(productoDto, productoExistente);

                // El repositorio actualiza `ActualizadoEn`.
                await _productoRepository.UpdateProducto(productoExistente);

                // Se vuelve a consultar para que `Categoria` refleje un posible cambio de categoría.
                var actualizado = await _productoRepository.GetProductoByIdAsync(id);
                var resultDto = _mapper.Map<ProductoDto>(actualizado ?? productoExistente);

                var response = new ApiResponse<ProductoDto>(resultDto);
                return Ok(response);
            }
            catch (DbUpdateException ex) when (EsErrorMySql(ex, 1062))
            {
                // Índice único `IX_productos_Nombre` violado.
                return Conflict(new { message = "Ya existe un producto registrado con ese nombre." });
            }
            catch (DbUpdateException ex) when (EsErrorMySql(ex, 1452))
            {
                // Llave foránea `FK_productos_categorias` inexistente.
                return BadRequest(new { message = "La categoría indicada no existe." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el producto", error = ex.Message });
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
                var desactivado = await _productoRepository.DesactivarProductoAsync(id);
                if (!desactivado)
                    return NotFound(new { message = "Producto no encontrado." });

                return NoContent(); // 204 sin contenido
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al desactivar el producto", error = ex.Message });
            }
        }

        #endregion

        #region Utilidades

        /// <summary>
        /// Comprueba que la categoría exista. `ICategoriaRepository` no se usa porque sus
        /// métodos no están implementados; se consulta el contexto directamente.
        /// </summary>
        private async Task<bool> ExisteCategoriaAsync(int categoriaId)
        {
            return await _inventarioContext.Categorias.AnyAsync(c => c.Id == categoriaId);
        }

        /// <summary>
        /// Detecta un código de error de MySQL concreto dentro de la cadena de excepciones.
        /// 1062 = índice único duplicado, 1452 = llave foránea inexistente.
        /// </summary>
        private static bool EsErrorMySql(DbUpdateException ex, int numeroError)
        {
            Exception? actual = ex;

            while (actual != null)
            {
                if (actual is MySqlException mySqlException && mySqlException.Number == numeroError)
                    return true;

                actual = actual.InnerException;
            }

            return false;
        }

        #endregion
    }
}