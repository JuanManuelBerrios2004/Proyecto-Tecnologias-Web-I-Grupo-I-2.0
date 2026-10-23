using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class ProductoConfiguration :
        IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> entity)
        {
            entity.ToTable("productos");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("IX_productos_Nombre");
            entity.HasIndex(e => e.CategoriaId).HasDatabaseName("IX_productos_CategoriaId");

            entity.Property(e => e.CategoriaId).HasColumnName("CategoriaId").IsRequired();
            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PrecioCompra).HasColumnName("PrecioCompra")
                .HasColumnType("decimal(10,2)");
            entity.Property(e => e.PrecioVenta).HasColumnName("PrecioVenta")
                .HasColumnType("decimal(10,2)");
            entity.Property(e => e.StockActual).HasColumnName("StockActual")
                .HasDefaultValue(0);
            entity.Property(e => e.Activo).HasColumnName("Activo")
                .HasColumnType("tinyint(1)").HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasColumnName("CreadoEn")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ActualizadoEn).HasColumnName("ActualizadoEn")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_productos_categorias");
        }
    }
}