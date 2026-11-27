using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class CrearProductoDtoValidator : ProductoDtoValidator<CrearProductoDto>
    {
        public CrearProductoDtoValidator()
            : base(dto => dto.CategoriaId, dto => dto.Nombre, dto => dto.PrecioCompra, dto => dto.PrecioVenta)
        {
        }
    }
}
