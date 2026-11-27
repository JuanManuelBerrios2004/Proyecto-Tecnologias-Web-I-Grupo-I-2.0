using System.Net;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Exceptions;
using ControlInventario.Core.Interfaces;
using ControlInventario.Services.Interfaces;

namespace ControlInventario.Services.Services
{
    public class ProductoService : IProductoService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductoService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<IEnumerable<Producto>> GetProductosAsync(string? nombre, int? categoriaId, bool? soloActivos)
        {
            return _unitOfWork.ProductoRepository.GetProductosAsync(nombre, categoriaId, soloActivos);
        }

        public Task<Producto?> GetProductoByIdAsync(int id)
        {
            return _unitOfWork.ProductoRepository.GetProductoByIdAsync(id);
        }

        public Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre, int? categoriaId)
        {
            return _unitOfWork.ProductoRepository.GetStockProductosAsync(nombre, categoriaId);
        }

        public async Task<Producto> InsertProducto(Producto producto)
        {
            await ValidarCategoriaAsync(producto.CategoriaId);
            await _unitOfWork.ProductoRepository.Add(producto);
            await GuardarProductoAsync();

            return await _unitOfWork.ProductoRepository.GetProductoByIdAsync(producto.Id) ?? producto;
        }

        public async Task<Producto> UpdateProducto(Producto cambios)
        {
            var existente = await _unitOfWork.ProductoRepository.GetProductoByIdAsync(cambios.Id)
                ?? throw new BusinessException("Producto no encontrado.", HttpStatusCode.NotFound);

            await ValidarCategoriaAsync(cambios.CategoriaId);
            _unitOfWork.ProductoRepository.UpdateProducto(existente, cambios);
            await GuardarProductoAsync();

            return await _unitOfWork.ProductoRepository.GetProductoByIdAsync(existente.Id) ?? existente;
        }

        public async Task<bool> DesactivarProductoAsync(int id)
        {
            if (!await _unitOfWork.ProductoRepository.DesactivarProductoAsync(id))
            {
                return false;
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private async Task ValidarCategoriaAsync(int categoriaId)
        {
            if (!await _unitOfWork.CategoriaRepository.ExisteCategoriaAsync(categoriaId))
            {
                throw new BusinessException("La categoría indicada no existe.");
            }
        }

        private async Task GuardarProductoAsync()
        {
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1062))
            {
                throw new BusinessException("Ya existe un producto registrado con ese nombre.", HttpStatusCode.Conflict);
            }
            catch (DbUpdateException ex) when (ErrorMySql.Coincide(ex, 1452))
            {
                throw new BusinessException("La categoría indicada no existe.");
            }
        }
    }
}
