
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Data;

public partial class InventarioContext : DbContext
{
    public InventarioContext()
    {
    }

    public InventarioContext(
        DbContextOptions<InventarioContext> options)
        : base(options)
    {
    }

    // TABLAS DEL SISTEMA DE INVENTARIO

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Rol> Roles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<MovimientoInventario>
        MovimientosInventario { get; set; }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());
    }
}
