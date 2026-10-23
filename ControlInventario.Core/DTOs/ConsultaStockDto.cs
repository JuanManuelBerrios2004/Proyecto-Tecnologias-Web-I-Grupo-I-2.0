using System.Collections.Generic;

namespace ControlInventario.Core.DTOs
{
	public class ConsultaStockQueryDto
	{
		public string? Nombre { get; set; }

		public int? CategoriaId { get; set; }
	}

	public class ConsultaStockResponseDto
	{
		public string Mensaje { get; set; } = null!;

		public int Total { get; set; }

		public IEnumerable<ProductoStockDto> Datos { get; set; } = new List<ProductoStockDto>();
	}
}