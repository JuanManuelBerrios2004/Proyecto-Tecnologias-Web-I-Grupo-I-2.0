using System;

namespace ControlInventario.Core.DTOs
{
    public class UsuarioDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = null!;

        public string Email { get; set; } = null!;

        public int RolId { get; set; }

        public string? RolNombre { get; set; }

        public bool Activo { get; set; }

        public DateTime? FechaCreacion { get; set; }
    }
}