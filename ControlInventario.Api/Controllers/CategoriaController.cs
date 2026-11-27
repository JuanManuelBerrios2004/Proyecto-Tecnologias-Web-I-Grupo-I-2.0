using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ControlInventario.Api.Response;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Services.Interfaces;
using ControlInventario.Services.Validators;
using ControlInventario.Core.Security;

namespace ControlInventario.Api.Controllers
{
    // Fase 6: exigir autenticación en el controlador y restringir las escrituras a Dueño / Administrador.
    [Route("api/categories")]
    [ApiController]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly IMapper _mapper;
        private readonly CrearCategoriaDtoValidator _crearValidator;
        private readonly ActualizarCategoriaDtoValidator _actualizarValidator;

        public CategoriaController(
            ICategoriaService categoriaService,
            IMapper mapper,
            CrearCategoriaDtoValidator crearValidator,
            ActualizarCategoriaDtoValidator actualizarValidator)
        {
            _categoriaService = categoriaService;
            _mapper = mapper;
            _crearValidator = crearValidator;
            _actualizarValidator = actualizarValidator;
        }

        [HttpGet]
        [Authorize(Policy = RoleNames.UsuarioAutenticadoPolicy)]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _categoriaService.GetCategoriasAsync();
            var data = _mapper.Map<IEnumerable<CategoriaDto>>(categorias);
            return Ok(new ApiResponse<IEnumerable<CategoriaDto>>(data));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = RoleNames.UsuarioAutenticadoPolicy)]
        public async Task<IActionResult> GetCategoriaById(int id)
        {
            var categoria = await _categoriaService.GetCategoriaByIdAsync(id);
            var data = _mapper.Map<CategoriaDto>(categoria);
            return Ok(new ApiResponse<CategoriaDto>(data));
        }

        [HttpPost]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> InsertCategoria([FromBody] CrearCategoriaDto dto)
        {
            await _crearValidator.ValidateAndThrowAsync(dto);
            var categoria = _mapper.Map<Categoria>(dto);
            var creada = await _categoriaService.InsertCategoria(categoria);
            return CreatedAtAction(nameof(GetCategoriaById), new { id = creada.Id },
                _mapper.Map<CategoriaDto>(creada));
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> UpdateCategoria(int id, [FromBody] ActualizarCategoriaDto dto)
        {
            await _actualizarValidator.ValidateAndThrowAsync(dto);
            var cambios = _mapper.Map<Categoria>(dto);
            var actualizada = await _categoriaService.UpdateCategoria(id, cambios);
            return Ok(_mapper.Map<CategoriaDto>(actualizada));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = RoleNames.AdministradorPolicy)]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            await _categoriaService.DeleteCategoria(id);
            return NoContent();
        }
    }
}
