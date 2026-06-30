using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
    {
        public void Configure(EntityTypeBuilder<Medicine> builder)
        {
            builder.ToTable("Medicines");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                .UseIdentityColumn();

            builder.Property(m => m.Name)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(m => m.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(m => m.Manufacturer)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired(false);
        }
    }
}
