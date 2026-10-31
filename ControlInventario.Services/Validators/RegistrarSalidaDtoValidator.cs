using FluentValidation;
using ControlInventario.Core.DTOs;
using System;
using System.Linq;

namespace ControlInventario.Services.Validators
{
    public class RegistrarSalidaDtoValidator : AbstractValidator<RegistrarSalidaDto>
    {
        public RegistrarSalidaDtoValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0).WithMessage("El ID de producto es obligatorio y debe ser mayor que cero.");

            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("El ID de usuario es obligatorio y debe ser mayor que cero.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad a retirar debe ser mayor que cero.");

            // Paso 4 del CU-04: solo «Venta» o «Ajuste por Merma / Daño».
            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("El motivo de la salida es obligatorio.")
                .Must(BeMotivoDeSalida)
                .WithMessage("El motivo debe ser VENTA o MERMA_DETERIORO.");

            // La columna `Observaciones` admite hasta 255 caracteres.
            RuleFor(x => x.Observacion)
                .MaximumLength(255).WithMessage("La observación no puede exceder los 255 caracteres.");
        }

        private static bool BeMotivoDeSalida(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return false;

            return MotivosSalida.Permitidos
                .Contains(motivo.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }
}