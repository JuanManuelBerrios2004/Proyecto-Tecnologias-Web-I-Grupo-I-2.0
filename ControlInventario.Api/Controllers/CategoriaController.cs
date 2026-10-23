using Microsoft.AspNetCore.Mvc;
using ControlInventario.Core.Interfaces;

namespace ControlInventario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriaController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        // Endpoints pendientes de implementación.
    }
}