using System;

namespace ControlInventario.Core.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }

        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = null!;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public int StockActual { get; set; }

        public bool Activo { get; set; }

        public DateTime CreadoEn { get; set; }

        public DateTime ActualizadoEn { get; set; }

        // Dato desnormalizado para mostrar en el catálogo; no es una columna de `productos`.
        public string? CategoriaNombre { get; set; }
    }

    // Lectura de stock disponible (CU-05).
    public class ProductoStockDto
    {
        public int Id { get; set; }

        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = null!;

        public decimal PrecioVenta { get; set; }

        public int StockActual { get; set; }

        // Dato desnormalizado; no es una columna de `productos`.
        public string? CategoriaNombre { get; set; }
    }
}