namespace ControlInventario.Services.Reglas
{
    internal static class ReglasProducto
    {
        internal const string MensajePrecioVenta = "El precio de venta debe ser superior al costo de compra";

        internal static bool PrecioVentaValido(decimal precioCompra, decimal precioVenta)
        {
            return precioVenta > precioCompra;
        }
    }
}
