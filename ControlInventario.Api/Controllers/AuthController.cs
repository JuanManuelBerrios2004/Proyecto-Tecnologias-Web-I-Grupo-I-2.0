using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly LoginDtoValidator _loginValidator;

        public AuthController(
            IAuthService authService,
            LoginDtoValidator loginValidator)
        {
            _authService = authService;
            _loginValidator = loginValidator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            await _loginValidator.ValidateAndThrowAsync(loginDto);
            var result = await _authService.LoginAsync(loginDto);
            return Ok(new ApiResponse<AuthResponseDto>(result));
        }

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
