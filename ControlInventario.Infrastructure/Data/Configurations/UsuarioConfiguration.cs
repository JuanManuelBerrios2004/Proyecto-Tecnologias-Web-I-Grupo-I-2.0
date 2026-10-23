using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class UsuarioConfiguration :
        IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> entity)
        {
            entity.ToTable("usuarios");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_usuarios_Email");
            entity.HasIndex(e => e.RolId).HasDatabaseName("IX_usuarios_RolId");

            entity.Property(e => e.RolId).HasColumnName("RolId").IsRequired();
            entity.Property(e => e.NombreCompleto).HasColumnName("NombreCompleto").HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("Email").HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(255);
            entity.Property(e => e.Activo).HasColumnName("Activo")
                .HasColumnType("tinyint(1)").HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasColumnName("CreadoEn")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_usuarios_roles");
        }
    }
}