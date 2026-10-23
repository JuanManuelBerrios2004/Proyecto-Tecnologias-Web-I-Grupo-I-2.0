using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlInventario.Core.Entities;

public partial class MovimientoInventario
{
    // Valores admitidos por el ENUM `TipoMovimiento` de MySQL.
    public static readonly string[] TiposMovimientoPermitidos = { "ENTRADA", "SALIDA" };

    // Valores admitidos por el ENUM `Motivo` de MySQL.
    public static readonly string[] MotivosPermitidos =
    {
        "COMPRA_PROVEEDOR", "VENTA", "MERMA_DETERIORO", "AJUSTE"
    };

    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int UsuarioId { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public string Motivo { get; set; } = null!;

    public int Cantidad { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    // La tabla `movimientos_inventario` no registra el stock resultante.
    [NotMapped]
    public int StockResultante { get; set; }

    public virtual Producto? Producto { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; } = null!;
}