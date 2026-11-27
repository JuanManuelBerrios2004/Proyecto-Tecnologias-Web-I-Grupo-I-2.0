using System.IdentityModel.Tokens.Jwt;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

      
        /// CU-01: Iniciar Sesión y obtener token JWT
   
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(new ApiResponse<AuthResponseDto>(result));
        }

      
        /// CU-08: Cerrar Sesión invalidando el token actual
     
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            var expiration = long.TryParse(expClaim, out var expUnix)
                ? DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
                : DateTime.UtcNow.AddMinutes(60);

            if (!string.IsNullOrEmpty(jti))
            {
                await _authService.LogoutAsync(jti, expiration);
            }

            return Ok(new ApiResponse<string>("Sesión cerrada correctamente."));
        }
    }
}