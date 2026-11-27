using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly InventarioContext _inventarioContext;

        public UsuarioRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public Task<IEnumerable<Usuario>> GetAllUsuariosAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExisteUsuarioAsync(int id)
        {
            return _inventarioContext.Usuarios.AnyAsync(usuario => usuario.Id == id);
        }

        public Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            return _inventarioContext.Usuarios
                .Include(usuario => usuario.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(usuario => usuario.Id == id);
        }

        public Task<Usuario?> GetUsuarioByEmailAsync(string email)
        {
            var normalizado = email.Trim().ToLower();
            return _inventarioContext.Usuarios
                .Include(usuario => usuario.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(usuario => usuario.Email.ToLower() == normalizado);
        }

        public Task InsertUsuario(Usuario usuario)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUsuario(Usuario usuario)
        {
            throw new NotImplementedException();
        }

        public Task DeleteUsuario(Usuario usuario)
        {
            throw new NotImplementedException();
        }
    }
}