using ControlInventario.Core.Entities;

namespace ControlInventario.Services.Interfaces
{
    public interface ICategoriaService
    {
        Task<IEnumerable<Categoria>> GetCategoriasAsync();
        Task<Categoria> GetCategoriaByIdAsync(int id);
        Task<Categoria> InsertCategoria(Categoria categoria);
        Task<Categoria> UpdateCategoria(int id, Categoria cambios);
        Task DeleteCategoria(int id);
    }
}
