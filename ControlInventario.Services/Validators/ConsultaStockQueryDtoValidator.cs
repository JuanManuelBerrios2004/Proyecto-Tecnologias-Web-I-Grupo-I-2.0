using FluentValidation;
using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Validators
{
    public class ConsultaStockQueryDtoValidator : AbstractValidator<ConsultaStockQueryDto>
    {
        public ConsultaStockQueryDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .Must(nombre => nombre == null || nombre.Trim().Length <= 100)
                .WithMessage("El tÃ©rmino de bÃºsqueda no puede exceder los 100 caracteres.");

            RuleFor(x => x.CategoriaId)
                .GreaterThan(0)
                .When(x => x.CategoriaId.HasValue)
                .WithMessage("El ID de categoría debe ser mayor que cero.");

            RuleFor(x => x.SoloActivos)
                .NotEqual(false)
                .WithMessage("La consulta de stock solo admite productos activos. Omita soloActivos o envíe true.");

            RuleFor(x => x.StockMinimo)
                .Null()
                .WithMessage("El parámetro stockMinimo no está soportado en la consulta de stock.");
        }
    }
}