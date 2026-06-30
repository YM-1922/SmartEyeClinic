using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class SurgeryTypeConfiguration : IEntityTypeConfiguration<SurgeryType>
    {
        public void Configure(EntityTypeBuilder<SurgeryType> builder)
        {
            builder.ToTable("SurgeryTypes");

            builder.HasKey(st => st.Id);

            builder.Property(st => st.Id)
                .UseIdentityColumn();

            builder.Property(st => st.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(st => st.Description)
                .HasMaxLength(300)
                .IsUnicode(false)
                .IsRequired(false);

            builder.HasIndex(st => st.Name)
                .IsUnique();
        }
    }
}
