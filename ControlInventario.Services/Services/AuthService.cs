
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using ControlInventario.Core.DTOs;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ControlInventario.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        // CU-08: Tokens revocados hasta su expiracion.
        // Almacenamiento temporal en memoria.
        private static readonly ConcurrentDictionary<string, DateTime>
            _revokedTokens = new();

        public AuthService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        // CU-01: INICIAR SESION
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            if (loginDto == null ||
                string.IsNullOrWhiteSpace(loginDto.Email) ||
                string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new BusinessException(
                    "Debe ingresar su correo y contraseña.");
            }

            var usuario = await _unitOfWork.UsuarioRepository
                .GetByCredentialsAsync(loginDto.Email);

            if (usuario == null ||
                !usuario.Activo ||
                !VerifyPassword(
                    loginDto.Password,
                    usuario.PasswordHash))
            {
                throw new BusinessException(
                    "Credenciales inválidas. Verifique su correo y contraseña.",
                    401);
            }

            var expiration = DateTime.UtcNow.AddMinutes(60);

            var rol = usuario.Rol?.Nombre ?? "Empleado";

            var token = GenerateJwtToken(
                usuario.NombreCompleto,
                usuario.Email,
                rol,
                expiration);

            return new AuthResponseDto
            {
                Token = token,
                Expiration = expiration,
                Usuario = usuario.NombreCompleto,
                Rol = rol
            };
        }

        // CU-08: CERRAR SESION
        public Task LogoutAsync(string jti, DateTime expiration)
        {
            if (!string.IsNullOrWhiteSpace(jti))
            {
                _revokedTokens[jti] = expiration;
            }

            // Eliminar registros de tokens expirados.
            var now = DateTime.UtcNow;

            foreach (var item in _revokedTokens
                .Where(x => x.Value <= now)
                .ToList())
            {
                _revokedTokens.TryRemove(
                    item.Key,
                    out _);
            }

            return Task.CompletedTask;
        }

        // CONSULTAR SI UN TOKEN FUE REVOCADO
        public bool IsTokenRevoked(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                return false;
            }

            return _revokedTokens.TryGetValue(
                jti,
                out var expiration)
                && expiration > DateTime.UtcNow;
        }

        // GENERAR TOKEN JWT
        private string GenerateJwtToken(
            string nombre,
            string email,
            string rol,
            DateTime expiration)
        {
            var secretKey =
                _configuration["Authentication:SecretKey"];

            var issuer =
                _configuration["Authentication:Issuer"];

            var audience =
                _configuration["Authentication:Audience"];

            if (string.IsNullOrWhiteSpace(secretKey) ||
                Encoding.UTF8.GetByteCount(secretKey) < 32)
            {
                throw new InvalidOperationException(
                    "La clave JWT no está configurada correctamente.");
            }

            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "Debe configurar Authentication:Issuer y Authentication:Audience.");
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    email),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    ClaimTypes.Name,
                    nombre),

                new Claim(
                    ClaimTypes.Email,
                    email),

                new Claim(
                    ClaimTypes.Role,
                    rol)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiration,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // VALIDAR CONTRASEÑA
        private static bool VerifyPassword(
            string plainPassword,
            string storedPasswordHash)
        {
            if (string.IsNullOrEmpty(plainPassword) ||
                string.IsNullOrEmpty(storedPasswordHash))
            {
                return false;
            }

            // Las contraseñas BCrypt utilizan este formato.
            // No aceptar una comparación en texto plano
            // porque PasswordHash debe contener un hash real.
            if (!storedPasswordHash.StartsWith("$2a$") &&
                !storedPasswordHash.StartsWith("$2b$") &&
                !storedPasswordHash.StartsWith("$2y$"))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(
                    plainPassword,
                    storedPasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
