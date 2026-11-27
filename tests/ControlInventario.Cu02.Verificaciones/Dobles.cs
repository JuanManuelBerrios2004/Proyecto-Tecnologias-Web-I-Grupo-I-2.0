using System.Data;
using ControlInventario.Core.DTOs;
using ControlInventario.Core.Entities;
using ControlInventario.Core.Interfaces;

namespace ControlInventario.Cu02.Verificaciones;

internal sealed class UnidadEnMemoria : IUnitOfWork
{
    public static readonly DateTime FechaGenerada = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private readonly UsuariosEnMemoria _usuarios = new();
    private readonly CategoriasEnMemoria _categorias;
    private readonly HistorialEnMemoria _historial;

    public UnidadEnMemoria()
    {
        Productos = new ProductosEnMemoria(this);
        _categorias = new CategoriasEnMemoria(Productos);
        _historial = new HistorialEnMemoria(this);
    }

    public ProductosEnMemoria Productos { get; }
    public List<HistorialProducto> Historial { get; } = [];
    public int Guardados { get; private set; }
    public int Confirmaciones { get; private set; }
    public int Reversiones { get; private set; }
    public bool EnTransaccion { get; private set; }
    public Exception? FalloAlGuardar { get; set; }
    public Exception? FalloAlBloquear { get; set; }
    public IProductoRepository ProductoRepository => Productos;
    public ICategoriaRepository CategoriaRepository => _categorias;
    public IUsuarioRepository UsuarioRepository => _usuarios;
    public IHistorialProductoRepository HistorialProductoRepository => _historial;
    public IMovimientoInventarioRepository MovimientoInventarioRepository => throw new NotSupportedException();

    public void SaveChanges() => throw new NotSupportedException();

    public Task SaveChangesAsync()
    {
        Guardados++;
        if (FalloAlGuardar != null)
        {
            return Task.FromException(FalloAlGuardar);
        }

        Productos.Confirmar();
        foreach (var fila in _historial.Pendientes)
        {
            fila.FechaModificacion = FechaGenerada;
            Historial.Add(fila);
        }

        _historial.Pendientes.Clear();
        return Task.CompletedTask;
    }

    public Task BeginTransactionAsync()
    {
        if (EnTransaccion)
            throw new InvalidOperationException("El doble no permite transacciones anidadas.");

        EnTransaccion = true;
        return Task.CompletedTask;
    }

    public async Task CommitAsync()
    {
        if (!EnTransaccion)
            throw new InvalidOperationException("Falta una transacción para confirmar.");

        await SaveChangesAsync();
        Confirmaciones++;
        EnTransaccion = false;
    }

    public Task RollbackAsync()
    {
        if (EnTransaccion)
        {
            EnTransaccion = false;
            Reversiones++;
            Productos.Descartar();
            _historial.Pendientes.Clear();
        }

        return Task.CompletedTask;
    }

    public IDbConnection? GetDbConnection() => throw new NotSupportedException();
    public IDbTransaction? GetDbTransaction() => throw new NotSupportedException();
    public void Dispose() { }
}

internal sealed class ProductosEnMemoria(UnidadEnMemoria unidad) : IProductoRepository
{
    private Producto? _alta;
    private Producto? _edicion;
    public List<Producto> Filas { get; } = [];

    public Task<Producto?> GetProductoByIdAsync(int id)
    {
        return Task.FromResult(Filas.FirstOrDefault(p => p.Id == id));
    }

    public Task<Producto?> ObtenerParaModificarAsync(int id)
    {
        if (!unidad.EnTransaccion)
            throw new InvalidOperationException("Se solicitó una lectura para modificar fuera de la transacción.");

        if (unidad.FalloAlBloquear != null)
            return Task.FromException<Producto?>(unidad.FalloAlBloquear);

        var fila = Filas.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(fila == null ? null : Copiar(fila));
    }

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
    {
        return Task.FromResult(Filas.Any(p => p.Id != excluirId
            && string.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IEnumerable<Producto>> GetAllProductosAsync() => GetProductosAsync();

    public Task<IEnumerable<Producto>> GetProductosAsync(string? nombre = null, int? categoriaId = null, bool? soloActivos = null)
    {
        return Task.FromResult<IEnumerable<Producto>>(Filas.Where(p =>
            (soloActivos != true || p.Activo) &&
            (categoriaId == null || p.CategoriaId == categoriaId) &&
            (nombre == null || p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))).ToArray());
    }

    public Task<IEnumerable<ProductoStockDto>> GetStockProductosAsync(string? nombre = null, int? categoriaId = null)
    {
        return Task.FromResult<IEnumerable<ProductoStockDto>>(Filas.Where(p =>
            p.Activo && (categoriaId == null || p.CategoriaId == categoriaId) &&
            (nombre == null || p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new ProductoStockDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria?.Nombre,
                StockActual = p.StockActual,
                PrecioVenta = p.PrecioVenta
            }).ToArray());
    }

    public Task Add(Producto producto)
    {
        _alta = producto;
        return Task.CompletedTask;
    }

    public void UpdateProducto(Producto existente, Producto cambios)
    {
        existente.CategoriaId = cambios.CategoriaId;
        existente.Nombre = cambios.Nombre;
        existente.PrecioCompra = cambios.PrecioCompra;
        existente.PrecioVenta = cambios.PrecioVenta;
        _edicion = existente;
    }

    public void DesactivarProducto(Producto producto)
    {
        producto.Activo = false;
        _edicion = producto;
    }

    public void Confirmar()
    {
        if (_alta != null)
        {
            _alta.Id = Filas.Select(p => p.Id).DefaultIfEmpty().Max() + 1;
            _alta.CreadoEn = UnidadEnMemoria.FechaGenerada;
            _alta.Categoria = new Categoria { Id = _alta.CategoriaId, Nombre = "Categoría de prueba" };
            Filas.Add(_alta);
        }

        if (_edicion != null)
        {
            var destino = Filas.Single(p => p.Id == _edicion.Id);
            destino.CategoriaId = _edicion.CategoriaId;
            destino.Nombre = _edicion.Nombre;
            destino.PrecioCompra = _edicion.PrecioCompra;
            destino.PrecioVenta = _edicion.PrecioVenta;
            destino.Activo = _edicion.Activo;
        }

        Descartar();
    }

    public void Descartar()
    {
        _alta = null;
        _edicion = null;
    }

    private static Producto Copiar(Producto p) => new()
    {
        Id = p.Id,
        CategoriaId = p.CategoriaId,
        Nombre = p.Nombre,
        PrecioCompra = p.PrecioCompra,
        PrecioVenta = p.PrecioVenta,
        StockActual = p.StockActual,
        Activo = p.Activo,
        CreadoEn = p.CreadoEn,
        Categoria = p.Categoria,
        Movimientos = p.Movimientos
    };
}

internal sealed class HistorialEnMemoria(UnidadEnMemoria unidad) : IHistorialProductoRepository
{
    public List<HistorialProducto> Pendientes { get; } = [];

    public Task AgregarAsync(IEnumerable<HistorialProducto> registros)
    {
        if (!unidad.EnTransaccion)
            throw new InvalidOperationException("Se preparó historial fuera de la transacción.");

        Pendientes.AddRange(registros);
        return Task.CompletedTask;
    }
}

internal sealed class CategoriasEnMemoria(ProductosEnMemoria productos) : ICategoriaRepository
{
    private readonly Categoria[] _categorias =
    [
        new() { Id = 2, Nombre = "Alimentos" },
        new() { Id = 3, Nombre = "Bebidas" }
    ];

    public Task<bool> ExisteCategoriaAsync(int id) => Task.FromResult(_categorias.Any(c => c.Id == id));
    public Task<IEnumerable<Categoria>> GetAll() => Task.FromResult<IEnumerable<Categoria>>(_categorias);
    public Task<Categoria?> GetById(int id) => Task.FromResult(_categorias.FirstOrDefault(c => c.Id == id));
    public Task<bool> TieneProductosAsociadosAsync(int id) => Task.FromResult(productos.Filas.Any(p => p.CategoriaId == id));
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null) => throw new NotSupportedException();
    public Task Add(Categoria entity) => throw new NotSupportedException();
    public void Update(Categoria entity) => throw new NotSupportedException();
    public Task Delete(int id) => throw new NotSupportedException();
}

internal sealed class UsuariosEnMemoria : IUsuarioRepository
{
    public Task<bool> ExisteUsuarioAsync(int id) => Task.FromResult(id == 27);
    public Task<IEnumerable<Usuario>> GetAllUsuariosAsync() => throw new NotSupportedException();
    public Task<Usuario?> GetUsuarioByIdAsync(int id) => throw new NotSupportedException();
    public Task<Usuario?> GetUsuarioByEmailAsync(string email) => throw new NotSupportedException();
    public Task InsertUsuario(Usuario usuario) => throw new NotSupportedException();
    public Task UpdateUsuario(Usuario usuario) => throw new NotSupportedException();
    public Task DeleteUsuario(Usuario usuario) => throw new NotSupportedException();
}
