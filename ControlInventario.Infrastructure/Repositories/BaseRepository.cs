using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

namespace ControlInventario.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T>
        where T : class
    {
        protected readonly InventarioContext Context;

        protected readonly DbSet<T> Entity;

        public BaseRepository(InventarioContext context)
        {   
            Context = context;
            Entity = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Entity
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await Entity.FindAsync(id);
        }

        public virtual async Task AddAsync(T entity)
        {
            await Entity.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            Entity.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            Entity.Remove(entity);
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }
    }
}