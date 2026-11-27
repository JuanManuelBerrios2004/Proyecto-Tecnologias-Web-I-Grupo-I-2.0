using System.Net;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;

namespace ControlInventario.Services.Services
{
    public class CategoriaService : ICategoriaService
    {
        private const string MensajeDuplicado = "Esta categoría ya se encuentra registrada";
        private const string MensajeConProductos = "No se puede eliminar la categoría porque tiene productos asociados";
        private readonly IUnitOfWork _unitOfWork;

        public CategoriaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<IEnumerable<Categoria>> GetCategoriasAsync()
        {
            return _unitOfWork.CategoriaRepository.GetAll();
        }

        public async Task<Categoria> GetCategoriaByIdAsync(int id)
        {
            return await _unitOfWork.CategoriaRepository.GetById(id)
                ?? throw new BusinessException("Categoría no encontrada.", HttpStatusCode.NotFound);
        }

        public async Task<Categoria> InsertCategoria(Categoria categoria)
        {
            categoria.Nombre = categoria.Nombre.Trim();
            await ComprobarNombreAsync(categoria.Nombre);

            await _unitOfWork.CategoriaRepository.Add(categoria);
            await GuardarNombreAsync();
            return categoria;
        }

        public async Task<Categoria> UpdateCategoria(int id, Categoria cambios)
        {
            var categoria = await GetCategoriaByIdAsync(id);
            var nombre = cambios.Nombre.Trim();
            await ComprobarNombreAsync(nombre, id);

            categoria.Nombre = nombre;
            categoria.Descripcion = cambios.Descripcion;
            await GuardarNombreAsync();
            return categoria;
        }

        public async Task DeleteCategoria(int id)
        {
            await GetCategoriaByIdAsync(id);
            if (await _unitOfWork.CategoriaRepository.TieneProductosAsociadosAsync(id))
            {
                throw new BusinessException(MensajeConProductos, HttpStatusCode.Conflict);
            }

            await _unitOfWork.CategoriaRepository.Delete(id);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1451, EsReferenciaDeProducto))
            {
                throw new BusinessException(MensajeConProductos, HttpStatusCode.Conflict);
            }
        }

        private async Task ComprobarNombreAsync(string nombre, int? excluirId = null)
        {
            if (await _unitOfWork.CategoriaRepository.ExisteNombreAsync(nombre, excluirId))
            {
                throw new BusinessException(MensajeDuplicado, HttpStatusCode.Conflict);
            }
        }

        private async Task GuardarNombreAsync()
        {
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1062, EsIndiceDelNombre))
            {
                throw new BusinessException(MensajeDuplicado, HttpStatusCode.Conflict);
            }
        }

        private static bool EsIndiceDelNombre(MySqlException error)
        {
            var mensaje = error.Message.TrimEnd();
            return mensaje.EndsWith("'nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'categorias.nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'IX_categorias_Nombre'", StringComparison.OrdinalIgnoreCase)
                || mensaje.EndsWith("'categorias.IX_categorias_Nombre'", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsReferenciaDeProducto(MySqlException error)
        {
            var mensaje = error.Message;
            return mensaje.Contains("`fk_productos_categorias`", StringComparison.OrdinalIgnoreCase)
                || (mensaje.Contains("`productos`", StringComparison.OrdinalIgnoreCase)
                    && mensaje.Contains("FOREIGN KEY (`categoria_id`) REFERENCES `categorias` (`id`)",
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
