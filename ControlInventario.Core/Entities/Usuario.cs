using System;
using System.Collections.Generic;

namespace ControlInventario.Core.Entities;

public partial class Usuario
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Rol? Rol { get; set; } = null!;

    public virtual ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}