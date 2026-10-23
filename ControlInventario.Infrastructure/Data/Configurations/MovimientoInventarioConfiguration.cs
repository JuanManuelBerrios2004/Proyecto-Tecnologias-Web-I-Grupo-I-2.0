using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class MovimientoInventarioConfiguration :
        IEntityTypeConfiguration<MovimientoInventario>
    {
        public void Configure(EntityTypeBuilder<MovimientoInventario> entity)
        {
            entity.ToTable("movimientos_inventario");

            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.ProductoId).HasDatabaseName("IX_movimientos_inventario_ProductoId");
            entity.HasIndex(e => e.UsuarioId).HasDatabaseName("IX_movimientos_inventario_UsuarioId");

            entity.Property(e => e.ProductoId).HasColumnName("ProductoId").IsRequired();
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioId").IsRequired();

            // ENUM de MySQL: solo admite 'ENTRADA' y 'SALIDA'.
            entity.Property(e => e.TipoMovimiento).HasColumnName("TipoMovimiento")
                .HasColumnType("enum('ENTRADA','SALIDA')").IsRequired();

            // ENUM de MySQL: solo admite los cuatro motivos definidos.
            entity.Property(e => e.Motivo).HasColumnName("Motivo")
                .HasColumnType("enum('COMPRA_PROVEEDOR','VENTA','MERMA_DETERIORO','AJUSTE')")
                .IsRequired();

            entity.Property(e => e.Cantidad).HasColumnName("Cantidad");
            entity.Property(e => e.Observaciones).HasColumnName("Observaciones").HasMaxLength(255);
            entity.Property(e => e.FechaRegistro).HasColumnName("FechaRegistro")
                .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Producto).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_movimientos_inventario_productos");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_movimientos_inventario_usuarios");
        }
    }
}