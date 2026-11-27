using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class CrearCategoriaDtoValidator : CategoriaDtoValidator<CrearCategoriaDto>
    {
        public CrearCategoriaDtoValidator() : base(dto => dto.Nombre, dto => dto.Descripcion)
        {
        }
    }
}
