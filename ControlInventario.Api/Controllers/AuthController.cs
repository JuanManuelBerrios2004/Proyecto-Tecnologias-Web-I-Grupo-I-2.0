using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.Interfaces;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthController(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        // Endpoints pendientes de implementación.
    }
}