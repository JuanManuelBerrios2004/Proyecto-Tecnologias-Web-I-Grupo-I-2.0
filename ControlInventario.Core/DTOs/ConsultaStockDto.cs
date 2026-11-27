using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlInventario.Core.DTOs
{
	public class ConsultaStockQueryDto
	{
		public string? Nombre { get; set; }

		public int? CategoriaId { get; set; }

		public bool? SoloActivos { get; set; }

		// Conserva el valor vacío para rechazar explícitamente este parámetro.
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public string? StockMinimo { get; set; }
	}

	public class ConsultaStockResponseDto
	{
		public string Mensaje { get; set; } = null!;

		public int Total { get; set; }

		public IEnumerable<ProductoStockDto> Datos { get; set; } = new List<ProductoStockDto>();
	}
}