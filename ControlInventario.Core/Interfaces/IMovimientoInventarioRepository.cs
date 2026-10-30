using ControlInventario.Core.Entities;

namespace ControlInventario.Core.Interfaces
{
    public enum MotivoRechazoMovimiento
    {
        Ninguno = 0,
        ProductoNoEncontrado = 1,
        ProductoInactivo = 2,
        UsuarioNoEncontrado = 3,
        StockInsuficiente = 4
    }

    public class ResultadoRegistroMovimiento
    {
        public bool Registrado { get; private set; }

        public MotivoRechazoMovimiento Motivo { get; private set; }

        public int StockResultante { get; private set; }

        public int StockDisponible { get; private set; }

        private ResultadoRegistroMovimiento(
            bool registrado,
            MotivoRechazoMovimiento motivo,
            int stockResultante,
            int stockDisponible)
        {
            Registrado = registrado;
            Motivo = motivo;
            StockResultante = stockResultante;
            StockDisponible = stockDisponible;
        }

        public static ResultadoRegistroMovimiento Exito(
            int stockResultante)
        {
            return new ResultadoRegistroMovimiento(
                true,
                MotivoRechazoMovimiento.Ninguno,
                stockResultante,
                stockResultante);
        }

        public static ResultadoRegistroMovimiento Rechazado(
            MotivoRechazoMovimiento motivo,
            int stockDisponible = 0)
        {
            return new ResultadoRegistroMovimiento(
                false,
                motivo,
                0,
                stockDisponible);
        }
    }

    public interface IMovimientoInventarioRepository
        : IBaseRepository<MovimientoInventario>
    {
        Task<IEnumerable<MovimientoInventario>>
            GetAllMovimientosAsync();

        Task<MovimientoInventario?>
            GetMovimientoByIdAsync(int id);

        Task<IEnumerable<MovimientoInventario>>
            GetMovimientosByProductoAsync(int productoId);

        Task<ResultadoRegistroMovimiento>
            RegistrarMovimientoAsync(
                MovimientoInventario movimiento);
    }
}