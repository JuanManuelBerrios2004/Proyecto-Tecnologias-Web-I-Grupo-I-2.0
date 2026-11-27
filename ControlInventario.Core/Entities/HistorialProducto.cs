namespace ControlInventario.Core.Entities;

public class HistorialProducto : BaseEntity
{
    public int ProductoId { get; set; }

    public int UsuarioId { get; set; }

    public string CampoModificado { get; set; } = null!;

    public DateTime FechaModificacion { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
