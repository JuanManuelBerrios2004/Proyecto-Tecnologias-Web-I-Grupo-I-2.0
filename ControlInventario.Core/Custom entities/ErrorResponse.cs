namespace ControlInventario.Core.CustomEntities
{
	/// <summary>
	/// Formato único de respuesta cuando una petición falla (reglas de negocio,
	/// validaciones o errores no controlados).
	/// </summary>
	public class ErrorResponse
	{
		public int Status { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public object? Errors { get; set; }
		public string? TraceId { get; set; }
	}
}