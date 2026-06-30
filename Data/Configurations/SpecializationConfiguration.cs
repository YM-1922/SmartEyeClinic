using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
    {
        public void Configure(EntityTypeBuilder<Specialization> builder)
        {
            builder.ToTable("Specializations");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .UseIdentityColumn();

            builder.Property(s => s.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(300)
                .IsUnicode(false)
                .IsRequired(false);

            builder.HasIndex(s => s.Name)
                .IsUnique();
        }
    }
}
