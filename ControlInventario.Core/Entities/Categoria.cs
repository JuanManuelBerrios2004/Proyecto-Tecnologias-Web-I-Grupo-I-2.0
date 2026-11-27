using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlInventario.Core.Entities;

public partial class Categoria : BaseEntity
{
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime CreadoEn { get; set; }

    // La tabla `categorias` no tiene columna de estado.
    [NotMapped]
    public bool Activo { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}