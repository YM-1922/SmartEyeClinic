using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class SurgeryConfiguration : IEntityTypeConfiguration<Surgery>
    {
        public void Configure(EntityTypeBuilder<Surgery> builder)
        {
            builder.ToTable("Surgeries");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .UseIdentityColumn();

            builder.Property(s => s.PatientId)
                .IsRequired();

            builder.Property(s => s.DoctorId)
                .IsRequired();

            builder.Property(s => s.AppointmentId)
                .IsRequired();

            builder.Property(s => s.SurgeryTypeId)
                .IsRequired();

            builder.Property(s => s.SurgeryDate)
                .IsRequired();

            builder.Property(s => s.Outcome)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(s => s.Notes)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(s => s.AppointmentId)
                .IsUnique();

            // Relationships
            builder.HasOne(s => s.Patient)
                .WithMany(p => p.Surgeries)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Patients");

            builder.HasOne(s => s.Doctor)
                .WithMany(d => d.Surgeries)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Doctors");

            builder.HasOne(s => s.Appointment)
                .WithOne(a => a.Surgery)
                .HasForeignKey<Surgery>(s => s.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Appointments");

            builder.HasOne(s => s.SurgeryType)
                .WithMany(st => st.Surgeries)
                .HasForeignKey(s => s.SurgeryTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Types");
        }
    }
}
