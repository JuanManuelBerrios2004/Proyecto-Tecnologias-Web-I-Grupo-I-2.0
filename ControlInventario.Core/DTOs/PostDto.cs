using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.DTOs
{
    public class PostDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Date { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? Imagen { get; set; }
    }
}
