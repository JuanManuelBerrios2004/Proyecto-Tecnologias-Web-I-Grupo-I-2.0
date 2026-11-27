using ControlInventario.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class HistorialProductoConfiguration : IEntityTypeConfiguration<HistorialProducto>
    {
        public void Configure(EntityTypeBuilder<HistorialProducto> entity)
        {
            entity.ToTable("historial_productos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
            entity.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entity.Property(x => x.CampoModificado).HasColumnName("campo_modificado").HasMaxLength(50).IsRequired();
            entity.Property(x => x.FechaModificacion).HasColumnName("fecha_modificacion")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

            entity.HasOne(x => x.Producto).WithMany()
                .HasForeignKey(x => x.ProductoId).HasConstraintName("fk_historial_productos")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).HasConstraintName("fk_historial_usuarios")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
