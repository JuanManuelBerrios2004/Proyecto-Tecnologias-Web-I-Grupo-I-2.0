using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task LogoutAsync(string jti, DateTime expiration);
        bool IsTokenRevoked(string jti);
    }
}