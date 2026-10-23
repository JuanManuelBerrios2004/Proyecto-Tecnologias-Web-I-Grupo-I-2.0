using FluentValidation;
using ControlInventario.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Services.Validators
{
    public class ActualizarProductoDtoValidator : AbstractValidator<ActualizarProductoDto>
    {
        public ActualizarProductoDtoValidator()
        {
            // Validación para CategoriaId
            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("El ID de categoría es obligatorio y debe ser mayor que cero.")
                .NotEmpty().WithMessage("El ID de categoría no puede estar vacío.");

            // Validación para Nombre
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("El nombre del producto no puede estar vacío.")
                .MaximumLength(100).WithMessage("El nombre del producto no puede exceder los 100 caracteres.");

            // Validación para PrecioCompra
            RuleFor(x => x.PrecioCompra)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de compra no puede ser negativo.")
                .Must(BeValidDecimal10_2).WithMessage("El precio de compra debe tener máximo dos decimales y ajustarse a DECIMAL(10,2).");

            // Validación para PrecioVenta
            RuleFor(x => x.PrecioVenta)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de venta no puede ser negativo.")
                .Must(BeValidDecimal10_2).WithMessage("El precio de venta debe tener máximo dos decimales y ajustarse a DECIMAL(10,2).");

            // Validación para StockActual (el stock sí es editable durante la actualización)
            RuleFor(x => x.StockActual)
                .GreaterThanOrEqualTo(0).WithMessage("El stock actual no puede ser negativo.");
        }

        /// <summary>
        /// Verifica que el valor tenga como máximo dos decimales y que quepa en DECIMAL(10,2) de MySQL.
        /// </summary>
        private bool BeValidDecimal10_2(decimal value)
        {
            const decimal maximo = 99999999.99m; // 8 dígitos enteros + 2 decimales

            if (value < 0 || value > maximo)
                return false;

            return decimal.Round(value, 2) == value;
        }
    }
}