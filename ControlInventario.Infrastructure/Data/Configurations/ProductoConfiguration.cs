
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class ProductoConfiguration
        : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> entity)
        {
            entity.ToTable("productos", tabla =>
            {
                tabla.HasCheckConstraint("chk_precio_venta", "precio_venta > precio_compra");
                tabla.HasCheckConstraint("chk_stock_positivo", "stock_actual >= 0");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CategoriaId)
                .HasColumnName("categoria_id")
                .IsRequired();

            entity.HasIndex(x => x.Nombre).IsUnique().HasDatabaseName("nombre");

            entity.Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.PrecioCompra)
                .HasColumnName("precio_compra")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(x => x.PrecioVenta)
                .HasColumnName("precio_venta")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(x => x.StockActual)
                .HasColumnName("stock_actual")
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(x => x.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(x => x.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Categoria)
                .WithMany(x => x.Productos)
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
