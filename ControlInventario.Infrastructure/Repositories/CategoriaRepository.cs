using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

namespace ControlInventario.Infrastructure.Repositories
{
    public class CategoriaRepository : BaseRepository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(InventarioContext inventarioContext) : base(inventarioContext)
        {
        }

        public Task<bool> ExisteCategoriaAsync(int id)
        {
            return Entity.AnyAsync(categoria => categoria.Id == id);
        }

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
        {
            // La igualdad utiliza la collation de la columna en MySQL.
            var query = Entity.Where(categoria => categoria.Nombre == nombre);
            if (excluirId is int id)
            {
                query = query.Where(categoria => categoria.Id != id);
            }

            return query.AnyAsync();
        }

        public Task<bool> TieneProductosAsociadosAsync(int id)
        {
            return Context.Productos.AnyAsync(producto => producto.CategoriaId == id);
        }
    }
}
