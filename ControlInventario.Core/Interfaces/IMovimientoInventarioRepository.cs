using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Core.Interfaces
{
    /// <summary>
    /// Motivo por el que no se pudo registrar un movimiento de inventario.
    /// </summary>
    public enum MotivoRechazoMovimiento
    {
        Ninguno = 0,
        ProductoNoEncontrado = 1,
        ProductoInactivo = 2,
        UsuarioNoEncontrado = 3,
        StockInsuficiente = 4
    }

    /// <summary>
    /// Resultado de intentar registrar un movimiento de inventario.
    /// </summary>
    public class ResultadoRegistroMovimiento
    {
        public bool Registrado { get; private set; }

        public MotivoRechazoMovimiento Motivo { get; private set; }

        /// <summary>
        /// Stock del producto tras aplicar el movimiento (solo si se registró).
        /// </summary>
        public int StockResultante { get; private set; }

        public int StockDisponible { get; private set; }

        private ResultadoRegistroMovimiento(bool registrado,
            MotivoRechazoMovimiento motivo,
            int stockResultante,
            int stockDisponible)
        {
            Registrado = registrado;
            Motivo = motivo;
            StockResultante = stockResultante;
            StockDisponible = stockDisponible;
        }

        public static ResultadoRegistroMovimiento Exito(int stockResultante) =>
            new ResultadoRegistroMovimiento(true, MotivoRechazoMovimiento.Ninguno, stockResultante, stockResultante);

        public static ResultadoRegistroMovimiento Rechazado(MotivoRechazoMovimiento motivo, int stockDisponible = 0) =>
            new ResultadoRegistroMovimiento(false, motivo, 0, stockDisponible);
    }

    public interface IMovimientoInventarioRepository
    {
        Task<IEnumerable<MovimientoInventario>> GetAllMovimientosAsync();
        Task<MovimientoInventario?> GetMovimientoByIdAsync(int id);
        Task<IEnumerable<MovimientoInventario>> GetMovimientosByProductoAsync(int productoId);
        Task InsertMovimiento(MovimientoInventario movimiento);

        /// <summary>
        /// Registra un movimiento y actualiza el stock del producto dentro de una misma
        /// transacción: o se confirman ambos cambios, o se revierten los dos.
        /// </summary>
        Task<ResultadoRegistroMovimiento> RegistrarMovimientoAsync(MovimientoInventario movimiento);
    }
}