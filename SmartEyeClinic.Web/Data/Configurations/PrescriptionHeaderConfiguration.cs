using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PrescriptionHeaderConfiguration : IEntityTypeConfiguration<PrescriptionHeader>
    {
        public void Configure(EntityTypeBuilder<PrescriptionHeader> builder)
        {
            builder.ToTable("PrescriptionHeaders");

            builder.HasKey(ph => ph.Id);

            builder.Property(ph => ph.Id)
                .UseIdentityColumn();

            builder.Property(ph => ph.ExaminationId)
                .IsRequired();

            builder.Property(ph => ph.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Indexes
            builder.HasIndex(ph => ph.ExaminationId)
                .IsUnique();

            // Relationships
            builder.HasOne(ph => ph.Examination)
                .WithOne(e => e.PrescriptionHeader)
                .HasForeignKey<PrescriptionHeader>(ph => ph.ExaminationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PrescriptionHeaders_Examinations");
        }
    }
}
