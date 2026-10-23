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

        public Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Usuario?> GetUsuarioByEmailAsync(string email)
        {
            throw new NotImplementedException();
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