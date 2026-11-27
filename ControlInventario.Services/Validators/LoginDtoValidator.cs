using FluentValidation;
using ControlInventario.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Services.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("El correo electrónico es obligatorio.")
                .MaximumLength(100)
                .WithMessage("El correo electrónico no puede exceder los 100 caracteres.")
                .EmailAddress()
                .WithMessage("El correo electrónico no tiene un formato válido.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("La contraseña es obligatoria.");
        }
    }
}