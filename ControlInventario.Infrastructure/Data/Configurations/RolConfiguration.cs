using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class RolConfiguration :
        IEntityTypeConfiguration<Rol>
    {
        public void Configure(EntityTypeBuilder<Rol> entity)
        {
            entity.ToTable("roles");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Nombre).IsUnique().HasDatabaseName("IX_roles_Nombre");

            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        }
    }
}