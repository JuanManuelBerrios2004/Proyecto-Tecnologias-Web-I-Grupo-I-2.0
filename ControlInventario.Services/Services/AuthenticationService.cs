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
    public sealed class AuthenticationService : IAuthenticationService
    {
        private const string CredencialesInvalidas = "Credenciales inválidas.";
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AuthenticationService(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            PasswordHasher<Usuario> passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponseDto> AutenticarAsync(LoginDto login)
        {
            var usuario = await _unitOfWork.UsuarioRepository
                .GetUsuarioByEmailAsync(login.Email.Trim());

            if (usuario == null
                || !usuario.Activo
                || _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, login.Password)
                    is PasswordVerificationResult.Failed)
            {
                throw new BusinessException(CredencialesInvalidas, HttpStatusCode.Unauthorized);
            }

            var rol = RoleNames.Normalizar(usuario.Rol?.Nombre);
            if (rol == null)
            {
                throw new BusinessException(CredencialesInvalidas, HttpStatusCode.Unauthorized);
            }

            var (token, expiraEn) = _tokenService.Generar(usuario, rol);
            return new LoginResponseDto
            {
                Token = token,
                ExpiraEn = expiraEn,
                UsuarioId = usuario.Id,
                Nombre = usuario.NombreCompleto,
                Rol = rol
            };
        }
    }
}
