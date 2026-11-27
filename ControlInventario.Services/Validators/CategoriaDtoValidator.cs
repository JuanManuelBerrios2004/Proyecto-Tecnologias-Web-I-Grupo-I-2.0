using System.Linq.Expressions;
using FluentValidation;

namespace ControlInventario.Services.Validators
{
    public abstract class CategoriaDtoValidator<TDto> : AbstractValidator<TDto>
    {
        protected CategoriaDtoValidator(
            Expression<Func<TDto, string?>> nombre,
            Expression<Func<TDto, string?>> descripcion)
        {
            RuleFor(nombre)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("El nombre de la categoría es obligatorio.")
                .Must(valor => valor?.Trim().Length <= 50)
                .WithMessage("El nombre de la categoría no puede superar los 50 caracteres.");

            RuleFor(descripcion)
                .MaximumLength(200)
                .WithMessage("La descripción de la categoría no puede superar los 200 caracteres.");
        }
    }
}
