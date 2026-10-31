using System;

namespace ControlInventario.Core.DTOs
{
    /// <summary>
    /// Motivos válidos para una salida de mercadería (CU-04, paso 4).
    /// Corresponden a los valores del ENUM `Motivo` de MySQL:
    ///   «Venta»                    -> VENTA
    ///   «Ajuste por Merma / Daño»  -> MERMA_DETERIORO
    /// </summary>
    public static class MotivosSalida
    {
        public const string Venta = "VENTA";
        public const string MermaDeterioro = "MERMA_DETERIORO";

        public static readonly string[] Permitidos = { Venta, MermaDeterioro };
    }

    /// <summary>
    /// Datos de entrada para registrar una salida de mercadería (CU-04).
    /// El tipo de movimiento no se envía: este caso de uso siempre registra una SALIDA.
    /// </summary>
    public class RegistrarSalidaDto
    {
        public int ProductoId { get; set; }

        // TODO (CU-01): cuando exista el login con JWT, el usuario saldrá del token y no del body.
        public int UsuarioId { get; set; }

        /// <summary>Cantidad de unidades a retirar.</summary>
        public int Cantidad { get; set; }

        /// <summary>VENTA o MERMA_DETERIORO.</summary>
        public string Motivo { get; set; } = null!;

        public string? Observacion { get; set; }
    }

    /// <summary>
    /// Datos que se muestran al seleccionar un producto en «Salida de Mercadería» (CU-04, paso 3):
    /// stock disponible actual y precio de venta sugerido.
    /// </summary>
    public class ProductoSalidaInfoDto
    {
        public int ProductoId { get; set; }

        public string Nombre { get; set; } = null!;

        public string? CategoriaNombre { get; set; }

        public int StockActual { get; set; }

        public decimal PrecioVentaSugerido { get; set; }
    }
}
