using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly LoginDtoValidator _loginValidator;

        public AuthController(
            IAuthenticationService authenticationService,
            LoginDtoValidator loginValidator)
        {
            _authenticationService = authenticationService;
            _loginValidator = loginValidator;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            await _loginValidator.ValidateAndThrowAsync(login);
            var response = await _authenticationService.AutenticarAsync(login);
            return Ok(new ApiResponse<LoginResponseDto>(response));
        }
    }
}