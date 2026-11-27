using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;

namespace ControlInventario.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T>
        where T : BaseEntity
    {
        protected readonly InventarioContext Context;

        protected readonly DbSet<T> Entity;

        public BaseRepository(InventarioContext context)
        {
            Context = context;
            Entity = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAll()
        {
            return await Entity
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<T?> GetById(int id)
        {
            return await Entity.FindAsync(id);
        }

        public virtual async Task Add(T entity)
        {
            await Entity.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            Entity.Update(entity);
        }

        public virtual async Task Delete(int id)
        {
            var entity = await GetById(id);
            if (entity != null)
            {
                Entity.Remove(entity);
            }
        }
    }
}