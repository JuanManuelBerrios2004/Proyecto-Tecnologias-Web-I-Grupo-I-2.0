
using System.Collections.Generic;
using System.Threading.Tasks;
using ControlInventario.Core.Entities;

namespace ControlInventario.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<Usuario>> GetAllUsuariosAsync();

        Task<Usuario?> GetUsuarioByIdAsync(int id);
        Task<bool> ExisteUsuarioAsync(int id);
        Task<Usuario?> GetUsuarioByEmailAsync(string email);

        Task<Usuario?> GetByCredentialsAsync(string email);

        Task InsertUsuario(Usuario usuario);

        Task UpdateUsuario(Usuario usuario);

        Task DeleteUsuario(Usuario usuario);
    }
}
