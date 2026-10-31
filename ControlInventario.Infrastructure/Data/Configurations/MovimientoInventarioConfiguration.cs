using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class MovimientoInventarioConfiguration :
        IEntityTypeConfiguration<MovimientoInventario>
    {
        public void Configure(
            EntityTypeBuilder<MovimientoInventario> entity)
        {
            entity.ToTable("movimientos_inventario");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            entity.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            entity.Property(e => e.TipoMovimiento)
                .HasColumnName("tipo_movimiento")
                .HasColumnType("enum('ENTRADA','SALIDA')")
                .IsRequired();

            entity.Property(e => e.Motivo)
                .HasColumnName("motivo")
                .HasColumnType(
                    "enum('COMPRA_PROVEEDOR','VENTA','MERMA_DETERIORO','AJUSTE')")
                .IsRequired();

            entity.Property(e => e.Cantidad)
                .HasColumnName("cantidad")
                .IsRequired();

            entity.Property(e => e.Observaciones)
                .HasColumnName("observaciones")
                .HasMaxLength(255);

            entity.Property(e => e.FechaRegistro)
                .HasColumnName("fecha_registro")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Producto)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Usuario)
                .WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}