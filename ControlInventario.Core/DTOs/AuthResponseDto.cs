namespace ControlInventario.Core.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}