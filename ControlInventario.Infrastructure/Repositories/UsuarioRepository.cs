using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly InventarioContext _inventarioContext;

        public UsuarioRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public async Task<IEnumerable<Usuario>> GetAllUsuariosAsync()
        {
            return await _inventarioContext.Usuarios
                .Include(usuario => usuario.Rol)
                .ToListAsync();
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

        public Task<Usuario?> GetByCredentialsAsync(string email)
        {
            var normalizado = email.Trim().ToLower();
            return _inventarioContext.Usuarios
                .Include(usuario => usuario.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(usuario => usuario.Email.ToLower() == normalizado);
        }

        public async Task InsertUsuario(Usuario usuario)
        {
            await _inventarioContext.Usuarios.AddAsync(usuario);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task UpdateUsuario(Usuario usuario)
        {
            _inventarioContext.Usuarios.Update(usuario);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task DeleteUsuario(Usuario usuario)
        {
            _inventarioContext.Usuarios.Remove(usuario);
            await _inventarioContext.SaveChangesAsync();
        }
    }
}
