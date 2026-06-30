using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class ExaminationConfiguration : IEntityTypeConfiguration<Examination>
    {
        public void Configure(EntityTypeBuilder<Examination> builder)
        {
            builder.ToTable("Examinations");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .UseIdentityColumn();

            builder.Property(e => e.AppointmentId)
                .IsRequired();

            builder.Property(e => e.Diagnosis)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(e => e.Symptoms)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.VisualAcuityLeft)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.VisualAcuityRight)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.IntraocularPressure)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.TreatmentPlan)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.ExaminedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Indexes
            builder.HasIndex(e => e.AppointmentId)
                .IsUnique();

            // Relationships
            builder.HasOne(e => e.Appointment)
                .WithOne(a => a.Examination)
                .HasForeignKey<Examination>(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Examinations_Appointments");
        }
    }
}
