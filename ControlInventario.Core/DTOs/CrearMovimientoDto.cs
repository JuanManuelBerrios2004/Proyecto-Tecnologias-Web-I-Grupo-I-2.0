using System;

namespace ControlInventario.Core.DTOs
{
    public class CrearMovimientoDto
    {
        public int ProductoId { get; set; }

        public int UsuarioId { get; set; }

        public string TipoMovimiento { get; set; } = null!;

        /// <summary>
        /// Motivo del movimiento. La columna `Motivo` es obligatoria y solo admite los
        /// valores definidos en `MovimientoInventario.MotivosPermitidos`.
        /// </summary>
        public string Motivo { get; set; } = null!;

        public int Cantidad { get; set; }

        public string? Observacion { get; set; }
    }

    /// <summary>
    /// Respuesta de un movimiento registrado (CU-02 / CU-05).
    /// </summary>
    public class MovimientoInventarioDto
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }

        public int UsuarioId { get; set; }

        public string TipoMovimiento { get; set; } = null!;

        public string Motivo { get; set; } = null!;

        public int Cantidad { get; set; }

        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; }

        /// <summary>
        /// Stock del producto después de aplicar el movimiento. No se persiste en la
        /// base de datos (columna `StockResultante` no existe en `movimientos_inventario`).
        /// </summary>
        public int StockResultante { get; set; }
    }
}