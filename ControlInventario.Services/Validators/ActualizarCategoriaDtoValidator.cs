using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class ActualizarCategoriaDtoValidator : CategoriaDtoValidator<ActualizarCategoriaDto>
    {
        public ActualizarCategoriaDtoValidator() : base(dto => dto.Nombre, dto => dto.Descripcion)
        {
        }
    }
}
