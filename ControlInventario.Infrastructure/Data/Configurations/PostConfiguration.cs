using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlInventario.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlInventario.Infrastructure.Data.Configurations
{
    public class CommentConfiguration :
        IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> entity)
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("post");

            entity.HasIndex(e => e.UserId, "FK_Post_User");

            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Image).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.Posts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Post_User");
        }
    }
}
