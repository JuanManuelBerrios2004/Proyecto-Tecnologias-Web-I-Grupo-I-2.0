using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.Interfaces
{
    public interface ICategoriaRepository
    {
        Task<IEnumerable<Categoria>> GetAllCategoriasAsync();
        Task<Categoria?> GetCategoriaByIdAsync(int id);
        Task InsertCategoria(Categoria categoria);
        Task UpdateCategoria(Categoria categoria);
        Task DeleteCategoria(Categoria categoria);
    }
}