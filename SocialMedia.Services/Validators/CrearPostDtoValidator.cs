using FluentValidation;
using SocialMedia.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocialMedia.Services.Validators
{
    public class CrearPostDtoValidator : AbstractValidator<PostDto>
    {
        public CrearPostDtoValidator()
        {
            // Para creación, el ID no debería enviarse o debería ser 0
            RuleFor(x => x.Id)
                .Equal(0).When(x => x.Id != 0)
                .WithMessage("El ID debe ser 0 o no enviarse para la creación de un nuevo post.");

            // Validación para UserId
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El ID de usuario es obligatorio y debe ser mayor que cero.")
                .NotEmpty().WithMessage("El ID de usuario no puede estar vacío.");

            // Validación para Date
            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("La fecha es obligatoria.")
                .Must(BeAValidDate).WithMessage("La fecha debe tener un formato válido (dd-MM-yyyy o similar).")
                .Must(BeNotFutureDate).WithMessage("La fecha no puede ser futura.");

            // Validación para Description
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La descripción es obligatoria.")
                .MinimumLength(5).WithMessage("La descripción debe tener al menos 5 caracteres.")
                .MaximumLength(500).WithMessage("La descripción no puede exceder los 500 caracteres.");

            // Validación para Image (opcional)
            RuleFor(x => x.Imagen)
                .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.Imagen))
                .WithMessage("La URL de la imagen no es válida. Debe comenzar con http:// o https://");
        }

        private bool BeAValidDate(string date) => DateTime.TryParse(date, out _);

        private bool BeNotFutureDate(string date)
        {
            if (DateTime.TryParse(date, out var parsedDate))
                return parsedDate <= DateTime.Now;
            return true;
        }

        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
