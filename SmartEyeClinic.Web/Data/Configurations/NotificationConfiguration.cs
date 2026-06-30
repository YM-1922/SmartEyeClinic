using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                .UseIdentityColumn();

            builder.Property(n => n.UserId)
                .IsRequired();

            builder.Property(n => n.Type)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(n => n.Title)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(n => n.Channel)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(n => n.Message)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false)
                .IsRequired(false);

            builder.Property(n => n.SentAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Relationships
            builder.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Notifications_Users");
        }
    }
}
