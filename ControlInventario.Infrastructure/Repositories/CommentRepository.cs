using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly InventarioContext _inventarioContext;

        public CommentRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public async Task<IEnumerable<Comment>> GetAllCommentsAsync()
        {
            var comments = await _inventarioContext.Comments.ToListAsync();
            return comments;
        }

        public async Task<Comment> GetCommentByIdAsync(int id)
        {
            var comment = await _inventarioContext.Comments
                .FirstOrDefaultAsync(x => x.Id == id);

            return comment;
        }

        public async Task InsertComment(Comment comment)
        {
            _inventarioContext.Comments.Add(comment);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task UpdateComment(Comment comment)
        {
            _inventarioContext.Comments.Update(comment);
            await _inventarioContext.SaveChangesAsync();
        }

        public async Task DeleteComment(Comment comment)
        {
            _inventarioContext.Comments.Remove(comment);
            await _inventarioContext.SaveChangesAsync();
        }
    }
}