using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .UseIdentityColumn();

            builder.Property(r => r.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(r => r.Description)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            builder.HasIndex(r => r.Name)
                .IsUnique();
        }
    }
}
