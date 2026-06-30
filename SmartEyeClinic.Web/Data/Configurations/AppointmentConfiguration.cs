using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .UseIdentityColumn();

            builder.Property(a => a.PatientId)
                .IsRequired();

            builder.Property(a => a.DoctorId)
                .IsRequired();

            builder.Property(a => a.ReceptionistId)
                .IsRequired(false);

            builder.Property(a => a.BranchId)
                .IsRequired();

            builder.Property(a => a.AppointmentDateTime)
                .IsRequired();

            builder.Property(a => a.DurationMinutes)
                .HasDefaultValue(30)
                .IsRequired();

            builder.Property(a => a.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(a => a.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(a => a.Notes)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Indexes
            builder.HasIndex(a => new { a.DoctorId, a.AppointmentDateTime })
                .HasDatabaseName("IX_Appointments_Doctor_Date");

            builder.HasIndex(a => a.PatientId)
                .HasDatabaseName("IX_Appointments_Patient");

            // Relationships
            builder.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Appointments_Patients");

            builder.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Appointments_Doctors");

            builder.HasOne(a => a.Receptionist)
                .WithMany(r => r.Appointments)
                .HasForeignKey(a => a.ReceptionistId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Appointments_Receptionists");

            builder.HasOne(a => a.Branch)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Appointments_Branches");
        }
    }
}
