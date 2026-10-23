using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;
using ControlInventario.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly InventarioContext _inventarioContext;

        public CategoriaRepository(InventarioContext inventarioContext)
        {
            _inventarioContext = inventarioContext;
        }

        public Task<IEnumerable<Categoria>> GetAllCategoriasAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Categoria?> GetCategoriaByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task InsertCategoria(Categoria categoria)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCategoria(Categoria categoria)
        {
            throw new NotImplementedException();
        }

        public Task DeleteCategoria(Categoria categoria)
        {
            throw new NotImplementedException();
        }
    }
}