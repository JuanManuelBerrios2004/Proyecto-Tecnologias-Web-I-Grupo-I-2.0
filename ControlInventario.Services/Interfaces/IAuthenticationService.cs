using ControlInventario.Core.DTOs;

namespace ControlInventario.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResponseDto> AutenticarAsync(LoginDto login);
    }
}
