
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public ProductoController(
            IMapper mapper,
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

        // CU-02: CONSULTAR CATALOGO DE PRODUCTOS
        [HttpGet]
        public async Task<IActionResult> GetProductos(
            [FromQuery] string? nombre,
            [FromQuery] int? categoriaId,
            [FromQuery] bool? soloActivos)
        {
            try
            {
                var productos = await _productoRepository
                    .GetProductosAsync(
                        nombre,
                        categoriaId,
                        soloActivos);

                var productosDto = _mapper.Map<
                    IEnumerable<ProductoDto>>(productos);

                return Ok(
                    new ApiResponse<IEnumerable<ProductoDto>>(
                        productosDto));
            }
            catch (Exception ex)
            {
                // Muestra el error completo en la terminal
                Console.WriteLine(
                    "========== ERROR MYSQL / PRODUCTOS ==========");

                Console.WriteLine(ex.ToString());

                Console.WriteLine(
                    "=============================================");

                // No exponer detalles internos al cliente
                return StatusCode(500, new
                {
                    message = "Error al consultar los productos"
                });
            }
        }

        // CU-02: CONSULTAR PRODUCTO POR ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductoById(int id)
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
                var producto = await _productoRepository
                    .GetProductoByIdAsync(id);

                if (producto == null)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                var productoDto = _mapper.Map<ProductoDto>(
                    producto);

                return Ok(
                    new ApiResponse<ProductoDto>(
                        productoDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al obtener el producto",
                    error = ex.Message
                });
            }
        }

        // CU-05: CONSULTAR STOCK DISPONIBLE
        [HttpGet("stock")]
        public async Task<IActionResult> GetStockProductos(
            [FromQuery] string? nombre,
            [FromQuery] int? categoriaId,
            [FromQuery] bool? soloActivos,
            [FromQuery] int? stockMinimo)
        {
            try
            {
                if (stockMinimo.HasValue &&
                    stockMinimo.Value < 0)
                {
                    return BadRequest(new
                    {
                        message = "El stock minimo no puede ser negativo."
                    });
                }

                IEnumerable<ProductoStockDto> stock;

                if (soloActivos != false)
                {
                    stock = await _productoRepository
                        .GetStockProductosAsync(
                            nombre,
                            categoriaId);
                }
                else
                {
                    var productos = await _productoRepository
                        .GetProductosAsync(
                            nombre,
                            categoriaId,
                            false);

                    stock = _mapper.Map<
                        IEnumerable<ProductoStockDto>>(
                            productos);
                }

                if (stockMinimo.HasValue)
                {
                    stock = stock.Where(
                        p => p.StockActual >= stockMinimo.Value);
                }

                stock = stock
                    .OrderBy(p => p.Nombre)
                    .ToList();

                return Ok(
                    new ApiResponse<IEnumerable<ProductoStockDto>>(
                        stock));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al obtener el stock",
                    error = ex.Message
                });
            }
        }

        // CU-02: REGISTRAR NUEVO PRODUCTO
        [HttpPost]
        public async Task<IActionResult> InsertProducto(
            [FromBody] CrearProductoDto productoDto)
        {
            var validacion = await _crearValidator
                .ValidateAsync(productoDto);

            if (!validacion.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validacion",
                    errors = validacion.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        error = e.ErrorMessage
                    })
                });
            }

            if (!await ExisteCategoriaAsync(
                productoDto.CategoriaId))
            {
                return BadRequest(new
                {
                    message = "La categoria indicada no existe."
                });
            }

            try
            {
                var producto = _mapper.Map<Producto>(
                    productoDto);

                await _productoRepository.InsertProducto(
                    producto);

                var creado = await _productoRepository
                    .GetProductoByIdAsync(producto.Id);

                var resultado = _mapper.Map<ProductoDto>(
                    creado ?? producto);

                return Created(
                    $"api/producto/{producto.Id}",
                    new ApiResponse<ProductoDto>(resultado));
            }
            catch (DbUpdateException ex)
                when (EsErrorMySql(ex, 1062))
            {
                return Conflict(new
                {
                    message = "Ya existe un producto con ese nombre."
                });
            }
            catch (DbUpdateException ex)
                when (EsErrorMySql(ex, 1452))
            {
                return BadRequest(new
                {
                    message = "La categoria indicada no existe."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al crear el producto",
                    error = ex.Message
                });
            }
        }

        // CU-02: ACTUALIZAR PRODUCTO
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProducto(
            int id,
            [FromBody] ActualizarProductoDto productoDto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "El ID debe ser mayor que cero."
                });
            }

            var validacion = await _actualizarValidator
                .ValidateAsync(productoDto);

            if (!validacion.IsValid)
            {
                return BadRequest(new
                {
                    message = "Error de validacion",
                    errors = validacion.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        error = e.ErrorMessage
                    })
                });
            }

            try
            {
                var producto = await _productoRepository
                    .GetProductoByIdAsync(id);

                if (producto == null)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                if (!await ExisteCategoriaAsync(
                    productoDto.CategoriaId))
                {
                    return BadRequest(new
                    {
                        message = "La categoria indicada no existe."
                    });
                }

                _mapper.Map(productoDto, producto);

                await _productoRepository.UpdateProducto(
                    producto);

                var actualizado = await _productoRepository
                    .GetProductoByIdAsync(id);

                var resultado = _mapper.Map<ProductoDto>(
                    actualizado ?? producto);

                return Ok(
                    new ApiResponse<ProductoDto>(resultado));
            }
            catch (DbUpdateException ex)
                when (EsErrorMySql(ex, 1062))
            {
                return Conflict(new
                {
                    message = "Ya existe un producto con ese nombre."
                });
            }
            catch (DbUpdateException ex)
                when (EsErrorMySql(ex, 1452))
            {
                return BadRequest(new
                {
                    message = "La categoria indicada no existe."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al actualizar el producto",
                    error = ex.Message
                });
            }
        }

        // CU-02: DESACTIVAR PRODUCTO
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProducto(
            int id)
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
                var desactivado = await _productoRepository
                    .DesactivarProductoAsync(id);

                if (!desactivado)
                {
                    return NotFound(new
                    {
                        message = "Producto no encontrado."
                    });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al desactivar el producto",
                    error = ex.Message
                });
            }
        }

        // VALIDAR EXISTENCIA DE CATEGORIA
        private async Task<bool> ExisteCategoriaAsync(
            int categoriaId)
        {
            return await _inventarioContext.Categorias
                .AnyAsync(c => c.Id == categoriaId);
        }

        // IDENTIFICAR ERRORES DE MYSQL
        private static bool EsErrorMySql(
            DbUpdateException ex,
            int numeroError)
        {
            Exception? actual = ex;

            while (actual != null)
            {
                if (actual is MySqlException mysql &&
                    mysql.Number == numeroError)
                {
                    return true;
                }

                actual = actual.InnerException;
            }

            return false;
        }
    }
}
