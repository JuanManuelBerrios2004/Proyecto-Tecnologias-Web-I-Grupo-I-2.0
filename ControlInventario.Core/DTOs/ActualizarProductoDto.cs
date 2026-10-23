using System;

namespace ControlInventario.Core.DTOs
{
    /// <summary>
    /// Datos de entrada para actualizar un producto (CU-02).
    /// El `Id` se recibe por la ruta del endpoint. No expone `Activo`, `CreadoEn` ni `ActualizadoEn`:
    /// la desactivación es lógica y se realiza con `DesactivarProductoAsync`.
    /// `StockActual` es nullable solo para detectar un envío explícito del cliente (RN-02):
    /// el stock existente nunca se sobrescribe desde el catálogo.
    /// </summary>
    public class ActualizarProductoDto
    {
        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = null!;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public int? StockActual { get; set; }
    }
}