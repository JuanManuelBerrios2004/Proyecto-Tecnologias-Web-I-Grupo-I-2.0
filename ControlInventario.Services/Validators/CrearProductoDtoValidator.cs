using FluentValidation;
using ControlInventario.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Services.Validators
{
    public class CrearProductoDtoValidator : AbstractValidator<CrearProductoDto>
    {
        public CrearProductoDtoValidator()
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
                .Must(BeValidDecimal10_2).WithMessage("El precio de venta debe tener máximo dos decimales y ajustarse a DECIMAL(10,2).")
                .GreaterThan(x => x.PrecioCompra).WithMessage("El precio de venta debe ser superior al costo de compra");

            // Validación para StockActual (RN-02): el stock no se envía desde el catálogo.
            // El producto nace con stock cero y solo cambia mediante movimientos.
            RuleFor(x => x.StockActual)
                .Must(x => !x.HasValue)
                .WithMessage("El stock no se puede enviar al crear un producto; se registra mediante movimientos de inventario.");
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