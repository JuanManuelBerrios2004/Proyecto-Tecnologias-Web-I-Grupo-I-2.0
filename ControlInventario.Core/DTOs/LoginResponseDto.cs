namespace ControlInventario.Core.DTOs
{
    public sealed class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiraEn { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Rol { get; set; } = null!;
    }
}
