using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<IEnumerable<Usuario>> GetAllUsuariosAsync();
        Task<Usuario?> GetUsuarioByIdAsync(int id);
        Task<Usuario?> GetUsuarioByEmailAsync(string email);
        Task InsertUsuario(Usuario usuario);
        Task UpdateUsuario(Usuario usuario);
        Task DeleteUsuario(Usuario usuario);
    }
}