using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;
using ControlInventario.Core.Security;

namespace ControlInventario.Api.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ConsultaStockQueryDtoValidator _validator;

        public InventoryController(
            IProductoService productoService,
            ConsultaStockQueryDtoValidator validator)
        {
            _productoService = productoService;
            _validator = validator;
        }

        /// <summary>
        /// CU-05: Consultar stock disponible. Lista los productos activos (RN-08)
        /// con nombre, categoría, stock actual y precio de venta, con filtros opcionales.
        /// No modifica ningún dato.
        /// Alias temporal compatible; la ruta oficial es /api/Producto/stock.
        /// </summary>
        [HttpGet("stock")]
        [Authorize(Policy = RoleNames.ConsultaStockPolicy)]
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

            var response = await _productoService.ConsultarStockAsync(query.Nombre, query.CategoriaId);
            return Ok(new ApiResponse<ConsultaStockResponseDto>(response));
        }
    }
}