using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Security;
using ControlInventario.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ControlInventario.Services.Services
{
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiraEn) Generar(Usuario usuario, string rol)
        {
            if (string.IsNullOrWhiteSpace(_settings.Key)
                || Encoding.UTF8.GetByteCount(_settings.Key) < 32
                || string.IsNullOrWhiteSpace(_settings.Issuer)
                || string.IsNullOrWhiteSpace(_settings.Audience)
                || _settings.ExpirationMinutes <= 0)
            {
                throw new InvalidOperationException("La configuración JWT no está completa.");
            }

            var expiraEn = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, rol)
            };

            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiraEn,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
        }
    }
}
