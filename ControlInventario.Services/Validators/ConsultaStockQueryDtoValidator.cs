using FluentValidation;
using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class ConsultaStockQueryDtoValidator : AbstractValidator<ConsultaStockQueryDto>
    {
        public ConsultaStockQueryDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .MaximumLength(100)
                .WithMessage("El término de búsqueda no puede exceder los 100 caracteres.");

            RuleFor(x => x.CategoriaId)
                .GreaterThan(0)
                .When(x => x.CategoriaId.HasValue)
                .WithMessage("El ID de categoría debe ser mayor que cero.");
        }
    }
}