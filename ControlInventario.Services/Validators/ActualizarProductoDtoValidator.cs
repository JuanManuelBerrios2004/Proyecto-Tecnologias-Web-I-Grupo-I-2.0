using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class ActualizarProductoDtoValidator : ProductoDtoValidator<ActualizarProductoDto>
    {
        public ActualizarProductoDtoValidator()
            : base(dto => dto.CategoriaId, dto => dto.Nombre, dto => dto.PrecioCompra, dto => dto.PrecioVenta)
        {
        }
    }
}
