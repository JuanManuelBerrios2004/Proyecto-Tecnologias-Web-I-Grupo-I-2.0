using ControlInventario.Core.Entities;

namespace ControlInventario.Core.Interfaces
{
    public interface ICategoriaRepository : IBaseRepository<Categoria>
    {
        Task<bool> ExisteCategoriaAsync(int id);
        Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
        Task<bool> TieneProductosAsociadosAsync(int id);
    }
}
