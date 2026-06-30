using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(al => al.Id);

            builder.Property(al => al.Id)
                .UseIdentityColumn();

            builder.Property(al => al.UserId)
                .IsRequired(false);

            builder.Property(al => al.Action)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(al => al.TableName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(al => al.RecordId)
                .IsRequired(false);

            builder.Property(al => al.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Relationships
            builder.HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AuditLogs_Users");
        }
    }
}
