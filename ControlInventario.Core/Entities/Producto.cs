using System.ComponentModel.DataAnnotations.Schema;

namespace ControlInventario.Core.Entities;

public partial class Producto
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    [NotMapped]
    public string? Descripcion { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public int StockActual { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    // No existe esta columna en la BD actual.
    [NotMapped]
    public DateTime ActualizadoEn { get; set; }

    public virtual Categoria? Categoria
    {
        get;
        set;
    } = null!;

    public virtual ICollection<MovimientoInventario>
        Movimientos
    {
        get;
        set;
    } = new List<MovimientoInventario>();
}