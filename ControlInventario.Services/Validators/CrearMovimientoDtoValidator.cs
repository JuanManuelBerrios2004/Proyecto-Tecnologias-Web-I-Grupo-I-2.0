using FluentValidation;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ControlInventario.Services.Validators
{
    public class CrearMovimientoDtoValidator : AbstractValidator<CrearMovimientoDto>
    {
        public CrearMovimientoDtoValidator()
        {
            // Validación para ProductoId
            RuleFor(x => x.ProductoId)
                .GreaterThan(0).WithMessage("El ID de producto es obligatorio y debe ser mayor que cero.")
                .NotEmpty().WithMessage("El ID de producto no puede estar vacío.");

            // Validación para UsuarioId
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("El ID de usuario es obligatorio y debe ser mayor que cero.")
                .NotEmpty().WithMessage("El ID de usuario no puede estar vacío.");

            // Validación para TipoMovimiento (ENUM 'ENTRADA' / 'SALIDA')
            RuleFor(x => x.TipoMovimiento)
                .NotEmpty().WithMessage("El tipo de movimiento es obligatorio.")
                .Must(BeTipoMovimientoPermitido)
                .WithMessage("El tipo de movimiento debe ser ENTRADA o SALIDA.");

            // Validación para Motivo (ENUM con los cuatro motivos admitidos)
            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("El motivo del movimiento es obligatorio.")
                .Must(BeMotivoPermitido)
                .WithMessage("El motivo del movimiento no es válido.");

            // Validación para Cantidad
            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
        }

        /// <summary>
        /// El tipo de movimiento solo puede ser 'ENTRADA' o 'SALIDA'.
        /// </summary>
        private bool BeTipoMovimientoPermitido(string tipoMovimiento)
        {
            if (string.IsNullOrWhiteSpace(tipoMovimiento))
                return false;

            return MovimientoInventario.TiposMovimientoPermitidos
                .Contains(tipoMovimiento.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// El motivo debe pertenecer a los motivos admitidos por el ENUM de la tabla.
        /// </summary>
        private bool BeMotivoPermitido(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return false;

            return MovimientoInventario.MotivosPermitidos
                .Contains(motivo.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }
}