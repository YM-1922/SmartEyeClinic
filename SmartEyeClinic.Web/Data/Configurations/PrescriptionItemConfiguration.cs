using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
    {
        public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
        {
            builder.ToTable("PrescriptionItems");

            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.Id)
                .UseIdentityColumn();

            builder.Property(pi => pi.PrescriptionId)
                .IsRequired();

            builder.Property(pi => pi.MedicineId)
                .IsRequired();

            builder.Property(pi => pi.Dosage)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(pi => pi.DurationDays)
                .IsRequired(false);

            builder.Property(pi => pi.Instructions)
                .HasMaxLength(300)
                .IsUnicode(false)
                .IsRequired(false);

            // Relationships
            builder.HasOne(pi => pi.PrescriptionHeader)
                .WithMany(ph => ph.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PrescriptionItems_Headers");

            builder.HasOne(pi => pi.Medicine)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(pi => pi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PrescriptionItems_Medicines");
        }
    }
}
