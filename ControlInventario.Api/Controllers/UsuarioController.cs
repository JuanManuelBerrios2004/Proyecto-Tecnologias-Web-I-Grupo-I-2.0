using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.Interfaces;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioController(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        // Endpoints pendientes de implementación.
    }
}