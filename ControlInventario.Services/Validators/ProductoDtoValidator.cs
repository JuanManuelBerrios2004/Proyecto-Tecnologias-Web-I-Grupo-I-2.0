using System.Linq.Expressions;
using ControlInventario.Services.Reglas;
using FluentValidation;

namespace ControlInventario.Services.Validators
{
    public abstract class ProductoDtoValidator<TDto> : AbstractValidator<TDto>
    {
        protected ProductoDtoValidator(
            Expression<Func<TDto, int>> categoriaId,
            Expression<Func<TDto, string?>> nombre,
            Expression<Func<TDto, decimal?>> precioCompra,
            Expression<Func<TDto, decimal?>> precioVenta)
        {
            var obtenerCompra = precioCompra.Compile();

            RuleFor(categoriaId)
                .GreaterThan(0).WithMessage("El ID de categoría es obligatorio y debe ser mayor que cero.");

            RuleFor(nombre)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .Must(valor => valor?.Trim().Length <= 100)
                .WithMessage("El nombre del producto no puede exceder los 100 caracteres.");

            RuleFor(precioCompra)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("El precio de compra es obligatorio.")
                .GreaterThanOrEqualTo(0m).WithMessage("El precio de compra no puede ser negativo.")
                .Must(TienePrecisionValida)
                .WithMessage("El precio de compra debe tener máximo dos decimales y ajustarse a DECIMAL(10,2).");

            RuleFor(precioVenta)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("El precio de venta es obligatorio.")
                .GreaterThanOrEqualTo(0m).WithMessage("El precio de venta no puede ser negativo.")
                .Must(TienePrecisionValida)
                .WithMessage("El precio de venta debe tener máximo dos decimales y ajustarse a DECIMAL(10,2).")
                .Must((dto, venta) => obtenerCompra(dto) is not decimal compra
                    || (venta is decimal precio && ReglasProducto.PrecioVentaValido(compra, precio)))
                .WithMessage(ReglasProducto.MensajePrecioVenta);
        }

        private static bool TienePrecisionValida(decimal? precio)
        {
            const decimal maximoDecimal10_2 = 99999999.99m;
            return precio is decimal valor && valor <= maximoDecimal10_2 && decimal.Round(valor, 2) == valor;
        }
    }
}
