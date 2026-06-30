using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .UseIdentityColumn();

            builder.Property(u => u.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(u => u.RoleId)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(u => u.ProfilePicture)
                .HasMaxLength(300)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            // Indexes & Unique Constraints
            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");

            // Relationships
            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Users_Roles");
        }
    }
}
