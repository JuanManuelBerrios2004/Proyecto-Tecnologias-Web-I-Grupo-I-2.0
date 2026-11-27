namespace ControlInventario.Core.Security
{
    public sealed class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int ExpirationMinutes { get; set; } = 60;
    }
}
