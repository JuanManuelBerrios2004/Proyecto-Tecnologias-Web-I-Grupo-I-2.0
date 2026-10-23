using ControlInventario.Core.DTOs;
using FluentValidation;

namespace ControlInventario.Services.Validators
{
    public class RegistrarEntradaDtoValidator
        : AbstractValidator<RegistrarEntradaDto>
    {
        public RegistrarEntradaDtoValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0)
                .WithMessage("El ID del producto debe ser mayor que cero.");

            RuleFor(x => x.UsuarioId)
                .GreaterThan(0)
                .WithMessage("El ID del usuario debe ser mayor que cero.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0)
                .WithMessage("La cantidad ingresada debe ser un valor entero positivo mayor a cero.");

            RuleFor(x => x.Observacion)
                .MaximumLength(255)
                .WithMessage("La observación no puede superar los 255 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Observacion));
        }
    }
}