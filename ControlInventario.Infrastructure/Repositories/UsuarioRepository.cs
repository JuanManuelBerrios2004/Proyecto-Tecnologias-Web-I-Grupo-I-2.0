using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

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
            return await _inventarioContext.Set<Usuario>()
                .Include(u => u.Rol)
                .ToListAsync();
        }

        public async Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            return await _inventarioContext.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario?> GetUsuarioByEmailAsync(string email)
        {
            return await _inventarioContext.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> GetByCredentialsAsync(string email)
        {
            return await _inventarioContext.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task InsertUsuario(Usuario usuario)
        {
            await _inventarioContext.Set<Usuario>().AddAsync(usuario);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task UpdateUsuario(Usuario usuario)
        {
            _inventarioContext.Set<Usuario>().Update(usuario);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task DeleteUsuario(Usuario usuario)
        {
            _inventarioContext.Set<Usuario>().Remove(usuario);
            await _inventarioContext.SaveChangesAsync();
        }
    }
}