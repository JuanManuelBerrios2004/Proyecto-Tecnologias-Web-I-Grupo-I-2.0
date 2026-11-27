namespace ControlInventario.Core.DTOs
{
    public class ActualizarProductoDto
    {
        public int CategoriaId { get; set; }

        public string? Nombre { get; set; }

        public decimal? PrecioCompra { get; set; }

        public decimal? PrecioVenta { get; set; }
    }
}
