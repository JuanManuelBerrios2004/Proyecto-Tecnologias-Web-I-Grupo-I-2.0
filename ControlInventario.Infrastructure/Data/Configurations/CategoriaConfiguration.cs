using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> entity)
        {
            entity.ToTable("categorias");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("IX_categorias_Nombre");

            entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
            entity.Property(e => e.CreadoEn).HasColumnName("creado_en")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
