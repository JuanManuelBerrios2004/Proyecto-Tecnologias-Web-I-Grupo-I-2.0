using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlInventario.Core.Entities;

public partial class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    // La tabla `roles` no tiene columna de descripción.
    [NotMapped]
    public string? Descripcion { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}