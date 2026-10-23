using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IProductoRepository _productoRepository;
        private readonly ConsultaStockQueryDtoValidator _validator;

        public InventoryController(
            IProductoRepository productoRepository,
            ConsultaStockQueryDtoValidator validator)
        {
            _productoRepository = productoRepository;
            _validator = validator;
        }

        /// <summary>
        /// CU-05: Consultar stock disponible. Lista los productos activos (RN-08)
        /// con nombre, categoría, stock actual y precio de venta, con filtros opcionales.
        /// No modifica ningún dato.
        /// </summary>
        // TODO (CU-01): agregar [Authorize] cuando exista el login con JWT.
        [HttpGet("stock")]
        public async Task<IActionResult> GetStock([FromQuery] ConsultaStockQueryDto query)
        {
            var validationResult = await _validator.ValidateAsync(query);
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
                var productos = (await _productoRepository
                    .GetStockProductosAsync(query.Nombre, query.CategoriaId)).ToList();

                var hayFiltros = !string.IsNullOrWhiteSpace(query.Nombre) || query.CategoriaId.HasValue;

                string mensaje;
                if (productos.Count > 0)
                    mensaje = "Consulta realizada correctamente.";
                else if (hayFiltros)
                    mensaje = "No se encontraron productos que coincidan con la búsqueda"; // Flujo alternativo 4a
                else
                    mensaje = "No hay productos activos registrados.";

                return Ok(new ConsultaStockResponseDto
                {
                    Mensaje = mensaje,
                    Total = productos.Count,
                    Datos = productos
                });
            }
            catch (Exception)
            {
                // No se expone el detalle interno de la base de datos al cliente.
                return StatusCode(500, new { message = "Error al consultar el stock de productos" });
            }
        }
    }
}