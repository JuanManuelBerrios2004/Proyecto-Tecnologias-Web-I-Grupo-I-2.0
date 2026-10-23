using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class PostConfiguration :
        IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> entity)
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("comment");

            entity.HasIndex(e => e.PostId, "FK_Comment_Post");

            entity.HasIndex(e => e.UserId, "FK_Comment_User");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasColumnType("bit(1)");

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_Post");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_User");
        }
    }
}
