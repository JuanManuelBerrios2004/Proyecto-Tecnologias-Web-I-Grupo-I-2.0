using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.DTOs
{
    public class RegistrarEntradaDto
    {
        public int ProductoId { get; set; }

        public int UsuarioId { get; set; }

        public int Cantidad { get; set; }

        public string? Observacion { get; set; }
    }
}