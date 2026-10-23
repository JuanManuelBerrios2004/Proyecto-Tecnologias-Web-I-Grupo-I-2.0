using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {   
        private readonly InventarioContext _inventarioContext;

        public PostRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }
                
        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            var posts = await _inventarioContext.Posts.ToListAsync();
            return posts;
        }

        public async Task<Post> GetPostByIdAsync(int id)
        {
            var post = await _inventarioContext.Posts.
                FirstOrDefaultAsync(x => x.Id == id);
            return post;
        }

        public async Task InsertPost(Post post)
        {
            _inventarioContext.Posts.Add(post);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task UpdatePost(Post post)
        {
            _inventarioContext.Posts.Update(post);
            await _inventarioContext.SaveChangesAsync();
        }
        public async Task DeletePost(Post post)
        {
            _inventarioContext.Posts.Remove(post);
            await _inventarioContext.SaveChangesAsync();
        }
    }
}
