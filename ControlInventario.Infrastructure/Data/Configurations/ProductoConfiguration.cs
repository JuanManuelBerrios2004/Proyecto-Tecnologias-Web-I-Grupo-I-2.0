using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure
    .Data.Configurations
{
    public class ProductoConfiguration
        : IEntityTypeConfiguration<Producto>
    {
        public void Configure(
            EntityTypeBuilder<Producto> entity)
        {
            entity.ToTable("productos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CategoriaId)
                .HasColumnName("categoria_id")
                .IsRequired();

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
                .HasDefaultValueSql(
                    "CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Categoria)
                .WithMany(x => x.Productos)
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(
                    DeleteBehavior.Restrict);
        }
    }
}