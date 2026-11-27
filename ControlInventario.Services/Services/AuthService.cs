using System.Collections.Concurrent;
using System.Net;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Core.Security;
using ControlInventario.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ControlInventario.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        private static readonly ConcurrentDictionary<string, DateTime> RevokedTokens = new();

        public AuthService(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            PasswordHasher<Usuario> passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            if (loginDto == null
                || string.IsNullOrWhiteSpace(loginDto.Email)
                || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new BusinessException("Debe ingresar su correo y contraseña.");
            }

            var usuario = await _unitOfWork.UsuarioRepository.GetByCredentialsAsync(loginDto.Email);
            if (usuario == null
                || !usuario.Activo
                || !VerifyPassword(usuario, loginDto.Password, usuario.PasswordHash))
            {
                throw new BusinessException(
                    "Credenciales inválidas. Verifique su correo y contraseña.",
                    HttpStatusCode.Unauthorized);
            }

            var rol = RoleNames.Normalizar(usuario.Rol?.Nombre);
            if (rol == null)
            {
                throw new BusinessException(
                    "Credenciales inválidas. Verifique su correo y contraseña.",
                    HttpStatusCode.Unauthorized);
            }

            var (token, expiration) = _tokenService.Generar(usuario, rol);
            return new AuthResponseDto
            {
                Token = token,
                Expiration = expiration,
                Usuario = usuario.NombreCompleto,
                Rol = rol
            };
        }

        public Task LogoutAsync(string jti, DateTime expiration)
        {
            if (!string.IsNullOrWhiteSpace(jti))
            {
                RevokedTokens[jti] = expiration;
            }

            var now = DateTime.UtcNow;
            foreach (var item in RevokedTokens.Where(item => item.Value <= now).ToArray())
            {
                RevokedTokens.TryRemove(item.Key, out _);
            }

            return Task.CompletedTask;
        }

        public bool IsTokenRevoked(string jti)
        {
            return !string.IsNullOrWhiteSpace(jti)
                && RevokedTokens.TryGetValue(jti, out var expiration)
                && expiration > DateTime.UtcNow;
        }

        private bool VerifyPassword(Usuario usuario, string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            if (storedHash.StartsWith("$2a$", StringComparison.Ordinal)
                || storedHash.StartsWith("$2b$", StringComparison.Ordinal)
                || storedHash.StartsWith("$2y$", StringComparison.Ordinal))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return false;
                }
            }

            try
            {
                return _passwordHasher.VerifyHashedPassword(usuario, storedHash, password)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
