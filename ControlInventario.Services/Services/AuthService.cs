using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        // Almacena tokens revocados (CU-08) hasta su fecha de expiración
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        // CU-01: Iniciar Sesión
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var usuario = await _unitOfWork.UsuarioRepository.GetByCredentialsAsync(loginDto.Email);

            if (usuario == null || !VerifyPassword(loginDto.Password, usuario.Password))
            {
                throw new BusinessException("Credenciales inválidas. Verifique su correo y contraseña.");
            }

            var expiration = DateTime.UtcNow.AddMinutes(60);
            var token = GenerateJwtToken(usuario.Nombre, usuario.Email, usuario.Rol?.Nombre ?? "Empleado", expiration);

            return new AuthResponseDto
            {
                Token = token,
                Expiration = expiration,
                Usuario = usuario.Nombre,
                Rol = usuario.Rol?.Nombre ?? "Empleado"
            };
        }

        // CU-08: Cerrar Sesión (Revocación de JWT)
        public Task LogoutAsync(string jti, DateTime expiration)
        {
            if (!string.IsNullOrEmpty(jti))
            {
                _revokedTokens[jti] = expiration;
            }

            // Limpieza de tokens ya expirados
            var now = DateTime.UtcNow;
            foreach (var item in _revokedTokens.Where(x => x.Value < now).ToList())
            {
                _revokedTokens.TryRemove(item.Key, out _);
            }

            return Task.CompletedTask;
        }

        public bool IsTokenRevoked(string jti) => _revokedTokens.ContainsKey(jti);

        private string GenerateJwtToken(string nombre, string email, string rol, DateTime expiration)
        {
            var secretKey = _configuration["Authentication:SecretKey"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Issuer"],
                audience: _configuration["Authentication:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool VerifyPassword(string plainPassword, string storedPassword)
        {
            // Soporta texto plano inicial o hash BCrypt si lo habilitas
            return plainPassword == storedPassword;
        }
    }
}