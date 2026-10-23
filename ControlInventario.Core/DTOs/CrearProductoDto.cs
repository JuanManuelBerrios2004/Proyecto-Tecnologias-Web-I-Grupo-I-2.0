using System;

namespace ControlInventario.Core.DTOs
{
    /// <summary>
    /// Datos de entrada para crear un producto (CU-02).
    /// No expone `Id`, `Activo`, `CreadoEn` ni `ActualizadoEn`: esos valores los controla el backend.
    /// `StockActual` es nullable solo para poder detectar un envío explícito del cliente
    /// (RN-02): el stock siempre nace en cero y solo se modifica con movimientos.
    /// </summary>
    public class CrearProductoDto
    {
        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = null!;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public int? StockActual { get; set; }
    }
}