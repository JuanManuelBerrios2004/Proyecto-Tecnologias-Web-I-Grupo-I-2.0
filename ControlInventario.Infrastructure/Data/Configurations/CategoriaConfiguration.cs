using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class CategoriaConfiguration :
        IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> entity)
        {
            entity.ToTable("categorias");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("IX_categorias_Nombre");

            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnName("Descripcion").HasMaxLength(200);
            entity.Property(e => e.CreadoEn).HasColumnName("CreadoEn")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}